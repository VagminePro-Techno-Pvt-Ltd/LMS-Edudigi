using Microsoft.AspNetCore.Mvc;
using TMS.Repository.Managers;
using TMS.ViewModels.Chat;

namespace TMS.Web.Controllers
{
    public class ChatController : BaseController
    {
        private readonly IChatManager _chatManager;
        private readonly IWebHostEnvironment _env;

        public ChatController(IChatManager chatManager, IWebHostEnvironment env)
        {
            _chatManager = chatManager;
            _env = env;
        }

        /// <summary>
        /// Loads the chat shell page.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// AJAX: Get all chat partners for the logged-in user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPartners()
        {
            var userId = GetUserId();
            var role = GetUserRole();
            if (userId == 0) return Unauthorized();

            var partners = await _chatManager.GetChatPartnersAsync(userId, role);
            return Json(partners);
        }

        /// <summary>
        /// AJAX: Get conversation history with a specific partner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMessages(int partnerId, int courseId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            // Mark messages as read when the conversation is opened
            await _chatManager.MarkAsReadAsync(userId, partnerId, courseId);

            var messages = await _chatManager.GetConversationAsync(userId, partnerId, courseId);
            return Json(messages);
        }

        /// <summary>
        /// AJAX POST: Send a message with optional file attachments.
        /// Accepts multipart/form-data for file uploads.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromForm] int courseId, [FromForm] int receiverId, [FromForm] string? messageText, List<IFormFile>? files)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            // Must have either text or files
            if (string.IsNullOrWhiteSpace(messageText) && (files == null || files.Count == 0))
                return Json(new { success = false, message = "Message cannot be empty." });

            var request = new SendMessageRequest
            {
                CourseId = courseId,
                ReceiverId = receiverId,
                MessageText = messageText
            };

            var savedMsg = await _chatManager.SendMessageAsync(userId, request);
            if (savedMsg == null)
                return Json(new { success = false, message = "Failed to send message." });

            // Handle file uploads
            if (files != null && files.Count > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "chat", courseId.ToString());
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                foreach (var file in files)
                {
                    if (file.Length > 0 && file.Length <= 10 * 1024 * 1024) // Max 10MB
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".mp4" };
                        if (!allowed.Contains(ext)) continue;

                        var uniqueName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadDir, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var relativePath = $"/uploads/chat/{courseId}/{uniqueName}";
                        var fileType = GetFileCategory(ext);

                        var attachment = await _chatManager.SaveAttachmentAsync(
                            savedMsg.Id, file.FileName, relativePath, fileType, file.Length);

                        if (attachment != null)
                            savedMsg.Attachments.Add(attachment);
                    }
                }
            }

            return Json(new { success = true, data = savedMsg });
        }

        /// <summary>
        /// AJAX POST: Mark messages from a partner as read.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkRead(int partnerId, int courseId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            await _chatManager.MarkAsReadAsync(userId, partnerId, courseId);
            return Json(new { success = true });
        }

        /// <summary>
        /// AJAX: Get total unread count for notification badges.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var count = await _chatManager.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        /// <summary>
        /// Maps file extensions to human-readable categories.
        /// </summary>
        private static string GetFileCategory(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => "Image",
                ".pdf" => "PDF",
                ".doc" or ".docx" => "Document",
                ".xls" or ".xlsx" => "Spreadsheet",
                ".ppt" or ".pptx" => "Presentation",
                ".mp4" => "Video",
                ".zip" => "Archive",
                _ => "File"
            };
        }
    }
}
