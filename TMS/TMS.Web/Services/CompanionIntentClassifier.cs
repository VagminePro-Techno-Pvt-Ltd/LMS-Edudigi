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

        // Matches "assignment", "assignments", "asignment", "homework", "deadlines", "due dates", etc.
        private static readonly Regex AssignmentsRegex = new(
            @"(^|\b)(as+ig+n\w*|homework\w*|deadlines?|due\s+dates?|pending\s+assignments?|submit\s+assignment|assignment\s+status)(\b|$)|(اسائنمنٹ|ہوم\s*ورک)",
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

            // 1. Attendance Intent (matches "attendance", "attendace", "attendence", etc.)
            if (AttendanceRegex.IsMatch(text))
            {
                // If it's conceptual without personal reference (e.g. "Explain attendance theory in HR"), route to Tutoring
                if (!isConceptual || isSelfReferential)
                {
                    return CompanionIntent.Attendance;
                }
            }

            // 2. Grades & Results Intent
            if (GradesRegex.IsMatch(text))
            {
                if (!isConceptual || isSelfReferential)
                {
                    return CompanionIntent.Grades;
                }
            }

            // 3. Enrolled Courses Intent
            if (CoursesRegex.IsMatch(text))
            {
                return CompanionIntent.EnrolledCourses;
            }

            // 4. Assignments & Deadlines Intent
            if (AssignmentsRegex.IsMatch(text))
            {
                if (!isConceptual || isSelfReferential || text.Contains("due", StringComparison.OrdinalIgnoreCase) || text.Contains("pending", StringComparison.OrdinalIgnoreCase))
                {
                    return CompanionIntent.Assignments;
                }
            }

            // 5. Schedule & Timetable Intent
            if (ScheduleRegex.IsMatch(text))
            {
                return CompanionIntent.Schedule;
            }

            // 6. Profile & Progress Intent
            if (ProfileRegex.IsMatch(text))
            {
                return CompanionIntent.Profile;
            }

            // Default to Tutoring (AI-powered or educational grounding)
            return CompanionIntent.Tutoring;
        }
    }
}
