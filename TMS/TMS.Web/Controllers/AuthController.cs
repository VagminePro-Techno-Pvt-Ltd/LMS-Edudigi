using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;

namespace TMS.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAccountManager _manager;
        private readonly IMasterManager<UserViewModel> _usermanager;
        private readonly IMasterBaseManager<ForgotPasswordViewModel> _forgotmanager;
        private readonly IDailyAttendanceManager _attendanceManager;
        private readonly StorageSettings _storageSettings;
        public AuthController(IAccountManager manager, IOptions<StorageSettings> options,
            IMasterBaseManager<ForgotPasswordViewModel> forgotmanager,
            IMasterManager<UserViewModel> usermanager,
            IDailyAttendanceManager attendanceManager)
        {
            _manager = manager;
            _storageSettings = options.Value;
            _forgotmanager = forgotmanager;
            _usermanager = usermanager;
            _attendanceManager = attendanceManager;
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

            var user = await _manager.Login(model!.Email!.Trim(), model.Password);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(nameof(LoginViewModel.Password), "Invalid credentials");
                return View(model);
            }

            await IdentitySignin(user.Email, user.Id, user.Name!, model.RememberMe, user.Role?.Name);

            // ── Force Password Change Check ──
            if (user.ForcePasswordChange)
            {
                return RedirectToAction("ChangePassword", new { forced = true });
            }

            if(user.Role?.Name?.ToLower()=="student")
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
                    new Claim("userId", Convert.ToString( userId)),
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
            // Must be logged in
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

            // User stays logged in — redirect to appropriate dashboard
            TempData["PasswordChanged"] = true;

            var role = User?.Claims?.FirstOrDefault(c => c.Type == "role")?.Value?.ToLower();
            if (role == "student")
                return RedirectToAction("StudentDashboard", "Home", new { area = "" });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }


        // =======================
        // VERIFY OTP
        // =======================
        public IActionResult VerifyOtp()
        {
            ViewBag.Email = TempData["Email"];
            return View();
        }

        

        // =======================
        // RESET PASSWORD
        // =======================
        public IActionResult ResetPassword()
        {
            ViewBag.Email = TempData["Email"];
            return View();
        }

       

        // =======================
        // SEND EMAIL HELPER
        // =======================
        private async Task SendOtpEmail(string toEmail, string otp)
        {
            var message = new MailMessage();
            message.To.Add(toEmail);
            message.Subject = "Your OTP Code";
            message.Body = $"Your OTP is: <b>{otp}</b><br/>It is valid for 10 minutes.";
            message.IsBodyHtml = true;

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password"),
                EnableSsl = true
            };

            await smtp.SendMailAsync(message);
        }

        public IActionResult ForgotPasswordFlow(int step = 1, string email = null)
        {
            ViewBag.Step = step;          // 1 = enter email, 2 = otp, 3 = new pass
            ViewBag.Email = email;
            return View("ForgotPasswordUnified");
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _manager.GetUserByEmail(email);
            if (user == null)
            {
                ViewBag.Error = "Email not registered.";
                return View("ForgotPasswordUnified", new { step = 1 });
            }

            var OTPS = await _forgotmanager.GetAsync(null, t => t.IsActive && t.Email == email);
            var lastOtp = OTPS.OrderByDescending(t => t.CreatedOn).FirstOrDefault();


            int otp = new Random().Next(100000, 999999);

            await _forgotmanager.AddUpdateAsync(new ForgotPasswordViewModel
            {
                Email = email,
                OTP = 212121,//otp,
                CreatedOn = DateTime.Now,
                IsActive = true
            }, 1);

            // await SendOtpEmail(email, otp);

            return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
        }
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            var OTPS = await _forgotmanager.GetAsync(null, t => t.IsActive && t.Email == email);
            var latestOtp = OTPS.OrderByDescending(t => t.CreatedOn).FirstOrDefault();

            if (latestOtp == null)
            {
                ViewBag.Error = "OTP not generated.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            if (latestOtp.OTP.ToString() != otp)
            {
                ViewBag.Error = "Invalid OTP.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            if ((DateTime.UtcNow - latestOtp.CreatedOn).TotalMinutes > 10)
            {
                ViewBag.Error = "OTP expired.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 2, email });
            }

            return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string newPassword)
        {
            var user = await _manager.GetUserByEmail(email);

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
            }

            user.Password = newPassword;
            bool status = await _manager.UpdatePassword(user);
            if (!status)
            {
                ViewBag.Error = "Something went wrong.";
                return RedirectToAction("ForgotPasswordFlow", new { step = 3, email });
            }

            return RedirectToAction("Login");
        }



    }
}
