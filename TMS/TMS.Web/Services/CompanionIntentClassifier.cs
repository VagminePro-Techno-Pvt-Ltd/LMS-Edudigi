using System;
using System.Text.RegularExpressions;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    /// <summary>
    /// Lightweight, deterministic intent matcher that categorizes student queries into:
    /// - Database-backed queries (Attendance, Grades, Courses, Assignments, Schedule, Profile)
    /// - AI-powered learning queries (Conceptual tutoring, summaries, problem solving, practice questions)
    /// Never invokes an external LLM for straightforward LMS data inquiries.
    /// Resilient against common student typos (e.g. "attendace", "asignment", "calender").
    /// </summary>
    public static class CompanionIntentClassifier
    {
        // Matches "attendance", "attendace", "attendence", "atendance", "attandance", "absent", "present", "haazri", etc.
        private static readonly Regex AttendanceRegex = new(
            @"(^|\b)(at+[ea]n+[ds]\w*|at+n+d\w*|absent\w*|absenc\w*|present\w*|presenc\w*|classes?\s+attended|miss\w*\s+classes?|how\s+many\s+classes|bunk\w*|haaz?ri\w*|haziri)(\b|$)|(حاضری|غیر\s*حاضر)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches "grade", "grades", "grad", "marks", "scores", "results", "grade card", "gpa", "cgpa", etc.
        private static readonly Regex GradesRegex = new(
            @"(^|\b)(grad\w*|marks?|marx|scores?|results?|reslt\w*|gpa|cgpa|transcript\w*|grade\s*card|internal\s+assessment\s+marks?|passing\s+marks?|my\s+grade|my\s+marks|my\s+result|exam\s+score|did\s+i\s+pass)(\b|$)|(نمبر|مارکس|نتیجہ|گریڈ)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches "my courses", "enrolled courses", "course list", "registered courses", etc.
        private static readonly Regex CoursesRegex = new(
            @"(^|\b)(enrolled\s+courses?|my\s+courses?|what\s+courses?|which\s+courses?|course\s+list|list\s+of\s+courses?|subjects?\s+enrolled|registered\s+courses?|courses?\s+(am\s+i\s+)?(taking|enrolled|registered)|my\s+subjects?)(\b|$)|(میرے\s*کورسز|کورسز|مضامین)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches "assignment", "assignments", "asignment", "homework", etc.
        private static readonly Regex AssignmentsRegex = new(
            @"(^|\b)(as+ig+n\w*|homework\w*)(\b|$)|(اسائنمنٹ|ہوم\s*ورک)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches general deadline queries: "deadline", "deadlines", "due dates", "when to submit", "last date", "kab submit", etc.
        private static readonly Regex DeadlinesRegex = new(
            @"(^|\b)(deadlines?|due\s+dates?|last\s+date\w*|when\s+to\s+submit|when\s+is\s+it\s+due|pending\s+submissions?|submit\s+by|overdue\w*|late\s+submission\w*|miss\w*\s+deadline\w*|submission\s+dates?|kab\s+submit|last\s+day\s+to\s+submit)(\b|$)|(آخری\s*تاریخ|ڈیڈ\s*لائن|جمع\s*کروانے\s*کی\s*تاریخ)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches course syllabus, faculty instructor, credits, and structure
        private static readonly Regex CourseDetailsRegex = new(
            @"(^|\b)(course\s+details?|about\s+(this\s+|the\s+)?course|syllabus|who\s+is\s+(the\s+)?(faculty|teacher|professor|instructor)|faculty\s+(name|details?)|who\s+teaches|course\s+credits?|how\s+many\s+credits|course\s+duration|subjects?\s+in\s+this\s+course|units?\s+in\s+this\s+course|course\s+structure|course\s+overview|what\s+is\s+this\s+course\s+about)(\b|$)|(کورس\s*کی\s*تفصیلات|نصاب|استاد|کریڈٹس)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches learning process, 4-quadrant pedagogy, submission steps, grading system, attendance criteria, and exam guidance
        private static readonly Regex LearningProcessRegex = new(
            @"(^|\b)(learning\s+process\w*|how\s+does\s+learning\s+work|how\s+to\s+study|learning\s+workflow|4\s*quadrants?|four\s+quadrants?|quadrant\s*(1|2|3|4|i|ii|iii|iv)|how\s+do\s+i\s+submit|how\s+to\s+submit|submission\s+(process|guide|steps?|guideline\w*)|how\s+to\s+upload|how\s+are\s+grades\s+calculated|grading\s+(system|scale|policy|rule)|ugc\s+(10\s*point|scale|grading|policy)?|passing\s+(criteria|marks\s+rule|percentage|rules?)|how\s+does\s+attendance\s+work|attendance\s+criteria|attendance\s+rule|75\s*%\s*attendance|how\s+to\s+take\s+(exam|quiz)|proctoring\s+exam|ai\s+proctoring|safe\s+exam\s+browser|seb\s+(browser|exam)|how\s+to\s+download\s+grade\s*card|download\s+marksheet|grace\s+marks\s+policy|compartmental\s+exam|re-?assessment\s+policy)(\b|$)|(سیکھنے\s*کا\s*طریقہ|تعلیمی\s*عمل|چار\s*کواڈرینٹ|اسائنمنٹ\s*جمع|گریڈنگ\s*کا\s*نظام)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches support queries, FAQs, technical troubleshooting, and helpdesk self-service
        private static readonly Regex SupportFaqRegex = new(
            @"(^|\b)(support|helpdesk|faqs?|frequently\s+asked|(forgot|reset|change|update)\s+((my|the|account)\s+)?password|password\s*(reset|change|recovery|forgot)|(cannot|can('?t|not)|unable\s+to|fail\w*(\s+to)?|issue\s+with|problem\s+with|trouble\s+with|why\s+is(\s+my)?)\s+.*upload\w*|upload\w*\s+(fail\w*|error|issue|problem|stuck)|file\s+(upload\s+)?(fail\w*|too\s+large|error|not\s+supported|size\s+limit|corrupt\w*)|8\s*mb|zoom\s+(not\s+working|issue|error)|live\s+class\s+not\s+(opening|working)|video\s+not\s+playing|marks\s+pending|result\s+pending|pending\s+evaluation|raise\s+ticket|support\s+ticket|contact\s+mentor|contact\s+coordinator|bonafide(\s+certificate)?|transcript\s+request|technical\s+(issue|support|help)|customer\s+care)(\b|$)|(مدد|سپورٹ|پاس\s*ورڈ|مسئلہ|اپلوڈ)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches "timetable", "schedule", "skedule", "calendar", "calender", "upcoming classes", "next class", etc.
        private static readonly Regex ScheduleRegex = new(
            @"(^|\b)(time\s*table|schedule\w*|skedule\w*|calendar\w*|calender\w*|upcoming\s+class\w*|next\s+class|classes?\s+today|today('s)?\s+class\w*|live\s+class\w*|virtual\s+class\w*|zoom\s+class\w*|lecture\s+schedule|when\s+is\s+my\s+class)(\b|$)|(ٹائم\s*ٹیبل|شیڈول|کلاس\s*کا\s*وقت)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches "profile", "student info", "roll number", "roll no", "progress", "course progress", etc.
        private static readonly Regex ProfileRegex = new(
            @"(^|\b)(profile\w*|student\s+info|roll\s*no\w*|roll\s+number|my\s+progress|course\s+progress|progres\w*|completion\s+rate|how\s+much\s+completed)(\b|$)|(پروفائل|پروگریس)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Explicit conceptual / tutoring verbs that indicate an educational inquiry rather than self-data inquiry
        private static readonly Regex ConceptualKeywordsRegex = new(
            @"(^|\b)(explain|summarize|summary|concept\s+of|teach\s+me|help\s+me\s+solve|step\s+by\s+step|practice\s+quiz|practice\s+question|what\s+is\s+(the\s+)?(concept|difference|formula|definition)|define|how\s+to\s+solve)(\b|$)|(وضاحت|خلاصہ|سمجھائیں|سوالات)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SelfReferenceRegex = new(
            @"(^|\b)(my|i|me|mine|میرے|میری|میرا)(\b|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static CompanionIntent Classify(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return CompanionIntent.Tutoring;
            }

            var text = message.Trim();
            bool isConceptual = ConceptualKeywordsRegex.IsMatch(text);
            bool isSelfReferential = SelfReferenceRegex.IsMatch(text);

            // 1. Support FAQ & Helpdesk Self-Service (Password reset, file upload troubleshooting, pending marks, etc.)
            if (SupportFaqRegex.IsMatch(text))
            {
                return CompanionIntent.SupportFaq;
            }

            // 2. Learning Process Intent (Explaining 4-quadrants, grading scale, submission steps, exam/SEB rules)
            if (LearningProcessRegex.IsMatch(text))
            {
                return CompanionIntent.LearningProcess;
            }

            // 3. Attendance Intent
            if (AttendanceRegex.IsMatch(text))
            {
                if (!isConceptual || isSelfReferential)
                {
                    return CompanionIntent.Attendance;
                }
            }

            // 4. Grades & Results Intent
            if (GradesRegex.IsMatch(text))
            {
                if (!isConceptual || isSelfReferential)
                {
                    return CompanionIntent.Grades;
                }
            }

            // 5. Course Details & Faculty (Syllabus, instructor, credits, duration)
            if (CourseDetailsRegex.IsMatch(text))
            {
                return CompanionIntent.CourseDetails;
            }

            // 6. Enrolled Courses Intent
            if (CoursesRegex.IsMatch(text))
            {
                return CompanionIntent.EnrolledCourses;
            }

            // 7. Assignments Intent (Specifically preserves backwards compatibility when query references "assignment" or "homework")
            if (AssignmentsRegex.IsMatch(text))
            {
                if (!isConceptual || isSelfReferential || text.Contains("due", StringComparison.OrdinalIgnoreCase) || text.Contains("pending", StringComparison.OrdinalIgnoreCase))
                {
                    return CompanionIntent.Assignments;
                }
            }

            // 8. General Academic Deadlines Intent (Deadlines, due dates, submission windows, overdue checks)
            if (DeadlinesRegex.IsMatch(text))
            {
                return CompanionIntent.Deadlines;
            }

            // 9. Schedule & Timetable Intent
            if (ScheduleRegex.IsMatch(text))
            {
                return CompanionIntent.Schedule;
            }

            // 10. Profile & Progress Intent
            if (ProfileRegex.IsMatch(text))
            {
                return CompanionIntent.Profile;
            }

            // Default to Tutoring (AI-powered or educational grounding)
            return CompanionIntent.Tutoring;
        }
    }
}
