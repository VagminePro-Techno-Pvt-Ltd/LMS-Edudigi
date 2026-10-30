using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TMS.ViewModels.Email;

namespace TMS.Web.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly EmailSettings _settings;

        public EmailTemplateService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public RenderedEmail Render(string templateName, IDictionary<string, string> data)
        {
            var dict = new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);

            string Get(string key, string defaultValue = "") =>
                dict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val) ? val : defaultValue;

            string Enc(string key, string defaultValue = "") =>
                WebUtility.HtmlEncode(Get(key, defaultValue));

            string portalUrl = !string.IsNullOrWhiteSpace(_settings.LmsPortalBaseUrl) 
                ? _settings.LmsPortalBaseUrl.TrimEnd('/') 
                : "http://localhost:5022";
            string supportContact = !string.IsNullOrWhiteSpace(_settings.SupportContact) 
                ? _settings.SupportContact 
                : "support@edudigi.com";

            return templateName switch
            {
                // ─── Phase 2: Account & Enrollment ───
                "PasswordResetOtp" => RenderPasswordResetOtp(dict, Enc, portalUrl, supportContact),
                "AccountWelcome" => RenderAccountWelcome(dict, Enc, portalUrl, supportContact),
                "PasswordChangedAlert" => RenderPasswordChangedAlert(dict, Enc, portalUrl, supportContact),
                "CourseEnrollment" => RenderCourseEnrollment(dict, Enc, portalUrl, supportContact),
                "CourseDropOrRemoval" => RenderCourseDropOrRemoval(dict, Enc, portalUrl, supportContact),

                // ─── Phase 3: Assignments ───
                "AssignmentPublication" => RenderAssignmentPublication(dict, Enc, portalUrl, supportContact),
                "AssignmentDeadlineReminder" => RenderAssignmentDeadlineReminder(dict, Enc, portalUrl, supportContact),
                "AssignmentSubmissionReceipt" => RenderAssignmentSubmissionReceipt(dict, Enc, portalUrl, supportContact),
                "AssignmentFacultySubmissionNotice" => RenderAssignmentFacultySubmissionNotice(dict, Enc, portalUrl, supportContact),
                "AssignmentGraded" => RenderAssignmentGraded(dict, Enc, portalUrl, supportContact),

                // ─── Phase 4: Attendance ───
                "AttendanceAbsence" => RenderAttendanceAbsence(dict, Enc, portalUrl, supportContact),
                "AttendanceShortage" => RenderAttendanceShortage(dict, Enc, portalUrl, supportContact),

                // ─── Phase 5: Examinations & Results ───
                "ExamPublication" => RenderExamPublication(dict, Enc, portalUrl, supportContact),
                "ExamScheduleChanged" => RenderExamScheduleChanged(dict, Enc, portalUrl, supportContact),
                "ExamReminder" => RenderExamReminder(dict, Enc, portalUrl, supportContact),
                "ExamSubmissionReceipt" => RenderExamSubmissionReceipt(dict, Enc, portalUrl, supportContact),
                "ResultsPublished" => RenderResultsPublished(dict, Enc, portalUrl, supportContact),

                // ─── Phase 6: Academic Announcements ───
                "AcademicAnnouncement" => RenderAcademicAnnouncement(dict, Enc, portalUrl, supportContact),

                _ => RenderGeneric(dict, Enc, templateName, portalUrl, supportContact)
            };
        }

        #region Template Implementations

        private RenderedEmail RenderPasswordResetOtp(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string otp = Enc("Otp", "000000");
            string name = Enc("RecipientName", "Student / Faculty");
            string subject = "EduDigiClass LMS — Password Reset OTP Verification";

            string body = $@"
<p>Hello <strong>{name}</strong>,</p>
<p>We received a request to reset the password for your EduDigiClass LMS account. Use the one-time verification code below to proceed:</p>
<div style=""text-align:center;margin:28px 0;"">
    <div style=""display:inline-block;letter-spacing:6px;font-size:32px;font-weight:700;background:#f0fdf4;color:#166534;border:2px dashed #86efac;padding:12px 28px;border-radius:10px;font-family:monospace;"">
        {otp}
    </div>
    <div style=""font-size:12px;color:#64748b;margin-top:8px;"">⚠️ Code is strictly valid for 10 minutes only. Do not share this OTP with anyone.</div>
</div>
<p style=""font-size:13px;color:#475569;"">If you did not request a password reset, please disregard this email or notify your system administrator immediately.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Password Reset Code", body, null, null, support),
                PlainTextBody = $"Your EduDigiClass LMS password reset OTP is {otp}. It is valid for 10 minutes. If you did not make this request, please ignore."
            };
        }

        private RenderedEmail RenderAccountWelcome(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Valued User");
            string role = Enc("Role", "Student");
            string email = Enc("Email", "");
            string loginUrl = $"{portalUrl}/Auth/Login";
            string subject = "Welcome to EduDigiClass LMS — Account Activated";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Your academic account on the <strong>EduDigiClass Learning Management Platform</strong> has been successfully provisioned!</p>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:18px;margin:20px 0;"">
    <table style=""width:100%;font-size:14px;border-collapse:collapse;"">
        <tr><td style=""padding:6px 0;color:#64748b;width:35%;"">Account Email:</td><td style=""padding:6px 0;font-weight:600;color:#0f172a;"">{email}</td></tr>
        <tr><td style=""padding:6px 0;color:#64748b;"">Assigned Role:</td><td style=""padding:6px 0;font-weight:600;color:#0f172a;"">{role}</td></tr>
        <tr><td style=""padding:6px 0;color:#64748b;"">Platform URL:</td><td style=""padding:6px 0;color:#2563eb;""><a href=""{loginUrl}"" style=""color:#2563eb;text-decoration:none;"">{loginUrl}</a></td></tr>
    </table>
</div>
<p>You can now sign in to access your registered courses, interactive learning materials, online assessments, and academic announcements.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Welcome to EduDigiClass", body, "Sign In to Portal", loginUrl, support),
                PlainTextBody = $"Dear {name}, welcome to EduDigiClass LMS! Your account ({email}, {role}) is active. Sign in at {loginUrl}."
            };
        }

        private RenderedEmail RenderPasswordChangedAlert(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "User");
            string time = Enc("TimestampUtc", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            string loginUrl = $"{portalUrl}/Auth/Login";
            string subject = "Security Notice: Your EduDigiClass LMS Password Was Changed";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>This is a confirmation that your account password for <strong>EduDigiClass LMS</strong> was changed on <strong>{time}</strong>.</p>
<div style=""background:#fef2f2;border-left:4px solid #ef4444;padding:14px 18px;border-radius:0 6px 6px 0;margin:20px 0;font-size:13px;color:#991b1b;"">
    <strong>Security Alert:</strong> If you did NOT initiate this password change, someone may have unauthorized access to your account. Please contact LMS support at <code>{support}</code> immediately.
</div>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Security Notification", body, "Sign In to Your Account", loginUrl, support),
                PlainTextBody = $"Dear {name}, your LMS password was changed on {time}. If you did not make this change, contact {support} immediately."
            };
        }

        private RenderedEmail RenderCourseEnrollment(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string courseName = Enc("CourseName", "Course");
            string courseCode = Enc("CourseCode", "");
            string semester = Enc("Semester", "");
            string courseId = Enc("CourseId", "0");
            string courseUrl = $"{portalUrl}/SubjectHub/{courseId}";
            string subject = $"Course Enrollment Confirmed: {courseCode} {courseName}";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>You have been officially enrolled in the following academic course for this semester:</p>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:18px;margin:20px 0;"">
    <div style=""font-size:16px;font-weight:700;color:#1e3a8a;margin-bottom:6px;"">{courseName}</div>
    <table style=""width:100%;font-size:13px;border-collapse:collapse;color:#334155;"">
        <tr><td style=""padding:4px 0;color:#64748b;width:30%;"">Course Code:</td><td style=""padding:4px 0;font-weight:600;"">{courseCode}</td></tr>
        {(string.IsNullOrWhiteSpace(semester) ? "" : $"<tr><td style=\"padding:4px 0;color:#64748b;\">Semester:</td><td style=\"padding:4px 0;\">{semester}</td></tr>")}
        <tr><td style=""padding:4px 0;color:#64748b;"">Status:</td><td style=""padding:4px 0;color:#16a34a;font-weight:600;"">Active Enrollment</td></tr>
    </table>
</div>
<p>Course syllabus, lecture notes, video materials, and assignments are now available in your Student Course Hub.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Course Enrollment", body, "Go to Course Hub", courseUrl, support),
                PlainTextBody = $"Dear {name}, you have been enrolled in {courseCode} - {courseName}. Access it at {courseUrl}."
            };
        }

        private RenderedEmail RenderCourseDropOrRemoval(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string courseName = Enc("CourseName", "Course");
            string courseCode = Enc("CourseCode", "");
            string reason = Enc("Reason", "Administrative or curriculum update");
            string subject = $"Enrollment Update: {courseCode} {courseName}";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>This is to inform you that your enrollment status in <strong>{courseCode} {courseName}</strong> has been updated (Dropped / Deactivated).</p>
<div style=""background:#fefce8;border:1px solid #fef08a;border-radius:8px;padding:14px 18px;margin:20px 0;font-size:13px;color:#854d0e;"">
    <strong>Notice:</strong> If you believe this change was made in error, please consult your academic advisor or contact student support at <code>{support}</code>.
</div>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Enrollment Status Update", body, "View My Courses", $"{portalUrl}/MyCourses", support),
                PlainTextBody = $"Dear {name}, your enrollment in {courseCode} {courseName} has been deactivated. Contact {support} for inquiries."
            };
        }

        private RenderedEmail RenderAssignmentPublication(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("AssignmentTitle", "Assignment");
            string course = Enc("CourseName", "Course");
            string dueDate = Enc("DueDate", "TBD");
            string maxMarks = Enc("MaxMarks", "100");
            string assignmentId = Enc("AssignmentId", "0");
            string assignmentUrl = $"{portalUrl}/Assignment/Details/{assignmentId}";
            string subject = $"New Assignment Published: {title} ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>A new academic assignment has been published in your course <strong>{course}</strong>:</p>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:18px;margin:20px 0;"">
    <div style=""font-size:16px;font-weight:700;color:#1e3a8a;margin-bottom:8px;"">{title}</div>
    <table style=""width:100%;font-size:13px;border-collapse:collapse;color:#334155;"">
        <tr><td style=""padding:4px 0;color:#64748b;width:30%;"">Course:</td><td style=""padding:4px 0;font-weight:600;"">{course}</td></tr>
        <tr><td style=""padding:4px 0;color:#64748b;"">Due Date & Time:</td><td style=""padding:4px 0;color:#dc2626;font-weight:700;"">{dueDate}</td></tr>
        <tr><td style=""padding:4px 0;color:#64748b;"">Maximum Marks:</td><td style=""padding:4px 0;font-weight:600;"">{maxMarks} Marks</td></tr>
    </table>
</div>
<p>Please review the assignment instructions and submit your responses before the deadline.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("New Assignment", body, "View & Submit Assignment", assignmentUrl, support),
                PlainTextBody = $"New assignment '{title}' in {course}. Due date: {dueDate}, Max marks: {maxMarks}. Submit at {assignmentUrl}."
            };
        }

        private RenderedEmail RenderAssignmentDeadlineReminder(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("AssignmentTitle", "Assignment");
            string course = Enc("CourseName", "Course");
            string dueDate = Enc("DueDate", "Upcoming");
            string hoursLeft = Enc("HoursLeft", "24");
            string assignmentId = Enc("AssignmentId", "0");
            string assignmentUrl = $"{portalUrl}/Assignment/Details/{assignmentId}";
            string subject = $"Reminder: {hoursLeft} Hours Left for Assignment '{title}' ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>This is a friendly reminder that the submission deadline for your assignment is approaching in approximately <strong>{hoursLeft} hours</strong>.</p>
<div style=""background:#fff7ed;border-left:4px solid #ea580c;padding:16px;border-radius:0 8px 8px 0;margin:20px 0;"">
    <div style=""font-size:15px;font-weight:700;color:#9a3412;"">{title}</div>
    <div style=""font-size:13px;color:#c2410c;margin-top:4px;"">Course: {course}</div>
    <div style=""font-size:13px;color:#9a3412;margin-top:4px;""><strong>Strict Deadline:</strong> {dueDate}</div>
</div>
<p>If you have already submitted your work, please verify your submission status on the assignment portal.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Assignment Deadline Reminder", body, "Submit Assignment Now", assignmentUrl, support),
                PlainTextBody = $"Reminder: {hoursLeft} hours remaining to submit '{title}' ({course}). Deadline: {dueDate}. Portal: {assignmentUrl}."
            };
        }

        private RenderedEmail RenderAssignmentSubmissionReceipt(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("AssignmentTitle", "Assignment");
            string course = Enc("CourseName", "Course");
            string submittedAt = Enc("SubmittedAt", DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"));
            string isLate = Enc("IsLate", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "Late Submission" : "On-Time";
            string assignmentId = Enc("AssignmentId", "0");
            string assignmentUrl = $"{portalUrl}/Assignment/Details/{assignmentId}";
            string subject = $"Submission Receipt: {title} ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Your submission for assignment <strong>{title}</strong> has been successfully received by the LMS platform.</p>
<div style=""background:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;padding:16px;margin:20px 0;"">
    <table style=""width:100%;font-size:13px;border-collapse:collapse;color:#166534;"">
        <tr><td style=""padding:4px 0;width:35%;"">Assignment:</td><td style=""padding:4px 0;font-weight:700;"">{title}</td></tr>
        <tr><td style=""padding:4px 0;"">Course:</td><td style=""padding:4px 0;"">{course}</td></tr>
        <tr><td style=""padding:4px 0;"">Submission Timestamp:</td><td style=""padding:4px 0;font-weight:600;"">{submittedAt}</td></tr>
        <tr><td style=""padding:4px 0;"">Status:</td><td style=""padding:4px 0;font-weight:700;"">{isLate}</td></tr>
    </table>
</div>
<p>Your faculty will review your work once the evaluation window opens. You will receive an email notification when marks and feedback are published.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Submission Receipt", body, "View Submission", assignmentUrl, support),
                PlainTextBody = $"Receipt: Your submission for '{title}' ({course}) was received on {submittedAt}. Status: {isLate}."
            };
        }

        private RenderedEmail RenderAssignmentFacultySubmissionNotice(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string facultyName = Enc("RecipientName", "Faculty");
            string studentName = Enc("StudentName", "Student");
            string title = Enc("AssignmentTitle", "Assignment");
            string course = Enc("CourseName", "Course");
            string submittedAt = Enc("SubmittedAt", DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"));
            string assignmentId = Enc("AssignmentId", "0");
            string evalUrl = $"{portalUrl}/Assignment/Submissions/{assignmentId}";
            string subject = $"New Student Submission: {title} by {studentName}";

            string body = $@"
<p>Dear <strong>{facultyName}</strong>,</p>
<p>A student has submitted work for your assignment <strong>{title}</strong> in course <strong>{course}</strong>:</p>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:20px 0;"">
    <div><strong>Student:</strong> {studentName}</div>
    <div style=""margin-top:4px;""><strong>Submitted At:</strong> {submittedAt}</div>
</div>
<p>You can access all student submissions and evaluate them via the faculty assignment evaluation dashboard.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Assignment Submission Notification", body, "Open Evaluation Portal", evalUrl, support),
                PlainTextBody = $"Student {studentName} submitted '{title}' ({course}) on {submittedAt}. Evaluate at {evalUrl}."
            };
        }

        private RenderedEmail RenderAssignmentGraded(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("AssignmentTitle", "Assignment");
            string course = Enc("CourseName", "Course");
            string marks = Enc("MarksObtained", "0");
            string maxMarks = Enc("MaxMarks", "100");
            string feedback = Enc("Feedback", "No additional remarks.");
            string assignmentId = Enc("AssignmentId", "0");
            string gradeUrl = $"{portalUrl}/Assignment/Details/{assignmentId}";
            string subject = $"Grades Published: {title} ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Your assignment <strong>{title}</strong> in <strong>{course}</strong> has been evaluated. Your marks and feedback are now published:</p>
<div style=""background:#eff6ff;border:1px solid #bfdbfe;border-radius:8px;padding:18px;margin:20px 0;text-align:center;"">
    <div style=""font-size:13px;color:#1e40af;text-transform:uppercase;letter-spacing:1px;font-weight:600;"">Marks Awarded</div>
    <div style=""font-size:32px;font-weight:800;color:#1e3a8a;margin:6px 0;"">{marks} <span style=""font-size:18px;font-weight:500;color:#64748b;"">/ {maxMarks}</span></div>
    <div style=""background:#ffffff;border-radius:6px;padding:12px;margin-top:14px;text-align:left;font-size:13px;color:#334155;border:1px solid #e2e8f0;"">
        <strong>Faculty Feedback:</strong><br/>{feedback}
    </div>
</div>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Assignment Evaluation Result", body, "View Detailed Grade", gradeUrl, support),
                PlainTextBody = $"Dear {name}, marks for '{title}' ({course}) have been published: {marks}/{maxMarks}. Feedback: {feedback}."
            };
        }

        private RenderedEmail RenderAttendanceAbsence(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string date = Enc("Date", DateTime.Today.ToString("dd/MM/yyyy"));
            string attendanceUrl = $"{portalUrl}/Home/StudentDashboard";
            string subject = $"Attendance Alert: Marked Absent on {date}";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>This is an automated notification from the EduDigiClass attendance management system. You were recorded as <strong>ABSENT</strong> on <strong>{date}</strong>.</p>
<div style=""background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:16px;margin:20px 0;font-size:13px;color:#991b1b;"">
    <strong>Attendance Notice:</strong> Regular attendance is mandatory across all enrolled courses. A minimum of 75% attendance is required to remain eligible for semester examinations.
</div>
<p>If you attended sessions or experienced a technical recording discrepancy, please contact your course faculty or academic department.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Daily Attendance Alert", body, "Check Attendance Record", attendanceUrl, support),
                PlainTextBody = $"Dear {name}, you were recorded as ABSENT on {date} on EduDigiClass LMS. Regular attendance is required."
            };
        }

        private RenderedEmail RenderAttendanceShortage(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string currentPercentage = Enc("CurrentPercentage", "0.0");
            string threshold = Enc("Threshold", "75.0");
            string totalSessions = Enc("TotalSessions", "0");
            string attendedSessions = Enc("AttendedSessions", "0");
            string attendanceUrl = $"{portalUrl}/Home/StudentDashboard";
            string subject = $"Urgent: Attendance Shortage Warning ({currentPercentage}% Attendance)";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Your aggregate attendance in EduDigiClass LMS has fallen below the mandatory <strong>{threshold}%</strong> university threshold:</p>
<div style=""background:#fff1f2;border:2px solid #fda4af;border-radius:10px;padding:20px;margin:20px 0;text-align:center;"">
    <div style=""font-size:12px;font-weight:700;color:#e11d48;text-transform:uppercase;letter-spacing:1px;"">Current Attendance Percentage</div>
    <div style=""font-size:36px;font-weight:800;color:#9f1239;margin:8px 0;"">{currentPercentage}%</div>
    <div style=""font-size:13px;color:#881337;"">Sessions Attended: <strong>{attendedSessions}</strong> of <strong>{totalSessions}</strong> recorded sessions</div>
</div>
<p style=""font-size:13px;color:#475569;"">Failure to achieve the required 75% attendance before the end of the academic term will result in examination debarment as per institutional academic regulations.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Attendance Shortage Warning", body, "View Detailed Attendance", attendanceUrl, support),
                PlainTextBody = $"Urgent Attendance Warning: Your attendance is currently {currentPercentage}% (below {threshold}%). Attended {attendedSessions}/{totalSessions} sessions."
            };
        }

        private RenderedEmail RenderExamPublication(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("ExamTitle", "Semester Examination");
            string course = Enc("CourseName", "Course");
            string date = Enc("ExamDate", "Upcoming");
            string duration = Enc("DurationMinutes", "60");
            string isSeb = Enc("RequiresSeb", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "Yes (Safe Exam Browser Mandatory)" : "Standard Browser";
            string examUrl = $"{portalUrl}/StudentExam/Index";
            string subject = $"Examination Notice: {title} ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>An official examination has been published for your course <strong>{course}</strong>:</p>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:18px;margin:20px 0;"">
    <div style=""font-size:16px;font-weight:700;color:#1e3a8a;margin-bottom:8px;"">{title}</div>
    <table style=""width:100%;font-size:13px;border-collapse:collapse;color:#334155;"">
        <tr><td style=""padding:4px 0;color:#64748b;width:35%;"">Course:</td><td style=""padding:4px 0;font-weight:600;"">{course}</td></tr>
        <tr><td style=""padding:4px 0;color:#64748b;"">Scheduled Time:</td><td style=""padding:4px 0;color:#1e3a8a;font-weight:700;"">{date}</td></tr>
        <tr><td style=""padding:4px 0;color:#64748b;"">Duration:</td><td style=""padding:4px 0;"">{duration} Minutes</td></tr>
        <tr><td style=""padding:4px 0;color:#64748b;"">Browser Mode:</td><td style=""padding:4px 0;color:#b45309;font-weight:600;"">{isSeb}</td></tr>
    </table>
</div>
<p>Ensure that you test your internet connection, webcam, and required browser configuration ahead of time.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Examination Schedule", body, "Open Examination Portal", examUrl, support),
                PlainTextBody = $"Exam notice: '{title}' ({course}) scheduled for {date}. Duration: {duration} mins. SEB: {isSeb}. Portal: {examUrl}."
            };
        }

        private RenderedEmail RenderExamScheduleChanged(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("ExamTitle", "Examination");
            string course = Enc("CourseName", "Course");
            string oldSchedule = Enc("OldSchedule", "Previous Date/Time");
            string newSchedule = Enc("NewSchedule", "Revised Date/Time");
            string examUrl = $"{portalUrl}/StudentExam/Index";
            string subject = $"Schedule Rescheduled: {title} ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Please note that the examination schedule for <strong>{title}</strong> in <strong>{course}</strong> has been revised:</p>
<div style=""background:#fffbeb;border:1px solid #fde68a;border-radius:8px;padding:16px;margin:20px 0;"">
    <div style=""font-size:13px;color:#78350f;text-decoration:line-through;"">Previous Schedule: {oldSchedule}</div>
    <div style=""font-size:15px;font-weight:700;color:#92400e;margin-top:6px;"">Revised Schedule: {newSchedule}</div>
</div>
<p>Please update your academic calendar accordingly.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Examination Rescheduled", body, "View Updated Schedule", examUrl, support),
                PlainTextBody = $"Schedule updated for exam '{title}' ({course}). Previous: {oldSchedule}. Revised: {newSchedule}."
            };
        }

        private RenderedEmail RenderExamReminder(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("ExamTitle", "Examination");
            string course = Enc("CourseName", "Course");
            string startTime = Enc("StartTime", "Upcoming");
            string hoursLeft = Enc("HoursLeft", "2");
            bool isSeb = Enc("RequiresSeb", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            string examUrl = $"{portalUrl}/StudentExam/Index";
            string subject = $"Exam Reminder: {title} starts in {hoursLeft} hours";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Your examination <strong>{title}</strong> ({course}) is scheduled to commence in approximately <strong>{hoursLeft} hours</strong> at <strong>{startTime}</strong>.</p>
<div style=""background:#f1f5f9;border-left:4px solid #0284c7;padding:16px;border-radius:0 8px 8px 0;margin:20px 0;font-size:13px;"">
    <strong>Pre-Exam Checklist:</strong>
    <ul style=""margin:8px 0 0 16px;padding:0;color:#334155;"">
        <li>Log in 15 minutes before exam start time.</li>
        {(isSeb ? "<li>Ensure Safe Exam Browser (SEB) is installed and tested.</li>" : "<li>Use a supported, updated browser (Chrome/Edge/Firefox).</li>")}
        <li>Maintain a stable internet connection.</li>
    </ul>
</div>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Upcoming Examination Reminder", body, "Launch Exam Dashboard", examUrl, support),
                PlainTextBody = $"Exam Reminder: '{title}' ({course}) starts in {hoursLeft} hours at {startTime}. Portal: {examUrl}."
            };
        }

        private RenderedEmail RenderExamSubmissionReceipt(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("ExamTitle", "Examination");
            string course = Enc("CourseName", "Course");
            string submittedAt = Enc("SubmittedAt", DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"));
            string attemptRef = Enc("AttemptRef", Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper());
            string examUrl = $"{portalUrl}/StudentExam/Index";
            string subject = $"Exam Submission Receipt: {title} ({course})";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>Your responses for <strong>{title}</strong> ({course}) were successfully submitted and securely logged in the EduDigiClass LMS database.</p>
<div style=""background:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;padding:16px;margin:20px 0;font-size:13px;color:#166534;"">
    <div><strong>Submission Reference ID:</strong> <code style=""font-weight:700;"">{attemptRef}</code></div>
    <div style=""margin-top:6px;""><strong>Submitted At:</strong> {submittedAt}</div>
</div>
<p>Official examination grades will be announced following faculty evaluation and committee approval.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Exam Submission Receipt", body, "View Exam Records", examUrl, support),
                PlainTextBody = $"Receipt: Exam '{title}' ({course}) submitted successfully at {submittedAt}. Reference ID: {attemptRef}."
            };
        }

        private RenderedEmail RenderResultsPublished(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student");
            string title = Enc("AssessmentTitle", "Assessment / Examination");
            string course = Enc("CourseName", "Course");
            string marks = Enc("MarksObtained", "");
            string maxMarks = Enc("MaxMarks", "");
            string grade = Enc("Grade", "Pass");
            string gradeCardUrl = $"{portalUrl}/InternalGradeCard";
            string subject = $"Official Result Published: {title} ({course})";

            string marksRow = !string.IsNullOrWhiteSpace(marks) && !string.IsNullOrWhiteSpace(maxMarks)
                ? $"<tr><td style=\"padding:6px 0;color:#64748b;\">Marks Awarded:</td><td style=\"padding:6px 0;font-weight:700;\">{marks} / {maxMarks}</td></tr>"
                : "";

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>The official academic results for <strong>{title}</strong> in <strong>{course}</strong> have been published.</p>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:18px;margin:20px 0;"">
    <table style=""width:100%;font-size:14px;border-collapse:collapse;color:#1e293b;"">
        <tr><td style=""padding:6px 0;color:#64748b;width:35%;"">Assessment:</td><td style=""padding:6px 0;font-weight:600;"">{title}</td></tr>
        <tr><td style=""padding:6px 0;color:#64748b;"">Course:</td><td style=""padding:6px 0;"">{course}</td></tr>
        {marksRow}
        <tr><td style=""padding:6px 0;color:#64748b;"">Academic Grade / Status:</td><td style=""padding:6px 0;font-weight:700;color:#1e3a8a;"">{grade}</td></tr>
    </table>
</div>
<p>You can view your complete cumulative UGC 10-point grade card on the student portal.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Official Academic Result", body, "Open Grade Card", gradeCardUrl, support),
                PlainTextBody = $"Official Results Published: {title} ({course}). Grade/Status: {grade}. Check Grade Card at {gradeCardUrl}."
            };
        }

        private RenderedEmail RenderAcademicAnnouncement(Dictionary<string, string> d, Func<string, string, string> Enc, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Student / Faculty");
            string title = Enc("AnnouncementTitle", "Academic Announcement");
            string priority = Enc("Priority", "Normal");
            string preview = Enc("BodyPreview", "");
            string announcementId = Enc("AnnouncementId", "0");
            string announcementUrl = $"{portalUrl}/Announcement";
            string subject = $"[Announcement] {title}";

            string priorityColor = priority.ToLowerInvariant() switch
            {
                "high" or "urgent" => "#ef4444",
                "medium" => "#f59e0b",
                _ => "#3b82f6"
            };

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<div style=""display:inline-block;background:{priorityColor};color:#ffffff;font-size:11px;font-weight:700;padding:3px 10px;border-radius:12px;text-transform:uppercase;margin-bottom:12px;"">
    {priority} Notice
</div>
<div style=""font-size:18px;font-weight:700;color:#0f172a;margin-bottom:12px;"">{title}</div>
<div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:16px 0;font-size:14px;color:#334155;line-height:1.6;"">
    {preview}
</div>
<p>Please log in to the announcement board to view the complete institutional circular.</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout("Academic Announcement", body, "View Announcement", announcementUrl, support),
                PlainTextBody = $"[Announcement: {priority}] {title}: {preview}. Read more at {announcementUrl}."
            };
        }

        private RenderedEmail RenderGeneric(Dictionary<string, string> d, Func<string, string, string> Enc, string template, string portalUrl, string support)
        {
            string name = Enc("RecipientName", "Valued User");
            string subject = Enc("Subject", "EduDigiClass LMS Notification");
            string message = Enc("Message", "You have a new academic notification.");

            string body = $@"
<p>Dear <strong>{name}</strong>,</p>
<p>{message}</p>";

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = WrapLayout(subject, body, "Go to LMS Portal", portalUrl, support),
                PlainTextBody = $"{subject}: {message} Portal: {portalUrl}"
            };
        }

        #endregion

        #region Master Layout Wrapper

        private static string WrapLayout(string heading, string contentHtml, string? ctaText, string? ctaUrl, string supportContact)
        {
            string ctaBlock = !string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl)
                ? $@"
<div style=""text-align:center;margin:32px 0 16px 0;"">
    <a href=""{ctaUrl}"" style=""display:inline-block;background:#1e3a8a;color:#ffffff;font-size:14px;font-weight:600;padding:12px 28px;text-decoration:none;border-radius:6px;box-shadow:0 2px 4px rgba(30,58,138,0.25);"">
        {WebUtility.HtmlEncode(ctaText)} &rarr;
    </a>
</div>"
                : "";

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{WebUtility.HtmlEncode(heading)}</title>
</head>
<body style=""margin:0;padding:24px 10px;background:#f1f5f9;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;-webkit-font-smoothing:antialiased;"">
    <div style=""max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.06);"">
        <!-- Header -->
        <div style=""background:linear-gradient(135deg,#0f172a 0%,#1e3a8a 100%);padding:28px 32px;color:#ffffff;"">
            <div style=""font-size:22px;font-weight:800;letter-spacing:-0.5px;color:#ffffff;"">EduDigiClass LMS</div>
            <div style=""font-size:12px;color:#93c5fd;text-transform:uppercase;letter-spacing:1px;font-weight:600;margin-top:2px;"">Official Academic Notification</div>
        </div>

        <!-- Body Content -->
        <div style=""padding:32px;color:#1e293b;line-height:1.6;font-size:14px;"">
            <h2 style=""margin:0 0 18px 0;font-size:18px;font-weight:700;color:#0f172a;"">{WebUtility.HtmlEncode(heading)}</h2>
            {contentHtml}
            {ctaBlock}
        </div>

        <!-- Footer -->
        <div style=""background:#f8fafc;padding:20px 32px;border-top:1px solid #e2e8f0;font-size:12px;color:#64748b;line-height:1.5;text-align:center;"">
            <div>This is an automated institutional notification from <strong>EduDigiClass LMS</strong>. Please do not reply directly.</div>
            <div style=""margin-top:6px;"">Need assistance? Contact support at <a href=""mailto:{supportContact}"" style=""color:#2563eb;text-decoration:none;"">{supportContact}</a></div>
            <div style=""margin-top:10px;font-size:11px;color:#94a3b8;"">&copy; {DateTime.UtcNow.Year} EduDigiClass. All rights reserved.</div>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }
}
