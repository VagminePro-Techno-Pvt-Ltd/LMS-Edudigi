using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.Repository.Managers;
using TMS.Models.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Linq;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class DiscussionController : BaseController
    {
        private readonly IDiscussionManager _discussionManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly TMS.Web.Services.IContentModerationService _moderationService;

        public DiscussionController(
            IDiscussionManager discussionManager,
            IWebHostEnvironment webHostEnvironment,
            TMS.Web.Services.IContentModerationService moderationService)
        {
            _discussionManager = discussionManager;
            _webHostEnvironment = webHostEnvironment;
            _moderationService = moderationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int programId, string filter = "latest")
        {
            if (programId <= 0)
            {
                var programs = await _discussionManager.GetUserProgramsAsync(GetUserId(), GetUserRole());
                if (programs.Count() == 1)
                {
                    return RedirectToAction(nameof(Index), new { programId = programs.First().Id });
                }
                return View("SelectProgram", programs);
            }

            if (!await _discussionManager.CanUserAccessForumAsync(programId, GetUserId(), GetUserRole()))
            {
                SetApplicationResult(false, "You do not have access to this program forum.");
                return RedirectToAction("Index", "Home");
            }

            var threads = await _discussionManager.GetThreadsByProgramAsync(programId, filter);
            ViewBag.ProgramId = programId;
            ViewBag.Filter = filter;
            return View(threads);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var thread = await _discussionManager.GetThreadDetailsAsync(id);
            if (thread == null) return NotFound();

            if (!await _discussionManager.CanUserAccessForumAsync(thread.ProgramId ?? 0, GetUserId(), GetUserRole()))
            {
                SetApplicationResult(false, "You do not have access to this discussion.");
                return RedirectToAction("Index", "Home");
            }

            return View(thread);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateThread(DiscussionThread thread, string CommentText, List<IFormFile> attachments)
        {
            if (!string.IsNullOrWhiteSpace(thread.Title) && !string.IsNullOrWhiteSpace(CommentText) && (thread.ProgramId ?? 0) > 0)
            {
                int userId = GetUserId();
                var modCheck = _moderationService.CheckContent($"{thread.Title} {CommentText}");

                var threadId = await _discussionManager.CreateThreadAsync(thread, CommentText, userId);
                if (attachments != null && attachments.Any())
                {
                    await HandleAttachments(attachments, threadId, null);
                }

                if (modCheck.IsInappropriate)
                {
                    await _moderationService.CreateFlagAsync(
                        "DiscussionThread",
                        threadId,
                        userId,
                        User.Identity?.Name,
                        thread.ProgramId,
                        null,
                        $"{thread.Title}\n{CommentText}",
                        modCheck.DetectedKeywords);

                    SetApplicationResult(true, "Discussion created. Notice: Your post was flagged for community standards review.");
                }
                else
                {
                    SetApplicationResult(true, "Discussion created successfully.");
                }
            }
            else
            {
                SetApplicationResult(false, "Failed to create discussion. Please provide a title, message, and program.");
            }
            return RedirectToAction(nameof(Index), new { programId = thread.ProgramId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(DiscussionReply reply, List<IFormFile> attachments)
        {
            if (!string.IsNullOrWhiteSpace(reply.CommentText) && reply.ThreadId > 0)
            {
                int userId = GetUserId();
                var modCheck = _moderationService.CheckContent(reply.CommentText);

                var replyId = await _discussionManager.AddReplyAsync(reply, userId);
                if (attachments != null && attachments.Any())
                {
                    await HandleAttachments(attachments, null, replyId);
                }

                if (modCheck.IsInappropriate)
                {
                    await _moderationService.CreateFlagAsync(
                        "DiscussionReply",
                        replyId,
                        userId,
                        User.Identity?.Name,
                        null,
                        null,
                        reply.CommentText,
                        modCheck.DetectedKeywords);

                    SetApplicationResult(true, "Reply posted. Notice: Your reply was flagged for community standards review.");
                }
                else
                {
                    SetApplicationResult(true, "Reply posted successfully.");
                }
            }
            else
            {
                SetApplicationResult(false, "Please enter your answer text.");
            }
            return RedirectToAction(nameof(Details), new { id = reply.ThreadId });
        }

        private async Task HandleAttachments(List<IFormFile> files, int? threadId, int? replyId)
        {
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "forum");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            var dbAttachments = new List<DiscussionAttachment>();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string uniqueName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                    string fullPath = Path.Combine(uploadDir, uniqueName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    dbAttachments.Add(new DiscussionAttachment
                    {
                        ThreadId = threadId,
                        ReplyId = replyId,
                        FileName = file.FileName,
                        FilePath = "/uploads/forum/" + uniqueName,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        UploadedBy = GetUserId(),
                        IsActive = true,
                        CreatedBy = GetUserId(),
                        CreatedOn = DateTime.Now
                    });
                }
            }
            if (dbAttachments.Any())
            {
                await _discussionManager.SaveAttachmentsAsync(dbAttachments);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAccepted(int replyId, int threadId)
        {
            var success = await _discussionManager.MarkReplyAsAcceptedAsync(replyId, GetUserId());
            return Json(new { success });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var success = await _discussionManager.ToggleLockAsync(id, GetUserId());
            return Json(new { success });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> TogglePin(int id)
        {
            var success = await _discussionManager.TogglePinAsync(id, GetUserId());
            return Json(new { success });
        }
    }
}


