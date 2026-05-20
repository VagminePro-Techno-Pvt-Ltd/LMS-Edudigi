using Microsoft.EntityFrameworkCore;
using TMS.Models.Account;
using TMS.Models.Chat;
using TMS.Models.Masters;
using TMS.Models.Training;
using TMS.Repository.Managers;
using TMS.ViewModels.Chat;

namespace TMS.Repository.Managers.Implementations
{
    internal class ChatManager : IChatManager
    {
        private readonly ApplicationDBContext _context;

        public ChatManager(ApplicationDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all users the current user can chat with based on shared course memberships.
        /// Student  → sees Faculty + Admin of enrolled courses
        /// Faculty  → sees Students + Admin of assigned courses
        /// Admin    → sees everyone across all courses
        /// </summary>
        public async Task<List<ChatPartnerViewModel>> GetChatPartnersAsync(int userId, string role)
        {
            var partners = new List<ChatPartnerViewModel>();
            
            // Normalize role check (Super Admin, Admin, coordinator etc)
            bool isAdmin = role.Contains("Admin", StringComparison.OrdinalIgnoreCase) || 
                           role.Contains("coordinator", StringComparison.OrdinalIgnoreCase);

            // 1. Get List of all Admins globally (Support/Campus Admins)
            var adminUsers = await (
                from u in _context.UserMasters
                join r in _context.RoleMaster on u.RoleId equals r.Id
                where u.IsActive && (r.Name.Contains("Admin") || r.Name.Contains("coordinator"))
                select new { u.Id, u.Name, RoleName = r.Name }
            ).ToListAsync();

            if (isAdmin)
            {
                // Admin View: See all Faculty and Students in the system
                var facultyPartners = await (
                    from fm in _context.CourseFacultyMaps
                    join c in _context.CourseMasters on fm.CourseId equals c.Id
                    join u in _context.UserMasters on fm.FacultyId equals u.Id
                    join r in _context.RoleMaster on u.RoleId equals r.Id
                    where u.IsActive && fm.FacultyId != userId
                    select new ChatPartnerViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Role = r.Name,
                        CourseName = c.Name,
                        CourseId = c.Id
                    }).ToListAsync();

                var studentPartners = await (
                    from en in _context.CourseEnrollments
                    join c in _context.CourseMasters on en.CourseId equals c.Id
                    join u in _context.UserMasters on en.StudentId equals u.Id
                    join r in _context.RoleMaster on u.RoleId equals r.Id
                    where u.IsActive && en.StudentId != userId
                    select new ChatPartnerViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Role = r.Name,
                        CourseName = c.Name,
                        CourseId = c.Id
                    }).ToListAsync();

                partners.AddRange(facultyPartners);
                partners.AddRange(studentPartners);

                // See other Admins
                foreach (var adm in adminUsers.Where(a => a.Id != userId))
                {
                    partners.Add(new ChatPartnerViewModel {
                        UserId = adm.Id,
                        Name = adm.Name,
                        Role = adm.RoleName,
                        CourseName = "Campus Administration",
                        CourseId = 0 // Dummy or Reserved
                    });
                }
            }
            else if (role.Contains("Faculty", StringComparison.OrdinalIgnoreCase))
            {
                // Faculty View
                var myCourses = await (
                    from fm in _context.CourseFacultyMaps
                    join c in _context.CourseMasters on fm.CourseId equals c.Id
                    where fm.FacultyId == userId
                    select new { c.Id, c.Name }
                ).ToListAsync();

                var myCourseIds = myCourses.Select(c => c.Id).ToList();

                // Students in my courses
                var studentPartners = await (
                    from en in _context.CourseEnrollments
                    join c in _context.CourseMasters on en.CourseId equals c.Id
                    join u in _context.UserMasters on en.StudentId equals u.Id
                    join r in _context.RoleMaster on u.RoleId equals r.Id
                    where myCourseIds.Contains(en.CourseId) && u.IsActive
                    select new ChatPartnerViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Role = r.Name,
                        CourseName = c.Name,
                        CourseId = c.Id
                    }).ToListAsync();

                // Other Faculty in same courses
                var otherFaculty = await (
                    from fm in _context.CourseFacultyMaps
                    join c in _context.CourseMasters on fm.CourseId equals c.Id
                    join u in _context.UserMasters on fm.FacultyId equals u.Id
                    join r in _context.RoleMaster on u.RoleId equals r.Id
                    where myCourseIds.Contains(fm.CourseId) && fm.FacultyId != userId && u.IsActive
                    select new ChatPartnerViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Role = r.Name,
                        CourseName = c.Name,
                        CourseId = c.Id
                    }).ToListAsync();

                partners.AddRange(studentPartners);
                partners.AddRange(otherFaculty);

                // Add Admins to each course context
                foreach (var c in myCourses)
                {
                    foreach (var adm in adminUsers)
                    {
                        partners.Add(new ChatPartnerViewModel {
                            UserId = adm.Id,
                            Name = adm.Name,
                            Role = adm.RoleName,
                            CourseName = c.Name,
                            CourseId = c.Id
                        });
                    }
                }
            }
            else // Student View
            {
                var myCourses = await (
                    from en in _context.CourseEnrollments
                    join c in _context.CourseMasters on en.CourseId equals c.Id
                    where en.StudentId == userId
                    select new { c.Id, c.Name }
                ).ToListAsync();

                var myCourseIds = myCourses.Select(c => c.Id).ToList();

                // Faculty in my courses
                var facultyPartners = await (
                    from fm in _context.CourseFacultyMaps
                    join c in _context.CourseMasters on fm.CourseId equals c.Id
                    join u in _context.UserMasters on fm.FacultyId equals u.Id
                    join r in _context.RoleMaster on u.RoleId equals r.Id
                    where myCourseIds.Contains(fm.CourseId) && u.IsActive
                    select new ChatPartnerViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Role = r.Name,
                        CourseName = c.Name,
                        CourseId = c.Id
                    }).ToListAsync();

                // Other Students in same courses
                var otherStudents = await (
                    from en in _context.CourseEnrollments
                    join c in _context.CourseMasters on en.CourseId equals c.Id
                    join u in _context.UserMasters on en.StudentId equals u.Id
                    join r in _context.RoleMaster on u.RoleId equals r.Id
                    where myCourseIds.Contains(en.CourseId) && en.StudentId != userId && u.IsActive
                    select new ChatPartnerViewModel
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Role = r.Name,
                        CourseName = c.Name,
                        CourseId = c.Id
                    }).ToListAsync();

                partners.AddRange(facultyPartners);
                partners.AddRange(otherStudents);

                // Add Admins to each course context
                foreach (var c in myCourses)
                {
                    foreach (var adm in adminUsers)
                    {
                        partners.Add(new ChatPartnerViewModel {
                            UserId = adm.Id,
                            Name = adm.Name,
                            Role = adm.RoleName,
                            CourseName = c.Name,
                            CourseId = c.Id
                        });
                    }
                }
            }

            // Deduplicate: same user may appear through multiple courses (or manual addition)
            var uniquePartners = partners
                .GroupBy(p => new { p.UserId, p.CourseId })
                .Select(g => g.First())
                .ToList();

            // Attach last message info and unread count for each partner-course pair
            foreach (var partner in uniquePartners)
            {
                var lastMsg = await _context.ChatMessages
                    .Where(m => m.CourseId == partner.CourseId
                        && !m.IsDeleted
                        && ((m.SenderId == userId && m.ReceiverId == partner.UserId)
                            || (m.SenderId == partner.UserId && m.ReceiverId == userId)))
                    .OrderByDescending(m => m.SentOn)
                    .FirstOrDefaultAsync();

                if (lastMsg != null)
                {
                    partner.LastMessage = string.IsNullOrEmpty(lastMsg.MessageText)
                        ? "📎 Attachment"
                        : (lastMsg.MessageText.Length > 40
                            ? lastMsg.MessageText.Substring(0, 40) + "..."
                            : lastMsg.MessageText);
                    partner.LastMessageTime = lastMsg.SentOn;
                }

                partner.UnreadCount = await _context.ChatMessages
                    .CountAsync(m => m.CourseId == partner.CourseId
                        && m.SenderId == partner.UserId
                        && m.ReceiverId == userId
                        && !m.IsRead
                        && !m.IsDeleted);
            }

            return uniquePartners
                .OrderByDescending(p => p.LastMessageTime)
                .ThenBy(p => p.Name)
                .ToList();
        }

        /// <summary>
        /// Loads the message history between two users for a specific course.
        /// </summary>
        public async Task<List<ChatMessageViewModel>> GetConversationAsync(int userId, int partnerId, int courseId, int take = 50)
        {
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Course)
                .Include(m => m.Attachments)
                .Where(m => m.CourseId == courseId
                    && !m.IsDeleted
                    && ((m.SenderId == userId && m.ReceiverId == partnerId)
                        || (m.SenderId == partnerId && m.ReceiverId == userId)))
                .OrderByDescending(m => m.SentOn)
                .Take(take)
                .ToListAsync();

            return messages
                .OrderBy(m => m.SentOn)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    CourseId = m.CourseId,
                    CourseName = m.Course?.Name,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.Name,
                    SenderRole = m.Sender?.Role?.Name,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver?.Name,
                    MessageText = m.MessageText,
                    SentOn = m.SentOn,
                    IsRead = m.IsRead,
                    Attachments = m.Attachments?.Select(a => new ChatAttachmentViewModel
                    {
                        Id = a.Id,
                        MessageId = a.MessageId,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize
                    }).ToList() ?? new()
                }).ToList();
        }

        /// <summary>
        /// Persists a new message to the database.
        /// </summary>
        public async Task<ChatMessageViewModel?> SendMessageAsync(int senderId, SendMessageRequest request)
        {
            var message = new ChatMessage
            {
                CourseId = request.CourseId,
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                MessageText = request.MessageText,
                SentOn = DateTime.UtcNow,
                IsRead = false,
                IsDeleted = false,
                IsActive = true,
                CreatedBy = senderId,
                CreatedOn = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Reload with nav properties for the response
            var saved = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            if (saved == null) return null;

            return new ChatMessageViewModel
            {
                Id = saved.Id,
                CourseId = saved.CourseId,
                CourseName = saved.Course?.Name,
                SenderId = saved.SenderId,
                SenderName = saved.Sender?.Name,
                ReceiverId = saved.ReceiverId,
                MessageText = saved.MessageText,
                SentOn = saved.SentOn,
                IsRead = saved.IsRead
            };
        }

        /// <summary>
        /// Saves attachment metadata after the file is physically stored.
        /// </summary>
        public async Task<ChatAttachmentViewModel?> SaveAttachmentAsync(int messageId, string fileName, string filePath, string fileType, long fileSize)
        {
            var attachment = new ChatAttachment
            {
                MessageId = messageId,
                FileName = fileName,
                FilePath = filePath,
                FileType = fileType,
                FileSize = fileSize,
                UploadedOn = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = 0,
                CreatedOn = DateTime.UtcNow
            };

            _context.ChatAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            return new ChatAttachmentViewModel
            {
                Id = attachment.Id,
                MessageId = attachment.MessageId,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize
            };
        }

        /// <summary>
        /// Marks all unread messages from a partner as read.
        /// </summary>
        public async Task MarkAsReadAsync(int userId, int partnerId, int courseId)
        {
            var unread = await _context.ChatMessages
                .Where(m => m.CourseId == courseId
                    && m.SenderId == partnerId
                    && m.ReceiverId == userId
                    && !m.IsRead
                    && !m.IsDeleted)
                .ToListAsync();

            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets total unread messages for a user across all conversations.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted);
        }
    }
}
