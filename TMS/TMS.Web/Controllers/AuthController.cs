using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Email;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using TMS.Web.Services;
using TMS.Web.Services.Email;

namespace TMS.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAccountManager _manager;
        private readonly IMasterManager<UserViewModel> _usermanager;
        private readonly IMasterBaseManager<ForgotPasswordViewModel> _forgotmanager;
        private readonly IDailyAttendanceManager _attendanceManager;
        private readonly StorageSettings _storageSettings;
        private readonly IEmailOutboxService? _outboxService;
        private readonly IAuditLogService? _auditLogService;

        public AuthController(
            IAccountManager manager,
            IOptions<StorageSettings> options,
            IMasterBaseManager<ForgotPasswordViewModel> forgotmanager,
            IMasterManager<UserViewModel> usermanager,
            IDailyAttendanceManager attendanceManager,
            IEmailOutboxService? outboxService = null,
            IAuditLogService? auditLogService = null)
        {
            _manager = manager;
            _storageSettings = options.Value;
            _forgotmanager = forgotmanager;
            _usermanager = usermanager;
            _attendanceManager = attendanceManager;
            _outboxService = outboxService;
            _auditLogService = auditLogService;
        }

        public IActionResult LoginOptional()
        {
            return View();
        }

        public IActionResult Login(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel? model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model == null || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.ClearValidationState(nameof(LoginViewModel.Password));
                return View(model);
            }

            var user = await _manager.Login(model.Email, model.Password);
            if (user == null || user.Id <= 0)
            {
                ModelState.AddModelError("", "Invalid Email or Password");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is disabled, please contact administrator.");
                return View(model);
            }

            await IdentitySignin(user.Email, user.Id, user.Name!, model.RememberMe, user.Role?.Name);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    "USER_LOGIN",
                    "Authentication",
                    "UserMaster",
                    user.Id.ToString(),
                    null,
                    new { Email = user.Email, Role = user.Role?.Name },
                    "Session",
                    "SUCCESS",
                    $"User '{user.Email}' logged in successfully.",
                    user.Id);
            }

            // ── Force Password Change Check ──
            if (user.ForcePasswordChange)
            {
                return RedirectToAction("ChangePassword", new { forced = true });
            }

            if (user.Role?.Name?.ToLower() == "student")
            {
                await _attendanceManager.MarkLoginAttendance(user.Id);
                return RedirectToAction("StudentDashboard", "Home", new { area = "" });
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        private async Task IdentitySignin(string userName, int userId, string name, bool isPersistent = false, string? Role = null)
        {
            var claims = new List<Claim>
            {
                new Claim("user", userName),
                new Claim("userId", Convert.ToString(userId)),
                new Claim("userName", name)
            };
            if (!string.IsNullOrWhiteSpace(Role))
                claims.Add(new("role", Role));
            else
                claims.Add(new("role", "User"));

            await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme, "user", "role")), new AuthenticationProperties() { IsPersistent = isPersistent });
        }

        public async Task<IActionResult> LogOff()
        {
            var userIdClaim = User?.Claims?.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (int.TryParse(userIdClaim, out int uid) && _auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    "USER_LOGOUT",
                    "Authentication",
                    "UserMaster",
                    uid.ToString(),
                    null,
                    null,
                    "Session",
                    "SUCCESS",
                    "User logged out.",
                    uid);
            }

            await HttpContext.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("LoginOptional");
        }

        // ═══════════════════════════════════════
        //  CHANGE PASSWORD
        // ═══════════════════════════════════════
        [HttpGet]
        public IActionResult ChangePassword(bool forced = false)
        {
            var userIdClaim = User?.Claims?.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return RedirectToAction("Login");

            var model = new ChangePasswordViewModel { IsForced = forced };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userIdClaim = User?.Claims?.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return RedirectToAction("Login");

            int userId = int.Parse(userIdClaim);

            if (!ModelState.IsValid)
                return View(model);

            // Verify current password
            bool isCurrentValid = await _manager.VerifyCurrentPassword(userId, model.CurrentPassword);
            if (!isCurrentValid)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // Prevent same password reuse
            bool isSameAsCurrent = await _manager.VerifyCurrentPassword(userId, model.NewPassword);
            if (isSameAsCurrent)
            {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as your current password.");
                return View(model);
            }

            // Change the password
            bool success = await _manager.ChangePassword(userId, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to change password. Please try again.");
                return View(model);
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    "PASSWORD_CHANGE",
                    "Authentication",
                    "UserMaster",
                    userId.ToString(),
                    null,
                    new { Password = "[REDACTED]" },
                    "Password",
                    "SUCCESS",
                    "User changed password successfully.",
                    userId);
            }

            // User stays logged in — redirect to appropriate dashboard
            TempData["PasswordChanged"] = true;

            try
            {
                var userObj = await _usermanager.GetAsync(userId);
                if (userObj != null && !string.IsNullOrWhiteSpace(userObj.Email) && _outboxService != null)
                {
                    await _outboxService.EnqueueAsync(new EmailEnqueueRequest
                    {
                        RecipientEmail = userObj.Email.Trim(),
                        RecipientName = userObj.Name,
                        Subject = "Security Notice: Your EduDigiClass LMS Password Was Changed",
                        TemplateName = "PasswordChangedAlert",
                        TemplateData = new Dictionary<string, string>
                        {
                            ["RecipientName"] = userObj.Name ?? "User",
                            ["TimestampUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                        },
                        EventType = "PasswordChanged",
                        IdempotencyKey = $"pwd-change-{userId}-{DateTime.UtcNow.Ticks}"
                    });
                }
            }
            catch
            {
                // Non-fatal for password change response
            }

            var role = User?.Claims?.FirstOrDefault(c => c.Type == "role")?.Value?.ToLower();
            if (role == "student")
                return RedirectToAction("StudentDashboard", "Home", new { area = "" });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // =======================
        // FORGOT PASSWORD FLOW
        // =======================
        public IActionResult ForgotPassword()
        {
            return RedirectToAction("ForgotPasswordFlow", new { step = 1 });
        }

        public IActionResult VerifyOtp()
        {
            ViewBag.Email = TempData["Email"];
            return View();
        }

        public IActionResult ResetPassword()
        {
            ViewBag.Email = TempData["Email"];
            return View();
        }

        public IActionResult ForgotPasswordFlow(int step = 1, string? email = null)
        {
            ViewBag.Step = step;
            ViewBag.Email = email;
            return View("ForgotPasswordUnified");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter a valid email address.";
                return View("ForgotPasswordUnified", new { step = 1 });
            }

            email = email.Trim();
            var user = await _manager.GetUserByEmail(email);

            if (user != null && user.IsActive)
            {
                var activeOtps = await _forgotmanager.GetAsync(null, t => t.IsActive && t.Email == email);
                var recentOtp = activeOtps
                    .Where(t => (DateTime.UtcNow - t.CreatedOn).TotalMinutes < 1)
                    .OrderByDescending(t => t.CreatedOn)
                    .FirstOrDefault();

                if (recentOtp == null)
                {
                    foreach (var old in activeOtps)
                    {
                        old.IsActive = false;
                        await _forgotmanager.AddUpdateAsync(old, 1);
                    }

                    int secureOtp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000);

                    await _forgotmanager.AddUpdateAsync(new ForgotPasswordViewModel
                    {
                        Email = email,
                        OTP = secureOtp,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    }, 1);

                    if (_outboxService != null)
                    {
                        await _outboxService.EnqueueAsync(new EmailEnqueueRequest
                        {
                            RecipientEmail = user.Email,
                            RecipientName = user.Name,
                            Subject = "EduDigiClass LMS — Password Reset OTP Verification",
                            TemplateName = "PasswordResetOtp",
                            TemplateData = new Dictionary<string, string>
                            {
                                ["Otp"] = secureOtp.ToString(),
                                ["RecipientName"] = user.Name ?? "Student / Faculty"
                            },
                            EventType = "PasswordResetOtp",
                            IdempotencyKey = $"otp-reset-{user.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                        });
                    }
                }
            }

            return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                ViewBag.Error = "Please enter both your email address and the 6-digit OTP.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            email = email.Trim();
            otp = otp.Trim();

            var otps = await _forgotmanager.GetAsync(null, t => t.IsActive && t.Email == email);
            var latestOtp = otps.OrderByDescending(t => t.CreatedOn).FirstOrDefault();

            if (latestOtp == null)
            {
                ViewBag.Error = "No active OTP request found. Please request a new verification code.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            if (latestOtp.OTP.ToString() != otp)
            {
                ViewBag.Error = "Invalid verification code. Please check your email and try again.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            if ((DateTime.UtcNow - latestOtp.CreatedOn).TotalMinutes > 10)
            {
                ViewBag.Error = "Verification code has expired (10-minute validity limit). Please request a new OTP.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = "Email and new password are required.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
            }

            email = email.Trim();
            var user = await _manager.GetUserByEmail(email);

            if (user == null)
            {
                ViewBag.Error = "Unable to process password reset request.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
            }

            var activeOtps = await _forgotmanager.GetAsync(null, t => t.IsActive && t.Email == email);
            var validOtp = activeOtps.OrderByDescending(t => t.CreatedOn).FirstOrDefault();
            if (validOtp == null || (DateTime.UtcNow - validOtp.CreatedOn).TotalMinutes > 15)
            {
                ViewBag.Error = "Session expired or already used. Please request a new verification code.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 1 });
            }

            user.Password = newPassword;
            bool status = await _manager.UpdatePassword(user);
            if (!status)
            {
                ViewBag.Error = "Something went wrong while resetting password. Please try again.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
            }

            foreach (var ot in activeOtps)
            {
                ot.IsActive = false;
                await _forgotmanager.AddUpdateAsync(ot, 1);
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    "PASSWORD_RESET",
                    "Authentication",
                    "UserMaster",
                    user.Id.ToString(),
                    null,
                    new { Password = "[REDACTED]" },
                    "Password",
                    "SUCCESS",
                    "User reset password via secure OTP.",
                    user.Id);
            }

            if (_outboxService != null)
            {
                await _outboxService.EnqueueAsync(new EmailEnqueueRequest
                {
                    RecipientEmail = user.Email,
                    RecipientName = user.Name,
                    Subject = "Security Notice: Your EduDigiClass LMS Password Was Changed",
                    TemplateName = "PasswordChangedAlert",
                    TemplateData = new Dictionary<string, string>
                    {
                        ["RecipientName"] = user.Name ?? "User",
                        ["TimestampUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                    },
                    EventType = "PasswordChanged",
                    IdempotencyKey = $"pwd-reset-success-{user.Id}-{DateTime.UtcNow.Ticks}"
                });
            }

            TempData["SuccessMessage"] = "Your password has been reset successfully! Please sign in with your new password.";
            return RedirectToAction("Login");
        }
    }
}
