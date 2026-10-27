using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMS.Common;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    /// <summary>
    /// Implements official LMS academic data retrieval directly from SQL Server database entities.
    /// Operates without any dependency on external LLMs or API keys.
    /// </summary>
    public class DbAcademicRecordsService : IAcademicRecordsService
    {
        private readonly ApplicationDBContext _db;
        private readonly IStudentLearningService _learningService;
        private readonly ILogger<DbAcademicRecordsService>? _logger;

        public DbAcademicRecordsService(
            ApplicationDBContext db,
            IStudentLearningService learningService,
            ILogger<DbAcademicRecordsService>? logger = null)
        {
            _db = db;
            _learningService = learningService;
            _logger = logger;
        }

        public async Task<StudentAttendanceSummaryDto> GetAttendanceSummaryAsync(int studentId, int? courseId = null)
        {
            var summary = new StudentAttendanceSummaryDto
            {
                StudentId = studentId
            };

            if (studentId <= 0)
            {
                summary.SummaryText = "Student ID is invalid or missing.";
                return summary;
            }

            try
            {
                // 1. Platform-level Daily Attendance records (Login-based attendance)
                var dailyAtt = await _db.DailyAttendances
                    .AsNoTracking()
                    .Where(a => a.StudentId == studentId && a.IsActive)
                    .ToListAsync();

                // 2. Classroom-level Attendance records (ClassInstance-based attendance)
                var classAttQuery = _db.ClassAttendances
                    .AsNoTracking()
                    .Include(ca => ca.ClassInstance)
                    .Where(ca => ca.StudentId == studentId && ca.IsActive);

                if (courseId.HasValue && courseId.Value > 0)
                {
                    classAttQuery = classAttQuery.Where(ca => ca.ClassInstance != null && ca.ClassInstance.CourseId == courseId.Value);
                    var courseObj = await _db.CourseMasters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId.Value);
                    summary.CourseName = courseObj?.Name;
                }

                var classAtt = await classAttQuery.ToListAsync();

                summary.DailyLoginSessions = dailyAtt.Count;
                summary.ClassroomSessions = classAtt.Count;
                summary.TotalClasses = dailyAtt.Count + classAtt.Count;

                summary.AttendedClasses = dailyAtt.Count(a => a.AttendanceStatus == 1) +
                                         classAtt.Count(a => a.AttendanceStatus == 1);

                summary.AbsentClasses = dailyAtt.Count(a => a.AttendanceStatus == 0) +
                                       classAtt.Count(a => a.AttendanceStatus == 0);

                summary.LateClasses = dailyAtt.Count(a => a.AttendanceStatus == 2) +
                                     classAtt.Count(a => a.AttendanceStatus == 2);

                summary.ExcusedClasses = dailyAtt.Count(a => a.AttendanceStatus == 3) +
                                        classAtt.Count(a => a.AttendanceStatus == 3);

                if (summary.TotalClasses > 0)
                {
                    summary.AttendancePercentage = Math.Round((double)summary.AttendedClasses * 100.0 / summary.TotalClasses, 1);
                    var periodLabel = !string.IsNullOrWhiteSpace(summary.CourseName)
                        ? $"for **{summary.CourseName}**"
                        : "across all recorded reporting sessions";

                    summary.SummaryText = $"Your attendance is **{summary.AttendancePercentage}%** {periodLabel}. " +
                                          $"Attended: **{summary.AttendedClasses}** classes; total recorded classes: **{summary.TotalClasses}**.";
                }
                else
                {
                    summary.AttendancePercentage = null;
                    summary.SummaryText = "No attendance records have been registered for your account in the LMS database yet. " +
                                          "Daily platform attendance is recorded automatically on your first morning login, " +
                                          "and lecture attendance is recorded by faculty or Zoom webhooks during scheduled live sessions.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving attendance summary for student {StudentId}", studentId);
                summary.SummaryText = "An error occurred while fetching your official attendance records from the LMS database.";
            }

            return summary;
        }

        public async Task<StudentGradesSummaryDto> GetGradesSummaryAsync(int studentId, int? courseId = null)
        {
            var summary = new StudentGradesSummaryDto
            {
                StudentId = studentId
            };

            if (studentId <= 0) return summary;

            try
            {
                var enrolledCourseIds = await _learningService.GetEnrolledCourseIdsAsync(studentId);
                if (courseId.HasValue && courseId.Value > 0 && enrolledCourseIds.Contains(courseId.Value))
                {
                    enrolledCourseIds = new List<int> { courseId.Value };
                }

                if (!enrolledCourseIds.Any())
                {
                    summary.SummaryText = "You are not currently enrolled in any active courses with published grade cards.";
                    return summary;
                }

                var courseLookup = await _db.CourseMasters
                    .AsNoTracking()
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);

                // 1. Fetch published Internal Assessments with published results
                var assessments = await _db.InternalAssessments
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted && a.IsPublished && a.ResultsPublished && enrolledCourseIds.Contains(a.CourseId))
                    .ToListAsync();

                var assessmentIds = assessments.Select(a => a.Id).ToList();

                var submissions = await _db.InternalAssessmentSubmissions
                    .AsNoTracking()
                    .Where(s => assessmentIds.Contains(s.AssessmentId) && s.StudentId == studentId)
                    .ToListAsync();

                foreach (var a in assessments)
                {
                    courseLookup.TryGetValue(a.CourseId, out var courseObj);
                    var sub = submissions.FirstOrDefault(s => s.AssessmentId == a.Id);
                    var gradeItem = new StudentCourseGradeItemDto
                    {
                        CourseId = a.CourseId,
                        CourseName = courseObj?.Name ?? "Course",
                        CourseCode = courseObj?.CourseCode ?? "N/A",
                        AssessmentTitle = a.Title,
                        MaxMarks = a.MaxMarks
                    };

                    if (sub != null && sub.Marks.HasValue && sub.EvaluatedBy.HasValue)
                    {
                        gradeItem.MarksObtained = sub.Marks.Value;
                        gradeItem.EvaluatedOn = sub.EvaluatedOn;
                        gradeItem.ResultStatus = InternalAssessmentResultPolicy.IsPass(sub.Marks.Value, a.MaxMarks) ? "PASS" : "FAIL";
                        summary.TotalEvaluated++;
                        if (gradeItem.ResultStatus == "PASS") summary.TotalPassed++;
                    }
                    else if (sub != null)
                    {
                        gradeItem.ResultStatus = "PENDING";
                        summary.TotalPending++;
                    }
                    else
                    {
                        gradeItem.ResultStatus = "ABSENT / FAIL";
                        summary.TotalEvaluated++;
                    }

                    summary.CourseGrades.Add(gradeItem);
                }

                // 2. Fetch Exam Attempts
                var examAttempts = await _db.StudentExamAttempts
                    .AsNoTracking()
                    .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                    .Where(ea => ea.StudentId == studentId && ea.IsSubmitted && ea.Exam != null && enrolledCourseIds.Contains(ea.Exam.CourseId))
                    .ToListAsync();

                foreach (var ea in examAttempts)
                {
                    if (ea.Exam == null) continue;
                    var score = ea.Score ?? (ea.Percentage.HasValue ? (ea.Percentage.Value * ea.Exam.TotalMarks / 100m) : 0m);
                    summary.CourseGrades.Add(new StudentCourseGradeItemDto
                    {
                        CourseId = ea.Exam.CourseId,
                        CourseName = ea.Exam.Course?.Name ?? "Exam Course",
                        CourseCode = ea.Exam.Course?.CourseCode ?? "N/A",
                        AssessmentTitle = ea.Exam.Title,
                        MarksObtained = score,
                        MaxMarks = ea.Exam.TotalMarks,
                        ResultStatus = ea.ResultStatus?.ToUpperInvariant() ?? (score >= ea.Exam.PassingMarks ? "PASS" : "FAIL"),
                        EvaluatedOn = ea.SubmittedAt ?? ea.StartedAt
                    });
                    summary.TotalEvaluated++;
                    if (score >= ea.Exam.PassingMarks) summary.TotalPassed++;
                }

                if (summary.CourseGrades.Any())
                {
                    summary.SummaryText = $"Found **{summary.CourseGrades.Count}** published academic assessment(s) " +
                                          $"({summary.TotalPassed} Passed, {summary.TotalEvaluated - summary.TotalPassed} Needs Improvement / Absent, {summary.TotalPending} Pending Evaluation).";
                }
                else
                {
                    summary.SummaryText = "No evaluated grade or examination results have been published for your enrolled courses yet. " +
                                          "When faculty publish marks, your official statements will appear in [My Grade Card](/InternalGradeCard/Index).";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving grades summary for student {StudentId}", studentId);
                summary.SummaryText = "An error occurred while loading your grade records from the LMS database.";
            }

            return summary;
        }

        public async Task<List<EnrolledCourseSummaryDto>> GetEnrolledCoursesSummaryAsync(int studentId)
        {
            if (studentId <= 0) return new List<EnrolledCourseSummaryDto>();

            try
            {
                return await _db.CourseEnrollments
                    .AsNoTracking()
                    .Include(e => e.Course)
                    .ThenInclude(c => c.CourseCategory)
                    .Where(e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == (int)EnrollmentStatus.Active))
                    .Select(e => new EnrolledCourseSummaryDto
                    {
                        CourseId = e.CourseId,
                        CourseCode = e.Course != null ? (e.Course.CourseCode ?? "N/A") : "N/A",
                        CourseName = e.Course != null ? (e.Course.Name ?? "Untitled Course") : "Untitled Course",
                        CategoryName = e.Course != null && e.Course.CourseCategory != null ? e.Course.CourseCategory.Name : null,
                        SemesterId = e.Course != null ? e.Course.SemesterId : null
                    })
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving enrolled courses for student {StudentId}", studentId);
                return new List<EnrolledCourseSummaryDto>();
            }
        }

        public async Task<StudentAssignmentsSummaryDto> GetAssignmentsSummaryAsync(int studentId, int? courseId = null)
        {
            var summary = new StudentAssignmentsSummaryDto
            {
                StudentId = studentId
            };

            if (studentId <= 0) return summary;

            try
            {
                var enrollments = await _db.CourseEnrollments
                    .AsNoTracking()
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == (int)EnrollmentStatus.Active))
                    .ToListAsync();

                var enrolledCourseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
                var enrolledProgramIds = enrollments
                    .Where(e => e.Course != null && e.Course.CourseCategoryId > 0)
                    .Select(e => e.Course!.CourseCategoryId)
                    .Distinct()
                    .ToList();

                var query = _db.AssignmentMasters
                    .AsNoTracking()
                    .Include(a => a.Course)
                    .Where(a => !a.IsDeleted);

                if (courseId.HasValue && courseId.Value > 0)
                {
                    query = query.Where(a => a.CourseId == courseId.Value);
                }
                else
                {
                    query = query.Where(a => (a.CourseId.HasValue && enrolledCourseIds.Contains(a.CourseId.Value)) ||
                                             enrolledProgramIds.Contains(a.ProgramId));
                }

                var assignments = await query.OrderBy(a => a.DueDate).ToListAsync();
                var assignmentIds = assignments.Select(a => a.Id).ToList();

                var submissions = await _db.AssignmentSubmissions
                    .AsNoTracking()
                    .Where(s => assignmentIds.Contains(s.AssignmentId) && s.StudentId == studentId && !s.IsDeleted)
                    .ToListAsync();

                var submissionIds = submissions.Select(s => s.Id).ToList();
                var grades = await _db.AssignmentGrades
                    .AsNoTracking()
                    .Where(g => submissionIds.Contains(g.SubmissionId) && !g.IsDeleted)
                    .ToListAsync();

                foreach (var a in assignments)
                {
                    var sub = submissions.FirstOrDefault(s => s.AssignmentId == a.Id);
                    var item = new StudentAssignmentItemDto
                    {
                        AssignmentId = a.Id,
                        Title = a.Title,
                        CourseName = a.Course?.Name ?? "General Assignment",
                        DueDate = a.DueDate,
                        MaxMarks = a.MaxMarks
                    };

                    if (sub != null)
                    {
                        var grade = grades.FirstOrDefault(g => g.SubmissionId == sub.Id);
                        item.Status = sub.Status ?? "Submitted";
                        item.Marks = grade?.Marks;
                        summary.SubmittedAssignments.Add(item);
                    }
                    else
                    {
                        item.Status = "Pending";
                        summary.PendingAssignments.Add(item);
                    }
                }

                if (summary.HasRecords)
                {
                    summary.SummaryText = $"You have **{summary.PendingAssignments.Count}** pending assignment(s) and " +
                                          $"**{summary.SubmittedAssignments.Count}** submitted assignment(s).";
                }
                else
                {
                    summary.SummaryText = "No active assignments are currently assigned for your enrolled courses.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving assignments for student {StudentId}", studentId);
                summary.SummaryText = "An error occurred while loading assignments from the LMS database.";
            }

            return summary;
        }

        public async Task<StudentScheduleSummaryDto> GetUpcomingScheduleAsync(int studentId, int? courseId = null)
        {
            var summary = new StudentScheduleSummaryDto
            {
                StudentId = studentId
            };

            if (studentId <= 0) return summary;

            try
            {
                var enrolledCourseIds = await _learningService.GetEnrolledCourseIdsAsync(studentId);
                if (courseId.HasValue && courseId.Value > 0 && enrolledCourseIds.Contains(courseId.Value))
                {
                    enrolledCourseIds = new List<int> { courseId.Value };
                }

                var courseLookup = await _db.CourseMasters
                    .AsNoTracking()
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);

                // 1. Live Class Instances
                var instances = await _db.ClassInstances
                    .AsNoTracking()
                    .Include(ci => ci.Course)
                    .Where(ci => ci.IsActive && enrolledCourseIds.Contains(ci.CourseId) &&
                                 ci.ScheduledStart.Date >= DateTime.Today && ci.Status != 3)
                    .OrderBy(ci => ci.ScheduledStart)
                    .Take(5)
                    .ToListAsync();

                foreach (var ci in instances)
                {
                    summary.UpcomingSessions.Add(new StudentScheduleItemDto
                    {
                        Title = ci.Title,
                        CourseName = ci.Course?.Name ?? "Course Lecture",
                        ScheduledStart = ci.ScheduledStart,
                        ScheduledEnd = ci.ScheduledEnd,
                        SessionType = "Class Lecture",
                        Status = ci.Status == 1 ? "Live Now" : "Scheduled"
                    });
                }

                // 2. Scheduled Zoom / Virtual Meetings
                var meetings = await _db.CourseMeeting
                    .AsNoTracking()
                    .Where(m => m.IsActive && (enrolledCourseIds.Contains(m.CourseId) || m.CourseId == 0) &&
                                 m.ScheduledAt >= DateTime.Now)
                    .OrderBy(m => m.ScheduledAt)
                    .Take(5)
                    .ToListAsync();

                foreach (var m in meetings)
                {
                    courseLookup.TryGetValue(m.CourseId, out var cObj);
                    summary.UpcomingSessions.Add(new StudentScheduleItemDto
                    {
                        Title = m.MeetingTitle ?? "Virtual Meeting",
                        CourseName = cObj?.Name ?? "Virtual Meeting",
                        ScheduledStart = m.ScheduledAt,
                        ScheduledEnd = m.ScheduledAt.AddMinutes(m.Duration),
                        SessionType = "Virtual Classroom",
                        Status = "Scheduled"
                    });
                }

                summary.UpcomingSessions = summary.UpcomingSessions.OrderBy(s => s.ScheduledStart).Take(6).ToList();

                if (summary.HasRecords)
                {
                    var next = summary.UpcomingSessions.First();
                    summary.SummaryText = $"Your next session is **{next.Title}** scheduled for **{next.ScheduledStart:ddd, dd MMM hh:mm tt}**.";
                }
                else
                {
                    summary.SummaryText = "No upcoming classes or live meetings are currently scheduled in your LMS calendar for the next few days.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving schedule for student {StudentId}", studentId);
                summary.SummaryText = "An error occurred while loading your schedule from the LMS database.";
            }

            return summary;
        }

        public async Task<StudentProfileSummaryDto> GetProfileSummaryAsync(int studentId, int? courseId = null)
        {
            var summary = new StudentProfileSummaryDto
            {
                StudentId = studentId
            };

            if (studentId <= 0) return summary;

            try
            {
                var user = await _db.UserMasters
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == studentId);

                if (user != null)
                {
                    summary.StudentName = user.Name ?? "Student";
                    summary.Email = user.Email ?? "N/A";
                    summary.ContactNo = user.ContactNo;
                    summary.RollNumber = $"220164013{studentId:D4}";

                    var enrollments = await _db.CourseEnrollments
                        .AsNoTracking()
                        .Include(e => e.Course)
                        .ThenInclude(c => c.CourseCategory)
                        .Where(e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == (int)EnrollmentStatus.Active))
                        .ToListAsync();

                    var firstProgram = enrollments.FirstOrDefault(e => e.Course?.CourseCategory != null)?.Course?.CourseCategory?.Name;
                    summary.ProgramName = firstProgram ?? "EduDigi LMS";

                    if (courseId.HasValue && courseId.Value > 0)
                    {
                        summary.OverallProgressPercent = await _learningService.GetCourseProgressPercentAsync(studentId, courseId.Value);
                    }

                    summary.SummaryText = $"Student: **{summary.StudentName}** | Roll No: **{summary.RollNumber}** | Program: **{summary.ProgramName}**";
                }
                else
                {
                    summary.SummaryText = "Profile information could not be found for the specified student.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving profile for student {StudentId}", studentId);
                summary.SummaryText = "An error occurred while loading profile details from the LMS database.";
            }

            return summary;
        }
    }
}
