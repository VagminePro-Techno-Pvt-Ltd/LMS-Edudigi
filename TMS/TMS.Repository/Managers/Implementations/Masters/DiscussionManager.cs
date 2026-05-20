using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Masters;

namespace TMS.Repository.Managers.Implementations.Masters
{
    public class DiscussionManager : IDiscussionManager
    {
        private readonly ApplicationDBContext _context;

        public DiscussionManager(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DiscussionThread>> GetThreadsByProgramAsync(int programId, string filter)
        {
            var query = _context.DiscussionThreads
                .Include(t => t.CreatedByUser)
                .Include(t => t.Replies)
                .Where(t => t.ProgramId == programId && t.IsActive);

            switch (filter?.ToLower() ?? "latest")
            {
                case "popular":
                    query = query.OrderByDescending(t => t.Replies.Count);
                    break;
                case "unanswered":
                    query = query.Where(t => !t.Replies.Any());
                    break;
                case "latest":
                default:
                    query = query.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.CreatedOn);
                    break;
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<CourseCategoryMaster>> GetUserProgramsAsync(int userId, string userRole)
        {
            userRole = userRole?.ToUpper() ?? "";

            if (userRole.Contains("ADMIN"))
            {
                return await _context.CourseCategoryMasters.Where(c => c.IsActive).ToListAsync();
            }

            if (userRole.Contains("STUDENT"))
            {
                return await _context.CourseEnrollments
                    .Include(e => e.Course).ThenInclude(c => c.CourseCategory)
                    .Where(e => e.StudentId == userId && e.Status == 1 && e.Course.CourseCategory != null)
                    .Select(e => e.Course.CourseCategory!)
                    .Distinct()
                    .ToListAsync();
            }

            if (userRole.Contains("FACULTY"))
            {
                return await _context.CourseFacultyMaps
                    .Include(m => m.Course).ThenInclude(c => c.CourseCategory)
                    .Where(m => m.FacultyId == userId && m.Course.CourseCategory != null)
                    .Select(m => m.Course.CourseCategory!)
                    .Distinct()
                    .ToListAsync();
            }

            return new List<CourseCategoryMaster>();
        }

        public async Task<DiscussionThread> GetThreadDetailsAsync(int threadId)
        {
            var thread = await _context.DiscussionThreads
                .Include(t => t.CreatedByUser)
                .Include(t => t.Attachments)
                .Include(t => t.Replies).ThenInclude(r => r.User)
                .Include(t => t.Replies).ThenInclude(r => r.Attachments)
                .FirstOrDefaultAsync(t => t.Id == threadId);

            if (thread != null)
            {
                thread.ViewCount++;
                await _context.SaveChangesAsync();
                
                // Build tree structure for replies
                if (thread.Replies != null)
                {
                    var lookup = thread.Replies.ToLookup(r => r.ParentReplyId);
                    foreach (var reply in thread.Replies)
                    {
                        reply.ChildReplies = lookup[reply.Id].OrderBy(r => r.CommentedOn).ToList();
                    }
                    thread.Replies = thread.Replies.Where(r => r.ParentReplyId == null).ToList();
                }
            }

            return thread;
        }

        public async Task<int> CreateThreadAsync(DiscussionThread thread, string body, int userId)
        {
            thread.CreatedByUserId = userId;
            thread.CreatedOn = DateTime.Now;
            thread.IsActive = true;
            thread.CreatedBy = userId;

            _context.DiscussionThreads.Add(thread);
            await _context.SaveChangesAsync();

            // Save the body as the first reply
            var initialReply = new DiscussionReply
            {
                ThreadId = thread.Id,
                UserId = userId,
                CommentText = body,
                CommentedOn = DateTime.Now,
                IsActive = true,
                CreatedBy = userId,
                CreatedOn = DateTime.Now
            };
            _context.DiscussionReplies.Add(initialReply);
            await _context.SaveChangesAsync();

            return thread.Id;
        }

        public async Task<int> AddReplyAsync(DiscussionReply reply, int userId)
        {
            reply.UserId = userId;
            reply.CommentedOn = DateTime.Now;
            reply.IsActive = true;
            reply.CreatedBy = userId;
            reply.CreatedOn = DateTime.Now;

            _context.DiscussionReplies.Add(reply);
            await _context.SaveChangesAsync();

            return reply.Id;
        }

        public async Task<bool> SaveAttachmentsAsync(List<DiscussionAttachment> attachments)
        {
            if (attachments != null && attachments.Any())
            {
                _context.DiscussionAttachments.AddRange(attachments);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> MarkReplyAsAcceptedAsync(int replyId, int userId)
        {
            var reply = await _context.DiscussionReplies.Include(r => r.Thread).FirstOrDefaultAsync(r => r.Id == replyId);
            if (reply == null) return false;

            var existingAccepted = await _context.DiscussionReplies
                .Where(r => r.ThreadId == reply.ThreadId && r.IsAccepted)
                .ToListAsync();
            
            foreach (var r in existingAccepted) r.IsAccepted = false;

            reply.IsAccepted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserAccessForumAsync(int programId, int userId, string userRole)
        {
            if (string.IsNullOrEmpty(userRole)) return false;
            
            // Normalize role for comparison
            userRole = userRole.ToUpper();

            // Admin access (Super Admin or Campus Admin)
            if (userRole.Contains("ADMIN")) return true;

            if (userRole.Contains("STUDENT"))
            {
                return await _context.CourseEnrollments
                    .Include(e => e.Course)
                    .AnyAsync(e => e.StudentId == userId && 
                                   e.Status == 1 && 
                                   e.Course.CourseCategoryId == programId);
            }

            if (userRole.Contains("FACULTY"))
            {
                return await _context.CourseFacultyMaps
                    .Include(m => m.Course)
                    .AnyAsync(m => m.FacultyId == userId && 
                                   m.Course.CourseCategoryId == programId);
            }

            return false;
        }


        public async Task<bool> ToggleLockAsync(int threadId, int userId)
        {
            var thread = await _context.DiscussionThreads.FindAsync(threadId);
            if (thread == null) return false;
            thread.IsLocked = !thread.IsLocked;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TogglePinAsync(int threadId, int userId)
        {
            var thread = await _context.DiscussionThreads.FindAsync(threadId);
            if (thread == null) return false;
            thread.IsPinned = !thread.IsPinned;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

