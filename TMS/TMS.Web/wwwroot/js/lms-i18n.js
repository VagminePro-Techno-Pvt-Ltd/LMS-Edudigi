/**
 * LMS Internationalization (i18n) Engine for Admin & Faculty Portals
 * Supports full English <-> Urdu switching, RTL layout, DataTables & Dynamic Content Translation,
 * and Persistent Per-User Storage.
 */
(function (window, document) {
    'use strict';

    // ── Built-in Compiled Localization Dictionaries ─────────
    const dictionaries = {
        en: {
            // Common UI
            "Dashboard": "Dashboard",
            "Home": "Home",
            "Navigation": "Navigation",
            "Search": "Search",
            "Search...": "Search...",
            "Search table...": "Search table...",
            "Search courses, curriculum, quizzes, students…": "Search courses, curriculum, quizzes, students…",
            "Notifications": "Notifications",
            "Help & Support": "Help & Support",
            "FAQ": "FAQ",
            "Browse frequently asked questions": "Browse frequently asked questions",
            "Contact Mentor": "Contact Mentor",
            "Reach out to your assigned mentor": "Reach out to your assigned mentor",
            "Raise a Ticket": "Raise a Ticket",
            "Report an issue or request support": "Report an issue or request support",
            "Quick Dashboard Guide": "Quick Dashboard Guide",
            "How can we assist you today?": "How can we assist you today?",
            "Change Password": "Change Password",
            "Logout": "Logout",
            "Logout Now": "Logout Now",
            "Sign out": "Sign out",
            "Settings": "Settings",
            "Profile": "Profile",
            "My Profile": "My Profile",
            "All rights reserved": "All rights reserved",
            "All rights reserved.": "All rights reserved.",
            "Privacy Policy": "Privacy Policy",
            "Terms & Conditions": "Terms & Conditions",
            "Success": "Success",
            "Alert": "Alert",
            "Warning": "Warning",
            "Error": "Error",
            "Information": "Information",
            "Close": "Close",
            "Save": "Save",
            "Save Changes": "Save Changes",
            "Cancel": "Cancel",
            "Submit": "Submit",
            "Reject": "Reject",
            "Approve": "Approve",
            "Nominate": "Nominate",
            "Withdraw Nomination": "Withdraw Nomination",
            "Reject Nomination": "Reject Nomination",
            "View": "View",
            "View Details": "View Details",
            "Edit": "Edit",
            "Delete": "Delete",
            "Create": "Create",
            "Add New": "Add New",
            "Actions": "Actions",
            "Action": "Action",
            "Status": "Status",
            "Active": "Active",
            "Inactive": "Inactive",
            "Pending": "Pending",
            "Approved": "Approved",
            "Rejected": "Rejected",
            "Published": "Published",
            "Draft": "Draft",
            "Completed": "Completed",
            "In Progress": "In Progress",
            "Under Review": "Under Review",
            "Yes": "Yes",
            "No": "No",
            "Filter": "Filter",
            "Reset": "Reset",
            "Export": "Export",
            "Export to Excel": "Export to Excel",
            "Export to PDF": "Export to PDF",
            "Import": "Import",
            "Print": "Print",
            "Download": "Download",
            "Upload": "Upload",
            "Back": "Back",
            "Back to List": "Back to List",
            "Next": "Next",
            "Previous": "Previous",
            "Showing": "Showing",
            "entries": "entries",
            "No records found": "No records found",
            "No data available in table": "No data available in table",
            "Loading...": "Loading...",
            "Are you sure?": "Are you sure?",
            "Confirmation": "Confirmation",
            "Details": "Details",
            "Description": "Description",
            "Title": "Title",
            "Name": "Name",
            "Date": "Date",
            "Created On": "Created On",
            "Created By": "Created By",
            "Updated On": "Updated On",
            "Updated By": "Updated By",
            "Remarks": "Remarks",
            "Notes": "Notes",
            "Type": "Type",
            "Category": "Category",
            "File": "File",
            "Choose file": "Choose file",
            "No file chosen": "No file chosen",
            "Total": "Total",
            "Present": "Present",
            "Absent": "Absent",
            "Pass": "Pass",
            "Fail": "Fail",
            "Passed": "Passed",
            "Failed": "Failed",
            "Empowering": "Empowering",
            "Education": "Education",
            "Together": "Together",
            "Welcome back": "Welcome back",
            "Last Login": "Last Login",
            "Quick Shortcuts": "Quick Shortcuts",
            "Recent Activity": "Recent Activity",
            "View All": "View All",
            "English": "English",
            "Urdu": "اردو",
            "Language": "Language",

            // Admin Navigation & Modules
            "User Management": "User Management",
            "All Users": "All Users",
            "Faculty": "Faculty",
            "Students": "Students",
            "Bulk Upload": "Bulk Upload",
            "Bulk Upload Users": "Bulk Upload Users",
            "Roles": "Roles",
            "Roles & Permissions": "Roles & Permissions",
            "Role Name": "Role Name",
            "Permissions": "Permissions",
            "Add User": "Add User",
            "Edit User": "Edit User",
            "User Details": "User Details",
            "First Name": "First Name",
            "Last Name": "Last Name",
            "Email Address": "Email Address",
            "Phone Number": "Phone Number",
            "Mobile Number": "Mobile Number",
            "Department": "Department",
            "Designation": "Designation",
            "Division": "Division",
            "Location": "Location",
            "Venue": "Venue",
            "Employment Type": "Employment Type",
            "Academics": "Academics",
            "Academic Hub": "Academic Hub",
            "Content Library": "Content Library",
            "Faculty Mapping": "Faculty Mapping",
            "Faculty Map": "Faculty Map",
            "Enrollments": "Enrollments",
            "Course Enrollment": "Course Enrollment",
            "Quiz Management": "Quiz Management",
            "Virtual Classroom": "Virtual Classroom",
            "Virtual Classroom (Zoom)": "Virtual Classroom (Zoom)",
            "Live Virtual Classes": "Live Virtual Classes",
            "Live Virtual Classes (Zoom)": "Live Virtual Classes (Zoom)",
            "Class Calendar": "Class Calendar",
            "Student Attendance": "Student Attendance",
            "Student Progress": "Student Progress",
            "Discussion Forum": "Discussion Forum",
            "Total Students": "Total Students",
            "Total Faculty": "Total Faculty",
            "Total Courses": "Total Courses",
            "Total Materials": "Total Materials",
            "Active Courses": "Active Courses",
            "Learning Assets": "Learning Assets",
            "Recent Users": "Recent Users",
            "System Overview & Analytics": "System Overview & Analytics",
            "Quick Navigation": "Quick Navigation",
            "Manage Programs": "Manage Programs",
            "Manage Courses": "Manage Courses",
            "Manage Faculty": "Manage Faculty",
            "Manage Students": "Manage Students",
            "View All Users": "View All Users",
            "View Academic Hub": "View Academic Hub",
            "Upload Users via CSV/Excel": "Upload Users via CSV/Excel",
            "Download Template": "Download Template",
            "Programs & Curriculum": "Programs & Curriculum",
            "Semester Structure": "Semester Structure",
            "Assign Faculty to Course": "Assign Faculty to Course",
            "Enroll Students in Course": "Enroll Students in Course",
            "Batch Enrollment": "Batch Enrollment",
            "Student ID": "Student ID",
            "Faculty ID": "Faculty ID",
            "Course Name": "Course Name",
            "Course Code": "Course Code",
            "Semester": "Semester",
            "Program": "Program",
            "Academic Year": "Academic Year",
            "Training Matrix": "Training Matrix",
            "Training Schedule": "Training Schedule",
            "Training Nomination": "Training Nomination",
            "Approval Status": "Approval Status",
            "Assigned Date": "Assigned Date",
            "Enrolled Date": "Enrolled Date",

            // Faculty Portal
            "My Courses": "My Courses",
            "My Courses & Curriculum": "My Courses & Curriculum",
            "Assigned Students": "Assigned Students",
            "Teaching Modules": "Teaching Modules",
            "My Teaching Load": "My Teaching Load",
            "Pending Evaluations": "Pending Evaluations",
            "Pending Assessments": "Pending Assessments",
            "Today's Live Classes": "Today's Live Classes",
            "Interactive PPT": "Interactive PPT",
            "Faculty Dashboard": "Faculty Dashboard",
            "Teaching Assignments": "Teaching Assignments",
            "Student Submissions": "Student Submissions",
            "View Submissions": "View Submissions",
            "Evaluate Submission": "Evaluate Submission",
            "Grade Submission": "Grade Submission",
            "Enter Marks": "Enter Marks",
            "Marks": "Marks",
            "Score": "Score",
            "Percentage": "Percentage",
            "Feedback to Student": "Feedback to Student",
            "Evaluation Date": "Evaluation Date",
            "Evaluator": "Evaluator",
            "Start Meeting": "Start Meeting",
            "Join Meeting": "Join Meeting",
            "Meeting Link": "Meeting Link",
            "Meeting Password": "Meeting Password",
            "Zoom Meeting": "Zoom Meeting",
            "Scheduled Classes": "Scheduled Classes",
            "Upcoming Lectures": "Upcoming Lectures",
            "Lecture Title": "Lecture Title",
            "Course Materials": "Course Materials",
            "Upload Lecture Notes": "Upload Lecture Notes",
            "Upload Slides (PPT)": "Upload Slides (PPT)",
            "Upload Lecture Video": "Upload Lecture Video",
            "Student Progress Summary": "Student Progress Summary",
            "Announcements": "Announcements",
            "Create Announcement": "Create Announcement",
            "Post Announcement": "Post Announcement",
            "Discussion Threads": "Discussion Threads",
            "Start Discussion": "Start Discussion",
            "Reply": "Reply",
            "Post Reply": "Post Reply",
            "Quick Grade": "Quick Grade",
            "Assigned Courses": "Assigned Courses",
            "Active Students": "Active Students",

            // Courses & Quadrants
            "Course": "Course",
            "Courses": "Courses",
            "Course Details": "Course Details",
            "Course Curriculum": "Course Curriculum",
            "Program Name": "Program Name",
            "Degree": "Degree",
            "Specialization": "Specialization",
            "Syllabus": "Syllabus",
            "Credits": "Credits",
            "Duration": "Duration",
            "Total Units": "Total Units",
            "Total Lessons": "Total Lessons",
            "Unit": "Unit",
            "Units": "Units",
            "Lesson": "Lesson",
            "Lessons": "Lessons",
            "Topic": "Topic",
            "Topics": "Topics",
            "4-Quadrant Learning System": "4-Quadrant Learning System",
            "Quadrant 1: E-Tutorial": "Quadrant 1: E-Tutorial",
            "Quadrant 2: E-Content": "Quadrant 2: E-Content",
            "Quadrant 3: Discussion Forum": "Quadrant 3: Discussion Forum",
            "Quadrant 4: Assessment": "Quadrant 4: Assessment",
            "E-Tutorial (Video / Audio)": "E-Tutorial (Video / Audio)",
            "E-Content (Digital Documents & Books)": "E-Content (Digital Documents & Books)",
            "Discussion & Web Resources": "Discussion & Web Resources",
            "Assessment & Quizzes": "Assessment & Quizzes",
            "Lecture Notes": "Lecture Notes",
            "Supplementary Materials": "Supplementary Materials",
            "Video Lecture": "Video Lecture",
            "Watch Video": "Watch Video",
            "Video Progress": "Video Progress",
            "Watched Duration": "Watched Duration",
            "Document Viewer": "Document Viewer",
            "Download File": "Download File",
            "File Size": "File Size",
            "Upload Date": "Upload Date",
            "Enrolled Students": "Enrolled Students",
            "Curriculum Structure": "Curriculum Structure",

            // Attendance
            "Attendance": "Attendance",
            "Attendance Register": "Attendance Register",
            "Mark Attendance": "Mark Attendance",
            "Take Attendance": "Take Attendance",
            "Attendance Records": "Attendance Records",
            "Attendance Percentage": "Attendance Percentage",
            "Total Lectures Conducted": "Total Lectures Conducted",
            "Lectures Attended": "Lectures Attended",
            "Present Count": "Present Count",
            "Absent Count": "Absent Count",
            "Attendance Status": "Attendance Status",
            "Attendance Date": "Attendance Date",
            "Session": "Session",
            "Lecture Hour": "Lecture Hour",
            "Student Name": "Student Name",
            "Roll Number": "Roll Number",
            "Mark All Present": "Mark All Present",
            "Mark All Absent": "Mark All Absent",
            "Attendance Summary": "Attendance Summary",
            "Low Attendance Warning (<75%)": "Low Attendance Warning (<75%)",

            // Assessments & Grade Cards
            "Internal Assessments": "Internal Assessments",
            "Internal Assessment": "Internal Assessment",
            "Internal Grade Cards": "Internal Grade Cards",
            "Quiz Assessments": "Quiz Assessments",
            "Assessments": "Assessments",
            "Assessment Policy": "Assessment Policy",
            "Assessment Window": "Assessment Window",
            "Assessment Type": "Assessment Type",
            "Assignment": "Assignment",
            "Written Examination": "Written Examination",
            "Practical Examination": "Practical Examination",
            "Project Work": "Project Work",
            "Presentation": "Presentation",
            "Viva Voce": "Viva Voce",
            "Multiple Choice Questions (MCQ)": "Multiple Choice Questions (MCQ)",
            "Question Bank": "Question Bank",
            "Questions": "Questions",
            "Add Question": "Add Question",
            "Edit Question": "Edit Question",
            "Question Text": "Question Text",
            "Option A": "Option A",
            "Option B": "Option B",
            "Option C": "Option C",
            "Option D": "Option D",
            "Correct Answer": "Correct Answer",
            "Explanation": "Explanation",
            "Maximum Marks": "Maximum Marks",
            "Passing Marks": "Passing Marks",
            "Weightage (%)": "Weightage (%)",
            "Start Date": "Start Date",
            "End Date": "End Date",
            "Due Date": "Due Date",
            "Late Submission Allowed": "Late Submission Allowed",
            "Publish Assessment": "Publish Assessment",
            "Unpublish Assessment": "Unpublish Assessment",
            "Publish Results": "Publish Results",
            "Hide Results": "Hide Results",
            "Request Result Approval": "Request Result Approval",
            "Approve Results": "Approve Results",
            "Reject Results": "Reject Results",
            "Grade Questions": "Grade Questions",
            "Grade Cards": "Grade Cards",
            "Semester Grade Card": "Semester Grade Card",
            "Preview Grade Card": "Preview Grade Card",
            "Print Grade Statement": "Print Grade Statement",
            "Student Marks": "Student Marks",
            "Marks Obtained": "Marks Obtained",
            "Result Status": "Result Status",
            "Grade": "Grade",
            "Grade Point": "Grade Point",
            "SGPA": "SGPA",
            "CGPA": "CGPA",
            "Attempt": "Attempt",
            "Attempt Date": "Attempt Date",
            "Submission Date": "Submission Date",
            "Submitted File": "Submitted File",
            "Evaluation Status": "Evaluation Status",
            "Graded": "Graded",
            "Ungraded": "Ungraded",
            "Minimum passing threshold: 33%": "Minimum passing threshold: 33%",

            // Academic Hub & Course Structure
            "Programs • Courses • Curriculum Hierarchy": "Programs • Courses • Curriculum Hierarchy",
            "Centralized command center for managing Degree Programs, Course Catalog, and Syllabus Structure": "Centralized command center for managing Degree Programs, Course Catalog, and Syllabus Structure",
            "Centralized command center for managing Degree Programs, Course Catalog, and Syllabus Structure.": "Centralized command center for managing Degree Programs, Course Catalog, and Syllabus Structure.",
            "Programs Directory": "Programs Directory",
            "Course Catalog": "Course Catalog",
            "Curriculum Hierarchy": "Curriculum Hierarchy",
            "Tree View": "Tree View",
            "Total Credits": "Total Credits",
            "Semesters": "Semesters",
            "Programs": "Programs",
            "Courses": "Courses",
            "New Program": "New Program",
            "Add Course & Structure": "Add Course & Structure",
            "Add Course": "Add Course",
            "Edit Program": "Edit Program",
            "Edit Program Details": "Edit Program Details",
            "No programs available": "No programs available",
            "Create an active program before building the curriculum.": "Create an active program before building the curriculum.",
            "No Courses Assigned Yet": "No Courses Assigned Yet",
            "You have not been assigned to any courses in the Faculty Course Mapping yet. Please contact your administrator to map your subjects.": "You have not been assigned to any courses in the Faculty Course Mapping yet. Please contact your administrator to map your subjects.",
            "No curriculum available": "No curriculum available",
            "Select a program to view its curriculum structure.": "Select a program to view its curriculum structure.",
            "Search courses by title or code...": "Search courses by title or code...",
            "All Programs": "All Programs",
            "All Semesters": "All Semesters",
            "No program data available": "No program data available",
            "Select another program and try again.": "Select another program and try again.",
            "Add Course to this Program": "Add Course to this Program",
            "No semester structure found": "No semester structure found",
            "No semesters or courses are currently defined for": "No semesters or courses are currently defined for",
            "Add First Course": "Add First Course",
            "Content Item": "Content Item",
            "Content Items": "Content Items",
            "Content": "Content",
            "Credit": "Credit",
            "Credits": "Credits",
            "Cr": "Cr",
            "Mo": "Mo",
            "Assign Faculty / Instructors": "Assign Faculty / Instructors",
            "View assigned content": "View assigned content",
            "Bachelor of Computer Applications (BCA)": "Bachelor of Computer Applications (BCA)",
            "Bachelor of Business Administration (BBA)": "Bachelor of Business Administration (BBA)",
            "Master of Business Administration (MBA)": "Master of Business Administration (MBA)",
            "Master of Computer Applications (MCA)": "Master of Computer Applications (MCA)",
            "Business Studies": "Business Studies",
            "Principles and Practice of Management": "Principles and Practice of Management",
            "The Bachelor of Business Administration (BBA) programme is designed to develop a strong foundation in business and management principles": "The Bachelor of Business Administration (BBA) programme is designed to develop a strong foundation in business and management principles",
            "The Bachelor of Computer Applications (BCA) program is designed to build foundational computer science and software development skills.": "The Bachelor of Computer Applications (BCA) program is designed to build foundational computer science and software development skills.",

            // Internal Assessments Faculty & Admin
            "Internal Assessments — Faculty": "Internal Assessments — Faculty",
            "Internal Assessments — Admin": "Internal Assessments — Admin",
            "Create, publish, evaluate and submit results for Admin approval.": "Create, publish, evaluate and submit results for Admin approval.",
            "Create, publish, evaluate and submit results for Admin approval": "Create, publish, evaluate and submit results for Admin approval",
            "Create Internal Assessment": "Create Internal Assessment",
            "Enter assessment title": "Enter assessment title",
            "Assessment types (multiple)": "Assessment types (multiple)",
            "Select assessment types": "Select assessment types",
            "Select all": "Select all",
            "Clear": "Clear",
            "Max marks": "Max marks",
            "Choose course": "Choose course",
            "Subject (optional)": "Subject (optional)",
            "All subjects": "All subjects",
            "Admin-approved assessment window": "Admin-approved assessment window",
            "Due date & time": "Due date & time",
            "Choose active schedule": "Choose active schedule",
            "Select a schedule to see its date range.": "Select a schedule to see its date range.",
            "Select a schedule to see its date range": "Select a schedule to see its date range",
            "Use schedule end": "Use schedule end",
            "Allow late submission": "Allow late submission",
            "Instructions": "Instructions",
            "Enter instructions for students": "Enter instructions for students",
            "Create Draft": "Create Draft",
            "Created / Assigned Assessments": "Created / Assigned Assessments",
            "Legacy semester-wide": "Legacy semester-wide",
            "Other course": "Other course",

            // Faculty Quiz Assessments & Evaluations
            "EVALUATION PORTAL": "EVALUATION PORTAL",
            "Evaluation Portal": "Evaluation Portal",
            "Faculty Quiz Assessments": "Faculty Quiz Assessments",
            "Review student submissions, evaluate subjective answers, and record official marks.": "Review student submissions, evaluate subjective answers, and record official marks.",
            "Review student submissions, evaluate subjective answers, and record official marks": "Review student submissions, evaluate subjective answers, and record official marks",
            "Submissions": "Submissions",
            "SUBMISSIONS": "SUBMISSIONS",
            "Evaluated": "Evaluated",
            "EVALUATED": "EVALUATED",
            "PENDING": "PENDING",
            "Create New Quiz": "Create New Quiz",
            "Manage Quizzes": "Manage Quizzes",
            "Student Attempts List": "Student Attempts List",
            "Search student, course, or quiz...": "Search student, course, or quiz...",
            "Search student, course, or quiz": "Search student, course, or quiz",
            "...Search student, course, or quiz": "...Search student, course, or quiz",
            "Quadrant": "Quadrant",
            "QUADRANT": "QUADRANT",
            "Quiz Title": "Quiz Title",
            "QUIZ TITLE": "QUIZ TITLE",
            "Submitted On": "Submitted On",
            "SUBMITTED ON": "SUBMITTED ON",
            "Score": "Score",
            "SCORE": "SCORE",
            "No Student Submissions Yet": "No Student Submissions Yet",
            "When students attempt quizzes for your assigned courses, their submissions will appear here.": "When students attempt quizzes for your assigned courses, their submissions will appear here.",
            "Pending Evaluation": "Pending Evaluation",
            "Evaluate / Review": "Evaluate / Review",
            "Evaluate": "Evaluate",
            "Review": "Review",

            // Dashboard Faculty & Admin
            "Assigned Curriculum": "Assigned Curriculum",
            "Content Hub": "Content Hub",
            "Assessment Evaluations": "Assessment Evaluations",
            "Access and upload lecture materials, PDFs, and assignments for your courses.": "Access and upload lecture materials, PDFs, and assignments for your courses.",
            "Grade subjective answers and review student checkpoint test attempts.": "Grade subjective answers and review student checkpoint test attempts.",
            "MY COURSES": "MY COURSES",
            "MY STUDENTS": "MY STUDENTS",
            "PENDING ASSESSMENTS": "PENDING ASSESSMENTS",
            "COURSE MATERIALS": "COURSE MATERIALS",
            "Active Modules": "Active Modules",
            "Active Learners": "Active Learners",
            "Submissions to Grade": "Submissions to Grade",
            "Content Resources": "Content Resources",
            "Assigned": "Assigned",
            "Enrolled": "Enrolled",
            "Resources": "Resources",
            "TOTAL STUDENTS": "TOTAL STUDENTS",
            "TOTAL FACULTY": "TOTAL FACULTY",
            "TOTAL COURSES": "TOTAL COURSES",
            "LIBRARY ASSETS": "LIBRARY ASSETS",
            "Registered Learners": "Registered Learners",
            "Active Educators": "Active Educators",
            "Instituted Degrees": "Instituted Degrees",
            "Resource Mappings": "Resource Mappings",
            "My Assigned Courses": "My Assigned Courses",
            "Courses and syllabus modules assigned to your teaching workload.": "Courses and syllabus modules assigned to your teaching workload.",
            "Course Title": "Course Title",
            "General Program": "General Program",
            "Faculty Quick Actions": "Faculty Quick Actions",
            "Shortcuts to frequently used instructor workflows.": "Shortcuts to frequently used instructor workflows.",
            "Platform Maintenance": "Platform Maintenance",
            "System Status": "System Status",
            "No active threats or outages. Background operations are proceeding within bounds.": "No active threats or outages. Background operations are proceeding within bounds.",
            "Institution Overview": "Institution Overview",
            "Manage users, program definitions, and academic catalogs via shortcuts.": "Manage users, program definitions, and academic catalogs via shortcuts.",
            "No assigned courses found yet. Please contact admin to map courses to your account.": "No assigned courses found yet. Please contact admin to map courses to your account.",
            "Here's an overview of your assigned courses, enrolled learners, and active assessments.": "Here's an overview of your assigned courses, enrolled learners, and active assessments.",
            "Here's an overview of your administrative platform management.": "Here's an overview of your administrative platform management.",

            // Faculty PPT
            "Manage Interactive PPTs": "Manage Interactive PPTs",
            "View and manage your uploaded PowerPoint presentations": "View and manage your uploaded PowerPoint presentations",
            "Upload New PPT": "Upload New PPT",
            "Total PPTs": "Total PPTs",
            "TOTAL PPTS": "TOTAL PPTS",
            "Total Size": "Total Size",
            "TOTAL SIZE": "TOTAL SIZE",
            "All Courses": "All Courses",
            "All Status": "All Status",
            "Search PPT title...": "Search PPT title...",
            "Search PPT title": "Search PPT title",
            "No PPTs Uploaded Yet": "No PPTs Uploaded Yet",
            "Upload your first Interactive PowerPoint presentation to get started.": "Upload your first Interactive PowerPoint presentation to get started.",
            "Upload your first Interactive PowerPoint presentation to get started": "Upload your first Interactive PowerPoint presentation to get started",
            "Upload PPT": "Upload PPT",
            "PPT Title": "PPT Title",
            "Course Details": "Course Details",
            "Unit & Topic": "Unit & Topic",
            "File Info": "File Info",
            "Uploaded": "Uploaded",
            "View Slides (Student Preview)": "View Slides (Student Preview)",
            "Manage Slide Interactions": "Manage Slide Interactions",
            "Edit PPT": "Edit PPT",
            "Delete PPT": "Delete PPT",

            // Content / Central Library & Quadrants
            "Central Library": "Central Library",
            "All uploaded content": "All uploaded content",
            "From Library": "From Library",
            "Upload New": "Upload New",
            "Upload New File": "Upload New File",
            "Upload Content": "Upload Content",
            "Browse Central Library": "Browse Central Library",
            "Global Content Center": "Global Content Center",
            "A centralized repository to manage all course materials. Upload once, assign anywhere.": "A centralized repository to manage all course materials. Upload once, assign anywhere.",
            "No content assigned to this course yet": "No content assigned to this course yet",
            "Search by title, file name...": "Search by title, file name...",
            "Learning Quadrants": "Learning Quadrants",
            "Filter by quadrant view": "Filter by quadrant view",
            "4 Quadrants Architecture": "4 Quadrants Architecture",
            "Quadrants Architecture 4": "Quadrants Architecture 4",
            "Four Quadrant Structure": "Four Quadrant Structure",
            "All Content": "All Content",
            "Q1 - PDF / Notes": "Q1 - PDF / Notes",
            "Q2 - Video": "Q2 - Video",
            "Q3 - Assignment": "Q3 - Assignment",
            "Q4 - Discussion": "Q4 - Discussion",
            "Q1 - PDF/Notes": "Q1 - PDF/Notes",
            "Q2 - VIDEO": "Q2 - VIDEO",
            "Q3 - ASSIGNMENT": "Q3 - ASSIGNMENT",
            "Q4 - DISCUSSION": "Q4 - DISCUSSION",
            "General": "General",
            "VIDEO URL": "VIDEO URL",
            "Video URL": "Video URL",
            "Assign to Course": "Assign to Course",
            "Select Destination Course": "Select Destination Course",
            "Assign to Quadrant": "Assign to Quadrant",
            "-- No Quadrant --": "-- No Quadrant --",
            "Assign Now": "Assign Now",
            "Assign to another course": "Assign to another course",
            "Edit content": "Edit content",
            "File Preview": "File Preview",
            "Close Preview": "Close Preview",
            "Loading Global Library...": "Loading Global Library...",

            // Internal Grade Cards
            "Internal Assessment: Grade Cards": "Internal Assessment: Grade Cards",
            "Internal Assessment Grade Cards": "Internal Assessment Grade Cards",
            "View subject-wise or semester-wise published Internal Assessment marks statements": "View subject-wise or semester-wise published Internal Assessment marks statements",
            "Available Grade Cards": "Available Grade Cards",
            "Published Assessment Entries": "Published Assessment Entries",
            "Evaluated Entries": "Evaluated Entries",
            "Faculty Grade Cards": "Faculty Grade Cards",
            "All Student Grade Cards": "All Student Grade Cards",
            "My Grade Cards": "My Grade Cards",
            "No published internal grade cards available": "No published internal grade cards available",
            "Grade cards appear after faculty publishes internal assessment results.": "Grade cards appear after faculty publishes internal assessment results.",
            "Faculty Marks Entry": "Faculty Marks Entry",
            "Student": "Student",
            "STUDENT": "STUDENT",
            "Outcome": "Outcome",
            "OUTCOME": "OUTCOME",
            "Subject-wise": "Subject-wise",
            "Semester-wise": "Semester-wise",
            "Student ID": "Student ID",
            "Student ID:": "Student ID:",

            // Student Portal & Sidebar
            "Student Portal": "Student Portal",
            "Learn": "Learn",
            "Syllabus": "Syllabus",
            "Class Calendar": "Class Calendar",
            "Assignments": "Assignments",
            "Assessments & Exams": "Assessments & Exams",
            "Assessments &amp; Exams": "Assessments & Exams",
            "My Grade Card": "My Grade Card",
            "Virtual Classroom": "Virtual Classroom",
            "My Attendance": "My Attendance",
            "My Progress Report": "My Progress Report",
            "Collaborate & Communication": "Collaborate & Communication",
            "Collaborate &amp; Communication": "Collaborate & Communication",
            "Notification & Inbox": "Notification & Inbox",
            "Notification &amp; Inbox": "Notification & Inbox",
            "Discussion Forum": "Discussion Forum",
            "Announcements": "Announcements",
            "Wiki": "Wiki",
            "Swayam": "Swayam",
            "Google": "Google",
            "Microsoft": "Microsoft",
            "Chat & Meeting": "Chat & Meeting",
            "Chat &amp; Meeting": "Chat & Meeting",
            "Blog": "Blog",
            "Interactive PPT": "Interactive PPT",
            "Empowering": "Empowering",
            "Education Together": "Education Together",
            "Education<br/>Together": "Education Together",
            "Student Dashboard": "Student Dashboard",

            // Student Dashboard Metrics & Cards
            "Learning Progress": "Learning Progress",
            "Topics Consumed": "Topics Consumed",
            "topics consumed": "topics consumed",
            "My Documents": "My Documents",
            "View enrolled courses & files": "View enrolled courses & files",
            "Job Inbox": "Job Inbox",
            "Placement Cell": "Placement Cell",
            "Check latest job openings": "Check latest job openings",
            "Mission:": "Mission:",
            "To provide an effective environment for teaching-learning and innovation in order to transform our students into dynamic, professionally sound and socially responsible citizens with ability to adapt to the changing needs of the industry and society.": "To provide an effective environment for teaching-learning and innovation in order to transform our students into dynamic, professionally sound and socially responsible citizens with ability to adapt to the changing needs of the industry and society.",
            "University Roll No.": "University Roll No.",
            "Section": "Section",
            "Library Code": "Library Code",
            "Mobile No.": "Mobile No.",
            "Birth Date": "Birth Date",
            "Address": "Address",
            "Today's Exams": "Today's Exams",
            "No exams scheduled.": "No exams scheduled.",
            "Utilize your free time wisely — a perfect opportunity to prepare for your upcoming exams.": "Utilize your free time wisely — a perfect opportunity to prepare for your upcoming exams.",
            "Today's Virtual Classes": "Today's Virtual Classes",
            "No Virtual Classes Today": "No Virtual Classes Today",
            "Recent Announcements": "Recent Announcements",
            "No announcements.": "No announcements.",
            "Stay tuned for any updates from your admin or faculty.": "Stay tuned for any updates from your admin or faculty.",
            "View All": "View All",
            "Total Courses": "Total Courses",
            "Enrolled Courses": "Enrolled Courses",
            "Exams": "Exams",
            "Results": "Results",
            "View score reports": "View score reports",
            "Passing Rate": "Passing Rate",
            "Based on all exams attempted": "Based on all exams attempted",
            "My Enrolled Courses": "My Enrolled Courses",
            "View All Courses": "View All Courses",
            "Continue Learning": "Continue Learning",
            "View Syllabus & Content": "View Syllabus & Content",
            "Course Progress": "Course Progress",
            "Finished": "Finished",
            "Done": "Done",
            "You are not enrolled in any courses yet.": "You are not enrolled in any courses yet.",
            "Contact your admin to get started with your learning journey.": "Contact your admin to get started with your learning journey.",
            "Instruction & Support": "Instruction & Support",
            "Frequently Asked Questions": "Frequently Asked Questions",
            "Progress Report": "Progress Report",
            "View Full Report": "View Full Report",
            "Quiz": "Quiz",
            "Assignment": "Assignment",
            "Study Material": "Study Material",
            "E-Content": "E-Content",

            // Student Courses & Units
            "Semester Subjects": "Semester Subjects",
            "Subjects": "Subjects",
            "Browse units and explore their learning content.": "Browse units and explore their learning content.",
            "Units": "Units",
            "Materials": "Materials",
            "Progress": "Progress",
            "Category": "Category",
            "Credits": "Credits",
            "Duration": "Duration",
            "Code": "Code",
            "Course Units": "Course Units",
            "Open": "Open",
            "No units have been added to this course yet. Content will appear here once your faculty uploads materials.": "No units have been added to this course yet. Content will appear here once your faculty uploads materials.",

            // Student Exams & Quizzes Hub
            "Assessments & Quizzes": "Assessments & Quizzes",
            "Assessments &amp; Quizzes": "Assessments & Quizzes",
            "All your scheduled exams, evaluations, topic tests and quizzes in one single hub.": "All your scheduled exams, evaluations, topic tests and quizzes in one single hub.",
            "Formal Exams": "Formal Exams",
            "active now": "active now",
            "Course Quizzes": "Course Quizzes",
            "Faculty created": "Faculty created",
            "Quizzes Completed": "Quizzes Completed",
            "Evaluated submissions": "Evaluated submissions",
            "Total Tests Available": "Total Tests Available",
            "Overall learning checks": "Overall learning checks",
            "All Assessments & Quizzes": "All Assessments & Quizzes",
            "All Assessments &amp; Quizzes": "All Assessments & Quizzes",
            "Formal Exams & Scheduled Assessments": "Formal Exams & Scheduled Assessments",
            "Formal Exams &amp; Scheduled Assessments": "Formal Exams & Scheduled Assessments",
            "Scheduled": "Scheduled",
            "No Scheduled Exams": "No Scheduled Exams",
            "You have no pending formal exams assigned currently.": "You have no pending formal exams assigned currently.",
            "Take Exam": "Take Exam",
            "Start Quiz": "Start Quiz",
            "View Result": "View Result",
            "Questions": "Questions",
            "Total Marks": "Total Marks",
            "Passing Marks": "Passing Marks",
            "Duration (Mins)": "Duration (Mins)",
            "Start Date": "Start Date",
            "End Date": "End Date",

            // Student Attendance
            "My Attendance Log": "My Attendance Log",
            "Keep track of your presence on the platform.": "Keep track of your presence on the platform.",
            "Current Rate": "Current Rate",
            "Quick Stats": "Quick Stats",
            "Total Days Tracked": "Total Days Tracked",
            "Days Present": "Days Present",
            "Late Arrivals": "Late Arrivals",
            "Absences": "Absences",
            "Attendance Policy": "Attendance Policy",
            "Your attendance is marked automatically when you log into the platform. A minimum of 75% attendance is recommended for optimal learning.": "Your attendance is marked automatically when you log into the platform. A minimum of 75% attendance is recommended for optimal learning.",
            "Recent Activity": "Recent Activity",
            "View Full History": "View Full History",
            "Login Time": "Login Time",
            "Remarks": "Remarks",
            "No attendance data available yet.": "No attendance data available yet.",

            // Student PPTs & Progress
            "Your available presentations across enrolled courses.": "Your available presentations across enrolled courses.",
            "No Interactive PPT available.": "No Interactive PPT available.",
            "Overall Progress": "Overall Progress",
            "Track your overall course completion and activity": "Track your overall course completion and activity"
        },
        ur: {
            // Common UI
            "Dashboard": "ڈیش بورڈ",
            "Home": "ہوم",
            "Navigation": "نیویگیشن",
            "Search": "تلاش کریں",
            "Search...": "تلاش کریں...",
            "Search table...": "جدول میں تلاش کریں...",
            "Search courses, curriculum, quizzes, students…": "کورسز، نصاب، کوئز، طلبہ تلاش کریں…",
            "Notifications": "اطلاعات",
            "Help & Support": "مدد اور تعاون",
            "FAQ": "عمومی سوالات",
            "Browse frequently asked questions": "عام طور پر پوچھے جانے والے سوالات دیکھیں",
            "Contact Mentor": "استاد / رہبر سے رابطہ",
            "Reach out to your assigned mentor": "اپنے مقرر کردہ رہبر سے رابطہ کریں",
            "Raise a Ticket": "شکایت درج کریں",
            "Report an issue or request support": "مسئلہ بتائیں یا مدد حاصل کریں",
            "Quick Dashboard Guide": "فوری ڈیش بورڈ گائیڈ",
            "How can we assist you today?": "ہم آج آپ کی کیا مدد کر سکتے ہیں؟",
            "Change Password": "پاس ورڈ تبدیل کریں",
            "Logout": "لاگ آؤٹ",
            "Logout Now": "ابھی لاگ آؤٹ کریں",
            "Sign out": "سائن آؤٹ",
            "Settings": "ترتیبات",
            "Profile": "پروفائل",
            "My Profile": "میرا پروفائل",
            "All rights reserved": "جملہ حقوق محفوظ ہیں",
            "All rights reserved.": "جملہ حقوق محفوظ ہیں۔",
            "Privacy Policy": "رازداری کی پالیسی",
            "Terms & Conditions": "شرائط و ضوابط",
            "Success": "کامیابی",
            "Alert": "انتباہ",
            "Warning": "تنبیہ",
            "Error": "خرابی",
            "Information": "معلومات",
            "Close": "بند کریں",
            "Save": "محفوظ کریں",
            "Save Changes": "تبدیلیاں محفوظ کریں",
            "Cancel": "منسوخ کریں",
            "Submit": "جمع کرائیں",
            "Reject": "مسترد کریں",
            "Approve": "منظور کریں",
            "Nominate": "نامزد کریں",
            "Withdraw Nomination": "نامزدگی واپس لیں",
            "Reject Nomination": "نامزدگی مسترد کریں",
            "View": "دیکھیں",
            "View Details": "تفصیلات دیکھیں",
            "Edit": "ترمیم کریں",
            "Delete": "حذف کریں",
            "Create": "تخلیق کریں",
            "Add New": "نیا شامل کریں",
            "Actions": "کارروائیاں",
            "Action": "کارروائی",
            "Status": "کیفیت",
            "Active": "فعال",
            "Inactive": "غیر فعال",
            "Pending": "زیرِ التوا",
            "Approved": "منظور شدہ",
            "Rejected": "مسترد شدہ",
            "Published": "شائع شدہ",
            "Draft": "مسودہ",
            "Completed": "مکمل",
            "In Progress": "جاری ہے",
            "Under Review": "زیرِ جائزہ",
            "Yes": "ہاں",
            "No": "نہیں",
            "Filter": "فلٹر",
            "Reset": "دوبارہ ترتیب دیں",
            "Export": "برآمد کریں",
            "Export to Excel": "ایکسل میں برآمد کریں",
            "Export to PDF": "پی ڈی ایف میں برآمد کریں",
            "Import": "درآمد کریں",
            "Print": "پرنٹ کریں",
            "Download": "ڈاؤن لوڈ",
            "Upload": "اپ لوڈ",
            "Back": "واپس",
            "Back to List": "فہرست پر واپس جائیں",
            "Next": "اگلا",
            "Previous": "پچھلا",
            "Showing": "دکھائے جا رہے ہیں",
            "entries": "اندراجات",
            "No records found": "کوئی ریکارڈ نہیں ملا",
            "No data available in table": "جدول میں کوئی ڈیٹا دستیاب نہیں ہے",
            "Loading...": "لوڈ ہو رہا ہے...",
            "Are you sure?": "کیا آپ کو یقین ہے؟",
            "Confirmation": "تصدیق",
            "Details": "تفصیلات",
            "Description": "وضاحت",
            "Title": "عنوان",
            "Name": "نام",
            "Date": "تاریخ",
            "Created On": "تخلیق کی تاریخ",
            "Created By": "تخلیق کار",
            "Updated On": "ترمیم کی تاریخ",
            "Updated By": "ترمیم کار",
            "Remarks": "آراء و تبصرہ",
            "Notes": "نوٹس",
            "Type": "قسم",
            "Category": "زمرہ",
            "File": "فائل",
            "Choose file": "فائل منتخب کریں",
            "No file chosen": "کوئی فائل منتخب نہیں کی گئی",
            "Total": "کل تعداد",
            "Present": "حاضر",
            "Absent": "غیر حاضر",
            "Pass": "کامیاب",
            "Fail": "ناکام",
            "Passed": "کامیاب",
            "Failed": "ناکام",
            "Empowering": "تعلیمی",
            "Education": "ترقی باہمی",
            "Together": "عزم کے ساتھ",
            "Welcome back": "خوش آمدید",
            "Last Login": "آخری لاگ ان",
            "Quick Shortcuts": "فوری شارٹ کٹس",
            "Recent Activity": "حالیہ سرگرمیاں",
            "View All": "سب دیکھیں",
            "English": "English",
            "Urdu": "اردو",
            "Language": "زبان",

            // Admin Navigation & Modules
            "User Management": "انتظامِ صارفین",
            "All Users": "تمام صارفین",
            "Faculty": "اساتذہ",
            "Students": "طلبہ",
            "Bulk Upload": "بلک اپ لوڈ",
            "Bulk Upload Users": "صارفین کا بلک اپ لوڈ",
            "Roles": "کردار (Roles)",
            "Roles & Permissions": "کردار اور اختیارات",
            "Role Name": "کردار کا نام",
            "Permissions": "اختیارات",
            "Add User": "نیا صارف شامل کریں",
            "Edit User": "صارف کی تفصیلات میں ترمیم",
            "User Details": "صارف کی تفصیلات",
            "First Name": "پہلا نام",
            "Last Name": "آخری نام",
            "Email Address": "ای میل پتہ",
            "Phone Number": "فون نمبر",
            "Mobile Number": "موبائل نمبر",
            "Department": "شعبہ (Department)",
            "Designation": "عہدہ (Designation)",
            "Division": "ڈویژن",
            "Location": "مقام",
            "Venue": "مقامِ نشست",
            "Employment Type": "ملازمت کی قسم",
            "Academics": "تعلیمی امور",
            "Academic Hub": "تعلیمی مرکز",
            "Content Library": "تعلیمی مواد لائبریری",
            "Faculty Mapping": "اساتذہ کی تفویض",
            "Faculty Map": "نقشہ تفویضِ اساتذہ",
            "Enrollments": "اندراجات / داخلے",
            "Course Enrollment": "کورس اندراج",
            "Quiz Management": "انتظامِ کوئز",
            "Virtual Classroom": "ورچوئل کلاس روم",
            "Virtual Classroom (Zoom)": "ورچوئل کلاس روم (زوم)",
            "Live Virtual Classes": "لائیو ورچوئل کلاسز",
            "Live Virtual Classes (Zoom)": "لائیو ورچوئل کلاسز (زوم)",
            "Class Calendar": "کلاس کا کیلنڈر",
            "Student Attendance": "طلبہ کی حاضری",
            "Student Progress": "طلبہ کی کارکردگی",
            "Discussion Forum": "مباحثہ فورم",
            "Total Students": "کل طلبہ",
            "Total Faculty": "کل اساتذہ",
            "Total Courses": "کل کورسز",
            "Total Materials": "کل تعلیمی مواد",
            "Active Courses": "فعال کورسز",
            "Learning Assets": "تدریسی اثاثے",
            "Recent Users": "حالیہ صارفین",
            "System Overview & Analytics": "نظام کا جائزہ اور تجزیات",
            "Quick Navigation": "فوری نیویگیشن",
            "Manage Programs": "پروگرامز کا انتظام",
            "Manage Courses": "کورسز کا انتظام",
            "Manage Faculty": "اساتذہ کا انتظام",
            "Manage Students": "طلبہ کا انتظام",
            "View All Users": "تمام صارفین دیکھیں",
            "View Academic Hub": "تعلیمی مرکز دیکھیں",
            "Upload Users via CSV/Excel": "سی ایس وی یا ایکسل کے ذریعے صارفین اپ لوڈ کریں",
            "Download Template": "ٹیمپلیٹ ڈاؤن لوڈ کریں",
            "Programs & Curriculum": "پروگرامز اور نصاب",
            "Semester Structure": "سیمسٹر کا ڈھانچہ",
            "Assign Faculty to Course": "کورس میں استاد تفویض کریں",
            "Enroll Students in Course": "کورس میں طلبہ کا اندراج کریں",
            "Batch Enrollment": "بیچ کے حساب سے اندراج",
            "Student ID": "طالب علم کا شناختی کوڈ",
            "Faculty ID": "استاد کا شناختی کوڈ",
            "Course Name": "کورس کا نام",
            "Course Code": "کورس کوڈ",
            "Semester": "سیمسٹر",
            "Program": "پروگرام",
            "Academic Year": "تعلیمی سال",
            "Training Matrix": "تربیتی میٹرکس",
            "Training Schedule": "تربیتی شیڈول",
            "Training Nomination": "تربیتی نامزدگی",
            "Approval Status": "منظوری کی کیفیت",
            "Assigned Date": "تفویض کی تاریخ",
            "Enrolled Date": "اندراج کی تاریخ",

            // Faculty Portal
            "My Courses": "میرے کورسز",
            "My Courses & Curriculum": "میرے کورسز اور نصاب",
            "Assigned Students": "تفویض کردہ طلبہ",
            "Teaching Modules": "تدریسی ماڈیولز",
            "My Teaching Load": "میرا تدریسی بوجھ",
            "Pending Evaluations": "زیرِ التوا جانچ و پڑتال",
            "Pending Assessments": "زیرِ التوا امتحانات",
            "Today's Live Classes": "آج کی لائیو کلاسز",
            "Interactive PPT": "انٹرایکٹو پاورپوائنٹ",
            "Faculty Dashboard": "اساتذہ کا ڈیش بورڈ",
            "Teaching Assignments": "تدریسی ذمہ داریاں",
            "Student Submissions": "طلبہ کی جمع کرائی گئی اسائنمنٹس",
            "View Submissions": "جمع شدہ اسائنمنٹس دیکھیں",
            "Evaluate Submission": "اسائنمنٹ کی جانچ کریں",
            "Grade Submission": "نمبر / گریڈ تفویض کریں",
            "Enter Marks": "نمبر درج کریں",
            "Marks": "نمبرات",
            "Score": "حاصل کردہ اسکور",
            "Percentage": "فیصد",
            "Feedback to Student": "طالب علم کے لیے تاثرات / رائے",
            "Evaluation Date": "جانچ کی تاریخ",
            "Evaluator": "ممتحن / استاد",
            "Start Meeting": "میٹنگ شروع کریں",
            "Join Meeting": "میٹنگ میں شامل ہوں",
            "Meeting Link": "میٹنگ کا لنک",
            "Meeting Password": "میٹنگ کا پاس ورڈ",
            "Zoom Meeting": "زوم میٹنگ",
            "Scheduled Classes": "طے شدہ کلاسز",
            "Upcoming Lectures": "آنے والے لیکچرز",
            "Lecture Title": "لیکچر کا عنوان",
            "Course Materials": "کورس کا درسی مواد",
            "Upload Lecture Notes": "لیکچر نوٹس اپ لوڈ کریں",
            "Upload Slides (PPT)": "پریزنٹیشن سلائیڈز اپ لوڈ کریں",
            "Upload Lecture Video": "لیکچر ویڈیو اپ لوڈ کریں",
            "Student Progress Summary": "طالب علم کی کارکردگی کا خلاصہ",
            "Announcements": "اعلانات",
            "Create Announcement": "نیا اعلان تیار کریں",
            "Post Announcement": "اعلان شائع کریں",
            "Discussion Threads": "مباحثے کے موضوعات",
            "Start Discussion": "نیا مباحثہ شروع کریں",
            "Reply": "جواب دیں",
            "Post Reply": "جواب ارسال کریں",
            "Quick Grade": "فوری گریڈنگ",
            "Assigned Courses": "تفویض کردہ کورسز",
            "Active Students": "فعال طلبہ",

            // Courses & Quadrants
            "Course": "کورس",
            "Courses": "کورسز",
            "Course Details": "کورس کی تفصیلات",
            "Course Curriculum": "کورس کا نصاب",
            "Program Name": "پروگرام کا نام",
            "Degree": "ڈگری",
            "Specialization": "شعبہ اختصاص",
            "Syllabus": "تفصیلی نصاب",
            "Credits": "کریڈٹس",
            "Duration": "مدت",
            "Total Units": "کل یونٹس",
            "Total Lessons": "کل اسباق",
            "Unit": "یونٹ",
            "Units": "یونٹس",
            "Lesson": "سبق",
            "Lessons": "اسباق",
            "Topic": "عنوان",
            "Topics": "عنوانات",
            "4-Quadrant Learning System": "4-کواڈرینٹ تدریسی نظام",
            "Quadrant 1: E-Tutorial": "کواڈرینٹ 1: ای-ٹیوٹوریل",
            "Quadrant 2: E-Content": "کواڈرینٹ 2: ای-مواد",
            "Quadrant 3: Discussion Forum": "کواڈرینٹ 3: مباحثہ فورم",
            "Quadrant 4: Assessment": "کواڈرینٹ 4: خود تشخیصی جانچ",
            "E-Tutorial (Video / Audio)": "ای-ٹیوٹوریل (ویڈیو و آڈیو لیکچرز)",
            "E-Content (Digital Documents & Books)": "ای-مواد (ڈیجیٹل دستاویزات، کتب و ای-بکس)",
            "Discussion & Web Resources": "مباحثہ فورم اور ویب وسائل",
            "Assessment & Quizzes": "جانچ اور امتحانی کوئز",
            "Lecture Notes": "لیکچر نوٹس",
            "Supplementary Materials": "اضافی مطالعاتی مواد",
            "Video Lecture": "ویڈیو لیکچر",
            "Watch Video": "ویڈیو دیکھیں",
            "Video Progress": "ویڈیو دیکھنے کی پیش رفت",
            "Watched Duration": "دیکھا گیا دورانیہ",
            "Document Viewer": "دستاویزات ویوور",
            "Download File": "فائل ڈاؤن لوڈ کریں",
            "File Size": "فائل کا سائز",
            "Upload Date": "اپ لوڈ کی تاریخ",
            "Enrolled Students": "اندراج شدہ طلبہ",
            "Curriculum Structure": "نصابی خاکہ و ڈھانچہ",

            // Attendance
            "Attendance": "حاضری",
            "Attendance Register": "رجسٹرِ حاضری",
            "Mark Attendance": "حاضری لگائیں",
            "Take Attendance": "حاضری درج کریں",
            "Attendance Records": "ریکارڈز حاضری",
            "Attendance Percentage": "فیصد حاضری",
            "Total Lectures Conducted": "کل منعقدہ لیکچرز",
            "Lectures Attended": "حاضر شدہ لیکچرز",
            "Present Count": "حاضر طلبہ کی تعداد",
            "Absent Count": "غیر حاضر طلبہ کی تعداد",
            "Attendance Status": "کیفیتِ حاضری",
            "Attendance Date": "تاریخِ حاضری",
            "Session": "نشست / سیشن",
            "Lecture Hour": "لیکچر کا وقت / گھنٹہ",
            "Student Name": "طالب علم کا نام",
            "Roll Number": "رول نمبر",
            "Mark All Present": "سب کو حاضر نشان زد کریں",
            "Mark All Absent": "سب کو غیر حاضر نشان زد کریں",
            "Attendance Summary": "خلاصہ حاضری",
            "Low Attendance Warning (<75%)": "کم حاضری کا انتباہ (75 فیصد سے کم)",

            // Assessments & Grade Cards
            "Internal Assessments": "داخلی امتحانات (IA)",
            "Internal Assessment": "داخلی امتحان",
            "Internal Grade Cards": "داخلی گریڈ کارڈز",
            "Quiz Assessments": "کوئز امتحانات",
            "Assessments": "امتحانات اور جانچ",
            "Assessment Policy": "امتحانی پالیسی",
            "Assessment Window": "امتحانی میعاد / ونڈو",
            "Assessment Type": "امتحان کی قسم",
            "Assignment": "اسائنمنٹ",
            "Written Examination": "تحریری امتحان",
            "Practical Examination": "عملی امتحان (پریکٹیکل)",
            "Project Work": "پراجیکٹ ورک",
            "Presentation": "پریزنٹیشن",
            "Viva Voce": "زبانی امتحان (وائیوا)",
            "Multiple Choice Questions (MCQ)": "کثیر الانتخابی سوالات (MCQ)",
            "Question Bank": "بینکِ سوالات",
            "Questions": "سوالات",
            "Add Question": "سوال شامل کریں",
            "Edit Question": "سوال میں ترمیم کریں",
            "Question Text": "سوال کی عبارت",
            "Option A": "آپشن الف (A)",
            "Option B": "آپشن ب (B)",
            "Option C": "آپشن ج (C)",
            "Option D": "آپشن د (D)",
            "Correct Answer": "درست جواب",
            "Explanation": "وضاحت / تشریح",
            "Maximum Marks": "زیادہ سے زیادہ نمبر",
            "Passing Marks": "کامیابی کے نمبر",
            "Weightage (%)": "ویٹیج (%)",
            "Start Date": "شروع ہونے کی تاریخ",
            "End Date": "ختم ہونے کی تاریخ",
            "Due Date": "آخری تاریخ",
            "Late Submission Allowed": "تاخیر سے جمع کرانے کی اجازت",
            "Publish Assessment": "امتحان شائع کریں",
            "Unpublish Assessment": "امتحان غیر شائع کریں",
            "Publish Results": "نتائج شائع کریں",
            "Hide Results": "نتائج پوشیدہ کریں",
            "Request Result Approval": "نتائج کی منظوری کی درخواست کریں",
            "Approve Results": "نتائج منظور کریں",
            "Reject Results": "نتائج مسترد کریں",
            "Grade Questions": "سوالات کے نمبر درج کریں",
            "Grade Cards": "گریڈ کارڈز",
            "Semester Grade Card": "سیمسٹر گریڈ کارڈ",
            "Preview Grade Card": "گریڈ کارڈ کا پیش منظر",
            "Print Grade Statement": "گریڈ کی تفصیلی رپورٹ پرنٹ کریں",
            "Student Marks": "طلبہ کے نمبرات",
            "Marks Obtained": "حاصل کردہ نمبرات",
            "Result Status": "کیفیتِ نتیجہ",
            "Grade": "گریڈ",
            "Grade Point": "گریڈ پوائنٹ",
            "SGPA": "ایس جی پی اے (SGPA)",
            "CGPA": "سی جی پی اے (CGPA)",
            "Attempt": "کوشش / امتحان میں شرکت",
            "Attempt Date": "امتحان کی تاریخ",
            "Submission Date": "جمع کرانے کی تاریخ",
            "Submitted File": "جمع کردہ فائل",
            "Evaluation Status": "جانچ کی کیفیت",
            "Graded": "جانچ مکمل",
            "Ungraded": "جانچ باقی ہے",
            "Minimum passing threshold: 33%": "کامیابی کا کم از کم معیار: 33 فیصد",

            // Academic Hub & Course Structure
            "Programs • Courses • Curriculum Hierarchy": "پروگرامز • کورسز • نصابی درجہ بندی",
            "Centralized command center for managing Degree Programs, Course Catalog, and Syllabus Structure": "ڈگری پروگرامز، کورس کیٹلاگ اور نصاب کے نظم کا مرکزی پلیٹ فارم۔",
            "Centralized command center for managing Degree Programs, Course Catalog, and Syllabus Structure.": "ڈگری پروگرامز، کورس کیٹلاگ اور نصاب کے نظم کا مرکزی پلیٹ فارم۔",
            "Programs Directory": "پروگرامز ڈائریکٹری",
            "Course Catalog": "کورس کیٹلاگ",
            "Curriculum Hierarchy": "نصابی درجہ بندی",
            "Tree View": "درختی منظر (Tree View)",
            "Total Credits": "کل کریڈٹس",
            "Semesters": "سیمسٹرز",
            "Programs": "پروگرامز",
            "Courses": "کورسز",
            "New Program": "نیا پروگرام",
            "Add Course & Structure": "کورس اور نصاب شامل کریں",
            "Add Course": "کورس شامل کریں",
            "Edit Program": "پروگرام میں ترمیم کریں",
            "Edit Program Details": "پروگرام کی تفصیلات میں ترمیم کریں",
            "No programs available": "کوئی پروگرام دستیاب نہیں ہے",
            "Create an active program before building the curriculum.": "نصاب تشکیل دینے سے پہلے کوئی فعال پروگرام بنائیں",
            "No Courses Assigned Yet": "ابھی تک کوئی کورس تفویض نہیں کیا گیا",
            "You have not been assigned to any courses in the Faculty Course Mapping yet. Please contact your administrator to map your subjects.": "آپ کو ابھی تک فیکلٹی کورس میپنگ میں کوئی کورس تفویض نہیں کیا گیا۔ برائے مہربانی اپنے ایڈمنسٹریٹر سے رابطہ کریں۔",
            "No curriculum available": "کوئی نصاب دستیاب نہیں ہے",
            "Select a program to view its curriculum structure.": "نصابی ڈھانچہ دیکھنے کے لیے کسی پروگرام کا انتخاب کریں۔",
            "Search courses by title or code...": "عنوان یا کوڈ سے کورس تلاش کریں...",
            "All Programs": "تمام پروگرامز",
            "All Semesters": "تمام سیمسٹرز",
            "No program data available": "پروگرام کا کوئی ڈیٹا دستیاب نہیں ہے",
            "Select another program and try again.": "کسی دوسرے پروگرام کا انتخاب کر کے دوبارہ کوشش کریں۔",
            "Add Course to this Program": "اس پروگرام میں کورس شامل کریں",
            "No semester structure found": "کوئی سیمسٹر ڈھانچہ نہیں ملا",
            "No semesters or courses are currently defined for": "کے لیے فی الحال کوئی سیمسٹر یا کورس وضع نہیں کیا گیا ہے۔",
            "Add First Course": "پہلا کورس شامل کریں",
            "Content Item": "تعلیمی مواد",
            "Content Items": "تعلیمی مواد",
            "Content": "مواد",
            "Credit": "کریڈٹ",
            "Credits": "کریڈٹس",
            "Cr": "کریڈٹ",
            "Mo": "ماہ",
            "Assign Faculty / Instructors": "اساتذہ / فیکلٹی تفویض کریں",
            "View assigned content": "تفویض کردہ مواد دیکھیں",
            "Bachelor of Computer Applications (BCA)": "بیچلر آف کمپیوٹر ایپلیکیشنز (BCA)",
            "Bachelor of Business Administration (BBA)": "بیچلر آف بزنس ایڈمنسٹریشن (BBA)",
            "Master of Business Administration (MBA)": "ماسٹر آف بزنس ایڈمنسٹریشن (MBA)",
            "Master of Computer Applications (MCA)": "ماسٹر آف کمپیوٹر ایپلیکیشنز (MCA)",
            "Business Studies": "بزنس اسٹڈیز",
            "Principles and Practice of Management": "پرنسپلز اینڈ پریکٹس آف مینجمنٹ",
            "The Bachelor of Business Administration (BBA) programme is designed to develop a strong foundation in business and management principles": "بیچلر آف بزنس ایڈمنسٹریشن (BBA) پروگرام کاروبار اور انتظامی اصولوں کی مضبوط بنیاد تیار کرنے کے لیے ڈیزائن کیا گیا ہے۔",
            "The Bachelor of Computer Applications (BCA) program is designed to build foundational computer science and software development skills.": "بیچلر آف کمپیوٹر ایپلیکیشنز (BCA) پروگرام کمپیوٹر سائنس اور سافٹ ویئر ڈویلپمنٹ کی بنیادی مہارتیں فراہم کرنے کے لیے وضع کیا گیا ہے۔",

            // Internal Assessments Faculty & Admin
            "Internal Assessments — Faculty": "داخلی امتحانات — فیکلٹی",
            "Internal Assessments — Admin": "داخلی امتحانات — ایڈمن",
            "Create, publish, evaluate and submit results for Admin approval.": "امتحان بنائیں، شائع کریں، جانچ کریں اور نتائج ایڈمن کی منظوری کے لیے جمع کریں۔",
            "Create, publish, evaluate and submit results for Admin approval": "امتحان بنائیں، شائع کریں، جانچ کریں اور نتائج ایڈمن کی منظوری کے لیے جمع کریں۔",
            "Create Internal Assessment": "نیا داخلی امتحان بنائیں",
            "Enter assessment title": "امتحان کا عنوان درج کریں",
            "Assessment types (multiple)": "امتحان کی اقسام (ایک سے زائد)",
            "Select assessment types": "امتحان کی اقسام کا انتخاب کریں",
            "Select all": "سب منتخب کریں",
            "Clear": "صاف کریں",
            "Max marks": "زیادہ سے زیادہ نمبر",
            "Choose course": "کورس کا انتخاب کریں",
            "Subject (optional)": "مضمون (اختیاری)",
            "All subjects": "تمام مضامین",
            "Admin-approved assessment window": "ایڈمن سے منظور شدہ امتحانی ونڈو",
            "Due date & time": "آخری تاریخ اور وقت",
            "Choose active schedule": "فعال امتحانی شیڈول منتخب کریں",
            "Select a schedule to see its date range.": "تاریخ کا دورانیہ دیکھنے کے لیے شیڈول منتخب کریں۔",
            "Select a schedule to see its date range": "تاریخ کا دورانیہ دیکھنے کے لیے شیڈول منتخب کریں۔",
            "Use schedule end": "شیڈول کی آخری تاریخ استعمال کریں",
            "Allow late submission": "تاخیر سے جمع کرانے کی اجازت دیں",
            "Instructions": "ہدایات",
            "Enter instructions for students": "طلبہ کے لیے ہدایات درج کریں",
            "Create Draft": "مسودہ بنائیں",
            "Created / Assigned Assessments": "تخلیق کردہ / تفویض کردہ امتحانات",
            "Legacy semester-wide": "سیمسٹر کے مطابق",
            "Other course": "دیگر کورس",

            // Faculty Quiz Assessments & Evaluations
            "EVALUATION PORTAL": "پورٹل برائے جانچ و تشخیص",
            "Evaluation Portal": "پورٹل برائے جانچ و تشخیص",
            "Faculty Quiz Assessments": "فیکلٹی کوئز امتحانات",
            "Review student submissions, evaluate subjective answers, and record official marks.": "طلبہ کے جوابات کی جانچ کریں، تشریحی سوالات کا جائزہ لیں اور سرکاری نمبرات درج کریں۔",
            "Review student submissions, evaluate subjective answers, and record official marks": "طلبہ کے جوابات کی جانچ کریں، تشریحی سوالات کا جائزہ لیں اور سرکاری نمبرات درج کریں۔",
            "Submissions": "جمع کرائے گئے امتحانات",
            "SUBMISSIONS": "جمع کرائے گئے امتحانات",
            "Evaluated": "جانچ مکمل",
            "EVALUATED": "جانچ مکمل",
            "PENDING": "زیرِ التوا",
            "Create New Quiz": "نیا کوئز بنائیں",
            "Manage Quizzes": "کوئز کا نظم کریں",
            "Student Attempts List": "طلبہ کی امتحانی کوششوں کی فہرست",
            "Search student, course, or quiz...": "طالب علم، کورس یا کوئز تلاش کریں...",
            "Search student, course, or quiz": "طالب علم، کورس یا کوئز تلاش کریں...",
            "...Search student, course, or quiz": "طالب علم، کورس یا کوئز تلاش کریں...",
            "Quadrant": "کواڈرینٹ (Quadrant)",
            "QUADRANT": "کواڈرینٹ (Quadrant)",
            "Quiz Title": "کوئز کا عنوان",
            "QUIZ TITLE": "کوئز کا عنوان",
            "Submitted On": "جمع کرانے کا وقت",
            "SUBMITTED ON": "جمع کرانے کا وقت",
            "Score": "حاصل کردہ اسکور",
            "SCORE": "حاصل کردہ اسکور",
            "No Student Submissions Yet": "ابھی تک طلبہ کی کوئی گذارشات موصول نہیں ہوئیں",
            "When students attempt quizzes for your assigned courses, their submissions will appear here.": "جب طلبہ آپ کے تفویض کردہ کورسز کے کوئز حل کریں گے تو ان کی گذارشات یہاں ظاہر ہوں گی۔",
            "Pending Evaluation": "جانچ باقی ہے",
            "Evaluate / Review": "جانچ / جائزہ لیں",
            "Evaluate": "جانچ کریں",
            "Review": "جائزہ لیں",

            // Dashboard Faculty & Admin
            "Assigned Curriculum": "تفویض کردہ نصاب",
            "Content Hub": "مرکزِ تعلیمی مواد",
            "Assessment Evaluations": "امتحانات کی جانچ",
            "Access and upload lecture materials, PDFs, and assignments for your courses.": "اپنے کورسز کے لیے درسی مواد، پی ڈی ایف اور اسائنمنٹس اپ لوڈ کریں۔",
            "Grade subjective answers and review student checkpoint test attempts.": "تشریحی جوابات کی جانچ کریں اور طلبہ کے ٹیسٹ کا جائزہ لیں۔",
            "MY COURSES": "میرے کورسز",
            "MY STUDENTS": "میرے طلبہ",
            "PENDING ASSESSMENTS": "زیرِ التوا امتحانات",
            "COURSE MATERIALS": "کورس کا مواد",
            "Active Modules": "فعال ماڈیولز",
            "Active Learners": "فعال طلبہ",
            "Submissions to Grade": "جانچ کے لیے گذارشات",
            "Content Resources": "تعلیمی وسائل",
            "Assigned": "تفویض شدہ",
            "Enrolled": "اندراج شدہ",
            "Resources": "وسائل",
            "TOTAL STUDENTS": "کل طلبہ",
            "TOTAL FACULTY": "کل فیکلٹی",
            "TOTAL COURSES": "کل کورسز",
            "LIBRARY ASSETS": "لائبریری اثاثے",
            "Registered Learners": "رجسٹرڈ طلبہ",
            "Active Educators": "فعال اساتذہ",
            "Instituted Degrees": "قائم کردہ ڈگریاں",
            "Resource Mappings": "مواد کی میپنگ",
            "My Assigned Courses": "میرے تفویض کردہ کورسز",
            "Courses and syllabus modules assigned to your teaching workload.": "آپ کے تدریسی کام کے لیے تفویض کردہ کورسز اور نصاب کے ماڈیولز۔",
            "Course Title": "کورس کا عنوان",
            "General Program": "جنرل پروگرام",
            "Faculty Quick Actions": "فیکلٹی کے فوری اقدامات",
            "Shortcuts to frequently used instructor workflows.": "بکثرت استعمال ہونے والے تدریسی امور کے شارٹ کٹس۔",
            "Platform Maintenance": "پلیٹ فارم کی دیکھ بھال",
            "System Status": "نظام کی کیفیت",
            "No active threats or outages. Background operations are proceeding within bounds.": "کوئی خلل یا رکاوٹ نہیں۔ پس پردہ تمام سرگرمیاں درست طور پر کام کر رہی ہیں۔",
            "Institution Overview": "ادارے کا جائزہ",
            "Manage users, program definitions, and academic catalogs via shortcuts.": "شارٹ کٹس کے ذریعے صارفین، پروگرامز اور تعلیمی کیٹلاگ کا نظم کریں۔",
            "No assigned courses found yet. Please contact admin to map courses to your account.": "ابھی تک کوئی تفویض کردہ کورس نہیں ملا۔ براہ کرم اپنے ایڈمن سے رابطہ کریں۔",
            "Here's an overview of your assigned courses, enrolled learners, and active assessments.": "یہاں آپ کے تفویض کردہ کورسز، زیر تعلیم طلبہ اور فعال امتحانات کا ایک جائزہ پیش ہے۔",
            "Here's an overview of your administrative platform management.": "یہاں آپ کے انتظامی امور اور پلیٹ فارم کے نظم کا جائزہ پیش ہے۔",

            // Faculty PPT
            "Manage Interactive PPTs": "انٹرایکٹیو پی پی ٹیز کا انتظام",
            "View and manage your uploaded PowerPoint presentations": "اپنی اپ لوڈ کردہ پاورپوائنٹ پریزنٹیشنز دیکھیں اور ان کا انتظام کریں۔",
            "Upload New PPT": "نئی پی پی ٹی اپ لوڈ کریں",
            "Total PPTs": "کل پی پی ٹیز",
            "TOTAL PPTS": "کل پی پی ٹیز",
            "Total Size": "کل سائز",
            "TOTAL SIZE": "کل سائز",
            "All Courses": "تمام کورسز",
            "All Status": "تمام کیفیات",
            "Search PPT title...": "پی پی ٹی کا عنوان تلاش کریں...",
            "Search PPT title": "پی پی ٹی کا عنوان تلاش کریں",
            "No PPTs Uploaded Yet": "ابھی تک کوئی پی پی ٹی اپ لوڈ نہیں کی گئی",
            "Upload your first Interactive PowerPoint presentation to get started.": "شروع کرنے کے لیے اپنی پہلی انٹرایکٹیو پاورپوائنٹ پریزنٹیشن اپ لوڈ کریں۔",
            "Upload your first Interactive PowerPoint presentation to get started": "شروع کرنے کے لیے اپنی پہلی انٹرایکٹیو پاورپوائنٹ پریزنٹیشن اپ لوڈ کریں۔",
            "Upload PPT": "پی پی ٹی اپ لوڈ کریں",
            "PPT Title": "پی پی ٹی کا عنوان",
            "Course Details": "کورس کی تفصیلات",
            "Unit & Topic": "یونٹ اور عنوان",
            "File Info": "فائل کی معلومات",
            "Uploaded": "اپ لوڈ شدہ",
            "View Slides (Student Preview)": "سلائیڈز دیکھیں (طالب علم پریویو)",
            "Manage Slide Interactions": "سلائیڈ انٹرایکشنز کا انتظام",
            "Edit PPT": "پی پی ٹی میں ترمیم کریں",
            "Delete PPT": "پی پی ٹی حذف کریں",

            // Content / Central Library & Quadrants
            "Central Library": "مرکزی لائبریری",
            "All uploaded content": "تمام اپ لوڈ کردہ مواد",
            "From Library": "لائبریری سے",
            "Upload New": "نیا اپ لوڈ کریں",
            "Upload New File": "نئی فائل اپ لوڈ کریں",
            "Upload Content": "مواد اپ لوڈ کریں",
            "Browse Central Library": "مرکزی لائبریری براؤز کریں",
            "Global Content Center": "مرکزی مواد کا مرکز",
            "A centralized repository to manage all course materials. Upload once, assign anywhere.": "کورس کے تمام مواد کے انتظام کا مرکزی ذخیرہ۔ ایک بار اپ لوڈ کریں، کہیں بھی تفویض کریں۔",
            "No content assigned to this course yet": "ابھی تک اس کورس کے لیے کوئی مواد تفویض نہیں کیا گیا",
            "Search by title, file name...": "عنوان، فائل کے نام سے تلاش کریں...",
            "Learning Quadrants": "تدریسی کواڈرینٹس",
            "Filter by quadrant view": "کواڈرینٹ کے لحاظ سے فلٹر کریں",
            "4 Quadrants Architecture": "4-کواڈرینٹ نظام",
            "Quadrants Architecture 4": "4-کواڈرینٹ نظام",
            "Four Quadrant Structure": "چار کواڈرینٹس کی ساخت",
            "All Content": "تمام مواد",
            "Q1 - PDF / Notes": "Q1 - پی ڈی ایف / نوٹس",
            "Q2 - Video": "Q2 - ویڈیو",
            "Q3 - Assignment": "Q3 - اسائنمنٹ",
            "Q4 - Discussion": "Q4 - مباحثہ",
            "Q1 - PDF/Notes": "Q1 - پی ڈی ایف / نوٹس",
            "Q2 - VIDEO": "Q2 - ویڈیو",
            "Q3 - ASSIGNMENT": "Q3 - اسائنمنٹ",
            "Q4 - DISCUSSION": "Q4 - مباحثہ",
            "General": "عمومی",
            "VIDEO URL": "ویڈیو لنک",
            "Video URL": "ویڈیو لنک",
            "Assign to Course": "کورس میں تفویض کریں",
            "Select Destination Course": "منزل کا کورس منتخب کریں",
            "Assign to Quadrant": "کواڈرینٹ میں تفویض کریں",
            "-- No Quadrant --": "-- کوئی کواڈرینٹ نہیں --",
            "Assign Now": "ابھی تفویض کریں",
            "Assign to another course": "کسی دوسرے کورس میں تفویض کریں",
            "Edit content": "مواد میں ترمیم کریں",
            "File Preview": "فائل کا پیش نظارہ",
            "Close Preview": "پیش نظارہ بند کریں",
            "Loading Global Library...": "مرکزی لائبریری لوڈ ہو رہی ہے...",

            // Internal Grade Cards
            "Internal Assessment: Grade Cards": "داخلی امتحانات: گریڈ کارڈز",
            "Internal Assessment Grade Cards": "داخلی امتحانی گریڈ کارڈز",
            "View subject-wise or semester-wise published Internal Assessment marks statements": "مضمون وار یا سیمسٹر وار شائع شدہ داخلی امتحانات کے تفصیلی نمبرات دیکھیں۔",
            "Available Grade Cards": "دستیاب گریڈ کارڈز",
            "Published Assessment Entries": "شائع شدہ امتحانی اندراجات",
            "Evaluated Entries": "جانچ شدہ اندراجات",
            "Faculty Grade Cards": "فیکلٹی گریڈ کارڈز",
            "All Student Grade Cards": "تمام طلبہ کے گریڈ کارڈز",
            "My Grade Cards": "میرے گریڈ کارڈز",
            "No published internal grade cards available": "کوئی شائع شدہ داخلی گریڈ کارڈ دستیاب نہیں ہے",
            "Grade cards appear after faculty publishes internal assessment results.": "گریڈ کارڈز فیکلٹی کی جانب سے داخلی امتحانی نتائج شائع کرنے کے بعد ظاہر ہوتے ہیں۔",
            "Faculty Marks Entry": "فیکلٹی نمبرات کا اندراج",
            "Student": "طالب علم",
            "STUDENT": "طالب علم",
            "Outcome": "نتیجہ",
            "OUTCOME": "نتیجہ",
            "Subject-wise": "مضمون وار",
            "Semester-wise": "سیمسٹر وار",
            "Student ID": "طالب علم شناختی نمبر (ID)",
            "Student ID:": "طالب علم شناختی نمبر (ID):",

            // Student Portal & Sidebar
            "Student Portal": "طلبہ پورٹل",
            "Learn": "تعلیم / سیکھیں",
            "Syllabus": "نصاب",
            "Class Calendar": "کلاس کا کیلنڈر",
            "Assignments": "اسائنمنٹس",
            "Assessments & Exams": "امتحانات و آزمائشیں",
            "Assessments &amp; Exams": "امتحانات و آزمائشیں",
            "My Grade Card": "میرا گریڈ کارڈ",
            "Virtual Classroom": "ورچوئل کلاس روم",
            "My Attendance": "میری حاضری",
            "My Progress Report": "میری کارکردگی رپورٹ",
            "Collaborate & Communication": "تعاون اور رابطہ",
            "Collaborate &amp; Communication": "تعاون اور رابطہ",
            "Notification & Inbox": "اطلاعات و ان باکس",
            "Notification &amp; Inbox": "اطلاعات و ان باکس",
            "Discussion Forum": "مباحثہ فورم",
            "Announcements": "اعلانات",
            "Wiki": "وکی (Wiki)",
            "Swayam": "سوئم (Swayam)",
            "Google": "گوگل",
            "Microsoft": "مائیکروسافٹ",
            "Chat & Meeting": "بات چیت اور میٹنگ",
            "Chat &amp; Meeting": "بات چیت اور میٹنگ",
            "Blog": "بلاگ",
            "Interactive PPT": "انٹرایکٹیو پاورپوائنٹ",
            "Empowering": "تعلیمی ترقی",
            "Education Together": "باہمی ساتھ",
            "Education<br/>Together": "باہمی ساتھ",
            "Student Dashboard": "طالب علم ڈیش بورڈ",

            // Student Dashboard Metrics & Cards
            "Learning Progress": "تعلیمی پیشرفت",
            "Topics Consumed": "موضوعات مکمل",
            "topics consumed": "موضوعات مکمل",
            "My Documents": "میری دستاویزات",
            "View enrolled courses & files": "داخل کردہ کورسز اور فائلیں دیکھیں",
            "Job Inbox": "ملازمتوں کا ان باکس",
            "Placement Cell": "پلیسمنٹ سیل",
            "Check latest job openings": "ملازمت کے تازہ ترین مواقع دیکھیں",
            "Mission:": "مقصد:",
            "To provide an effective environment for teaching-learning and innovation in order to transform our students into dynamic, professionally sound and socially responsible citizens with ability to adapt to the changing needs of the industry and society.": "تدریس و تعلیم اور جدت طرازی کے لیے ایک موثر ماحول فراہم کرنا تاکہ ہمارے طلبہ کو صنعت اور معاشرے کی بدلتی ہوئی ضروریات کے مطابق متحرک، پیشہ ورانہ طور پر مضبوط اور سماجی طور پر ذمہ دار شہری بنایا جا سکے۔",
            "University Roll No.": "یونیورسٹی رول نمبر",
            "Section": "سیکشن",
            "Library Code": "لائبریری کوڈ",
            "Mobile No.": "موبائل نمبر",
            "Birth Date": "تاریخ پیدائش",
            "Address": "پتہ",
            "Today's Exams": "آج کے امتحانات",
            "No exams scheduled.": "کوئی امتحان شیڈول نہیں ہے۔",
            "Utilize your free time wisely — a perfect opportunity to prepare for your upcoming exams.": "اپنے فارغ وقت کو دانشمندی سے استعمال کریں — اپنے آنے والے امتحانات کی تیاری کا بہترین موقع۔",
            "Today's Virtual Classes": "آج کی ورچوئل کلاسز",
            "No Virtual Classes Today": "آج کوئی ورچوئل کلاس نہیں ہے",
            "Recent Announcements": "حالیہ اعلانات",
            "No announcements.": "کوئی نیا اعلان نہیں ہے۔",
            "Stay tuned for any updates from your admin or faculty.": "ایڈمن یا فیکلٹی کی جانب سے نئی معلومات کے لیے باخبر رہیں۔",
            "View All": "سب دیکھیں",
            "Total Courses": "کل کورسز",
            "Enrolled Courses": "داخل شدہ کورسز",
            "Exams": "امتحانات",
            "Results": "نتائج",
            "View score reports": "نمبرات کی رپورٹ دیکھیں",
            "Passing Rate": "کامیابی کا تناسب",
            "Based on all exams attempted": "تمام دیے گئے امتحانات کی بنیاد پر",
            "My Enrolled Courses": "میرے داخل شدہ کورسز",
            "View All Courses": "تمام کورسز دیکھیں",
            "Continue Learning": "تعلیم جاری رکھیں",
            "View Syllabus & Content": "نصاب اور مواد دیکھیں",
            "Course Progress": "کورس کی پیشرفت",
            "Finished": "مکمل",
            "Done": "مکمل",
            "You are not enrolled in any courses yet.": "آپ ابھی تک کسی کورس میں داخل نہیں ہیں۔",
            "Contact your admin to get started with your learning journey.": "اپنا تعلیمی سفر شروع کرنے کے لیے اپنے ایڈمن سے رابطہ کریں۔",
            "Instruction & Support": "ہدایات اور مدد",
            "Frequently Asked Questions": "عام طور پر پوچھے جانے والے سوالات",
            "Progress Report": "پیشرفت رپورٹ",
            "View Full Report": "مکمل رپورٹ دیکھیں",
            "Quiz": "کوئز",
            "Assignment": "اسائنمنٹ",
            "Study Material": "مطالعہ کا مواد",
            "E-Content": "ای-مواد",

            // Student Courses & Units
            "Semester Subjects": "سیمسٹر کے مضامین",
            "Subjects": "مضامین",
            "Browse units and explore their learning content.": "یونٹس براؤز کریں اور ان کا تعلیمی مواد دیکھیں۔",
            "Units": "یونٹس",
            "Materials": "مواد کے وسائل",
            "Progress": "پیشرفت",
            "Category": "زمرہ",
            "Credits": "کریڈٹس",
            "Duration": "مدت",
            "Code": "کوڈ",
            "Course Units": "کورس کے یونٹس",
            "Open": "کھولیں",
            "No units have been added to this course yet. Content will appear here once your faculty uploads materials.": "ابھی تک اس کورس میں کوئی یونٹ شامل نہیں کیا گیا۔ فیکلٹی کی جانب سے مواد اپ لوڈ کرتے ہی یہاں ظاہر ہوگا۔",

            // Student Exams & Quizzes Hub
            "Assessments & Quizzes": "امتحانات اور کوئزز",
            "Assessments &amp; Quizzes": "امتحانات اور کوئزز",
            "All your scheduled exams, evaluations, topic tests and quizzes in one single hub.": "آپ کے تمام شیڈول امتحانات، جائزے، ٹیسٹ اور کوئزز ایک ہی مرکز میں۔",
            "Formal Exams": "باقاعدہ امتحانات",
            "active now": "ابھی فعال ہیں",
            "Course Quizzes": "کورس کوئزز",
            "Faculty created": "فیکلٹی کا تیار کردہ",
            "Quizzes Completed": "مکمل شدہ کوئزز",
            "Evaluated submissions": "جانچے گئے جوابات",
            "Total Tests Available": "کل دستیاب ٹیسٹ",
            "Overall learning checks": "مجموعی تعلیمی جانچ",
            "All Assessments & Quizzes": "تمام امتحانات اور کوئزز",
            "All Assessments &amp; Quizzes": "تمام امتحانات اور کوئزز",
            "Formal Exams & Scheduled Assessments": "باقاعدہ امتحانات اور شیڈول جائزے",
            "Formal Exams &amp; Scheduled Assessments": "باقاعدہ امتحانات اور شیڈول جائزے",
            "Scheduled": "شیڈول شدہ",
            "No Scheduled Exams": "کوئی شیڈول شدہ امتحان نہیں ہے",
            "You have no pending formal exams assigned currently.": "اس وقت آپ کا کوئی باقاعدہ امتحان زیر التوا نہیں ہے۔",
            "Take Exam": "امتحان شروع کریں",
            "Start Quiz": "کوئز شروع کریں",
            "View Result": "نتیجہ دیکھیں",
            "Questions": "سوالات",
            "Total Marks": "کل نمبرات",
            "Passing Marks": "کامیابی کے نمبرات",
            "Duration (Mins)": "وقت (منٹ)",
            "Start Date": "شروع ہونے کی تاریخ",
            "End Date": "ختم ہونے کی تاریخ",

            // Student Attendance
            "My Attendance Log": "میری حاضری کا ریکارڈ",
            "Keep track of your presence on the platform.": "پلیٹ فارم پر اپنی حاضری اور شرکت کا ریکارڈ رکھیں۔",
            "Current Rate": "موجودہ شرح",
            "Quick Stats": "فوری شماریات",
            "Total Days Tracked": "کل ٹریک شدہ دن",
            "Days Present": "حاضر دن",
            "Late Arrivals": "دیر سے آمد",
            "Absences": "غیر حاضر دن",
            "Attendance Policy": "حاضری کی پالیسی",
            "Your attendance is marked automatically when you log into the platform. A minimum of 75% attendance is recommended for optimal learning.": "پلیٹ فارم پر لاگ ان ہوتے ہی آپ کی حاضری خود بخود لگ جاتی ہے۔ بہترین تعلیمی نتائج کے لیے کم از کم 75 فیصد حاضری ضروری ہے۔",
            "Recent Activity": "حالیہ سرگرمی",
            "View Full History": "مکمل تاریخچہ دیکھیں",
            "Login Time": "لاگ ان کا وقت",
            "Remarks": "کیفیات / ریمارکس",
            "No attendance data available yet.": "ابھی تک حاضری کا کوئی ریکارڈ دستیاب نہیں ہے۔",

            // Student PPTs & Progress
            "Your available presentations across enrolled courses.": "آپ کے داخل شدہ کورسز کی دستیاب پریزنٹیشنز۔",
            "No Interactive PPT available.": "کوئی انٹرایکٹیو پی پی ٹی دستیاب نہیں ہے۔",
            "Overall Progress": "مجموعی پیشرفت",
            "Track your overall course completion and activity": "اپنی مجموعی کورس کی تکمیل اور تعلیمی سرگرمیوں کو ٹریک کریں"
        }
    };

    // ── Helper: Safe Cookie Management ───────────────────────
    function getCookie(name) {
        const matches = document.cookie.match(new RegExp('(?:^|; )' + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + '=([^;]*)'));
        return matches ? decodeURIComponent(matches[1]) : null;
    }

    function setCookie(name, value, days) {
        let expires = '';
        if (days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = '; expires=' + date.toUTCString();
        }
        document.cookie = name + '=' + encodeURIComponent(value) + expires + '; path=/; SameSite=Lax';
    }

    // ── Determine Current Authenticated User Key ────────────
    function getUserId() {
        const userEl = document.querySelector('[data-user-id]');
        if (userEl && userEl.dataset.userId) return userEl.dataset.userId.trim();
        const metaUser = document.querySelector('meta[name="current-user-id"]');
        if (metaUser && metaUser.content) return metaUser.content.trim();
        return '';
    }

    function getUserStorageKey() {
        const uid = getUserId();
        return uid ? 'lms_lang_' + uid : 'lms_lang';
    }

    // ── Language Detection ───────────────────────────────────
    function detectInitialLanguage() {
        const userKey = getUserStorageKey();
        let stored = null;
        try {
            if (typeof localStorage !== 'undefined') {
                stored = localStorage.getItem(userKey) || localStorage.getItem('lms_lang');
            }
        } catch (e) {}
        if (stored === 'ur' || stored === 'en') return stored;

        const uid = getUserId();
        const cookieLang = (uid ? getCookie('lms_lang_' + uid) : null) || getCookie('lms_lang');
        if (cookieLang === 'ur' || cookieLang === 'en') return cookieLang;

        // Check HTML attribute if set by server
        const htmlLang = document.documentElement.getAttribute('lang');
        if (htmlLang === 'ur') return 'ur';

        return 'en'; // English is default
    }

    let currentLanguage = detectInitialLanguage();

    // ── Build Fast Lookup Maps & Inverted Dictionaries ────────
    const lowerMap = {};
    const reverseMap = {};
    const reverseLowerMap = {};

    function initLookupMaps() {
        const urDict = dictionaries.ur || {};
        const enDict = dictionaries.en || {};

        for (const [key, val] of Object.entries(urDict)) {
            lowerMap[key.toLowerCase()] = val;
            if (!reverseMap[val] || (key !== key.toUpperCase() && reverseMap[val] === reverseMap[val].toUpperCase())) {
                reverseMap[val] = key;
            }
            reverseLowerMap[val.toLowerCase()] = key;
        }

        for (const [key, val] of Object.entries(enDict)) {
            const lk = key.toLowerCase();
            if (!lowerMap[lk] && urDict[key]) {
                lowerMap[lk] = urDict[key];
            }
        }
    }
    initLookupMaps();

    // Dynamic patterns with numbers/names/dates
    const enToUrPatterns = [
        { regex: /^Welcome back,\s*([^!]+)!?$/i, replace: 'خوش آمدید، $1!' },
        { regex: /^Last Login:\s*(.+)$/i, replace: 'آخری لاگ ان: $1' },
        { regex: /^Assessment Evaluations\s+(\d+)\s+Pending$/i, replace: 'امتحانات کی جانچ $1 زیرِ التوا' },
        { regex: /^(\d+)\s+Pending$/i, replace: '$1 زیرِ التوا' },
        { regex: /^Pending\s+(\d+)$/i, replace: '$1 زیرِ التوا' },
        { regex: /^(\d+)\s+Submissions?$/i, replace: '$1 جمع کردہ امتحانات' },
        { regex: /^Submissions?\s+(\d+)$/i, replace: '$1 جمع کردہ امتحانات' },
        { regex: /^(\d+)\s+Evaluated$/i, replace: '$1 جانچ مکمل' },
        { regex: /^Evaluated\s+(\d+)$/i, replace: '$1 جانچ مکمل' },
        { regex: /^Semesters?\s+(\d+)$/i, replace: '$1 سیمسٹرز' },
        { regex: /^(\d+)\s+Semesters?$/i, replace: '$1 سیمسٹرز' },
        { regex: /^(\d+)\s+Sem\b/i, replace: '$1 سیمسٹر' },
        { regex: /^Total Credits\s+(\d+)$/i, replace: 'کل کریڈٹس $1' },
        { regex: /^(\d+)\s+Credits?$/i, replace: '$1 کریڈٹس' },
        { regex: /^Credits\s+(\d+)$/i, replace: 'کریڈٹس $1' },
        { regex: /^(\d+)\s+Cr\b/i, replace: '$1 کریڈٹ' },
        { regex: /^(\d+)\s+Mo\b/i, replace: '$1 ماہ' },
        { regex: /^Mo\s+(\d+)$/i, replace: 'ماہ $1' },
        { regex: /^Programs?\s+(\d+)$/i, replace: '$1 پروگرامز' },
        { regex: /^(\d+)\s+Programs?$/i, replace: '$1 پروگرامز' },
        { regex: /^Courses?\s+(\d+)$/i, replace: '$1 کورسز' },
        { regex: /^(\d+)\s+Courses?$/i, replace: '$1 کورسز' },
        { regex: /^(?:content resource\(s\)\s*(\d+)|(\d+)\s*content resource\(s\))$/i, replace: '$1$2 تعلیمی وسائل' },
        { regex: /^Content Items?\s+(\d+)$/i, replace: '$1 تعلیمی مواد' },
        { regex: /^(\d+)\s+Content Items?$/i, replace: '$1 تعلیمی مواد' },
        { regex: /^Content\s+(\d+)$/i, replace: '$1 تعلیمی مواد' },
        { regex: /^(\d+)\s+Content\b/i, replace: '$1 تعلیمی مواد' },
        { regex: /^Semester\s+(\d+)$/i, replace: 'سیمسٹر $1' },
        { regex: /^(\d+)\s+Marks$/i, replace: '$1 نمبرات' },
        { regex: /^All Programs\s*\(\d+\)$/i, replace: 'تمام پروگرامز' },
        { regex: /^No courses added to\s*(.+)\s*yet\.?$/i, replace: 'ابھی تک $1 میں کوئی کورس شامل نہیں کیا گیا۔' },
        { regex: /^Add Course to\s*(.+)$/i, replace: '$1 میں کورس شامل کریں' },
        { regex: /^(?:MB\s*([\d\.]+|[\d,]+)|([\d\.]+|[\d,]+)\s*MB)$/i, replace: '$1$2 ایم بی (MB)' },
        { regex: /^(?:GB\s*([\d\.]+|[\d,]+)|([\d\.]+|[\d,]+)\s*GB)$/i, replace: '$1$2 جی بی (GB)' },
        { regex: /^(?:KB\s*([\d\.]+|[\d,]+)|([\d\.]+|[\d,]+)\s*KB)$/i, replace: '$1$2 کے بی (KB)' },
        { regex: /^(?:file\(s\)\s*(\d+)|(\d+)\s*file\(s\))$/i, replace: '$1$2 فائلیں' },
        { regex: /^(?:items?\s*(\d+)|(\d+)\s*items?)$/i, replace: '$1$2 اشیاء' },
        { regex: /^All Content\s*\(\d+\)$/i, replace: 'تمام مواد' },
        { regex: /^Student ID:?\s*(\d+)$/i, replace: 'طالب علم شناختی نمبر (ID): $1' },
        { regex: /^TL-(\d+)\s*\|\s*P-(\d+)\s*\|\s*PF-(\d+)\s*\|\s*Ab-(\d+)$/i, replace: 'کل: $1 | حاضر: $2 | پی ایف: $3 | غیر حاضر: $4' },
        { regex: /^(\d+)\/(\d+)\s+Topics Consumed$/i, replace: '$1/$2 موضوعات مکمل' },
        { regex: /^(\d+)\/(\d+)\s+topics consumed$/i, replace: '$1/$2 موضوعات مکمل' },
        { regex: /^(\d+)\s+of\s+(\d+)\s+completed$/i, replace: '$1 از $2 مکمل' },
        { regex: /^(\d+)\s+topics? consumed$/i, replace: '$1 موضوعات مکمل' },
        { regex: /^(\d+)%\s+Done$/i, replace: '$1% مکمل' },
        { regex: /^(\d+)\s+active now$/i, replace: '$1 ابھی فعال ہیں' },
        { regex: /^(\d+)\s+Scheduled$/i, replace: '$1 شیڈول شدہ' },
        { regex: /^(\d+)\s+Subjects$/i, replace: '$1 مضامین' },
        { regex: /^(\d+)\s+Units$/i, replace: '$1 یونٹس' },
        { regex: /^(\d+)\s+learning materials available$/i, replace: '$1 تعلیمی مواد دستیاب ہے' },
        { regex: /^Unit\s+(\d+):?\s*(.+)$/i, replace: 'یونٹ $1: $2' },
        { regex: /^Semester\s+(\d+)\s+Subjects$/i, replace: 'سیمسٹر $1 کے مضامین' }
    ];

    const urToEnPatterns = [
        { regex: /^خوش آمدید،\s*([^!]+)!?$/, replace: 'Welcome back, $1!' },
        { regex: /^آخری لاگ ان:\s*(.+)$/, replace: 'Last Login: $1' },
        { regex: /^امتحانات کی جانچ\s+(\d+)\s+زیرِ التوا$/, replace: 'Assessment Evaluations $1 Pending' },
        { regex: /^(\d+)\s+زیرِ التوا$/, replace: '$1 Pending' },
        { regex: /^(\d+)\s+جمع کردہ امتحانات$/, replace: '$1 Submissions' },
        { regex: /^(\d+)\s+جانچ مکمل$/, replace: '$1 Evaluated' },
        { regex: /^(\d+)\s+سیمسٹرز$/, replace: '$1 Semesters' },
        { regex: /^(\d+)\s+سیمسٹر$/, replace: '$1 Sem' },
        { regex: /^کل کریڈٹس\s+(\d+)$/, replace: 'Total Credits $1' },
        { regex: /^(\d+)\s+کریڈٹس$/, replace: '$1 Credits' },
        { regex: /^(\d+)\s+کریڈٹ$/, replace: '$1 Cr' },
        { regex: /^(\d+)\s+ماہ$/, replace: '$1 Mo' },
        { regex: /^(\d+)\s+پروگرامز$/, replace: '$1 Programs' },
        { regex: /^(\d+)\s+کورسز$/, replace: '$1 Courses' },
        { regex: /^(\d+)\s+تعلیمی مواد$/, replace: '$1 Content Items' },
        { regex: /^سیمسٹر\s+(\d+)$/, replace: 'Semester $1' },
        { regex: /^(\d+)\s+نمبرات$/, replace: '$1 Marks' },
        { regex: /^([\d\.]+|[\d,]+)\s*ایم بی \(MB\)$/i, replace: '$1 MB' },
        { regex: /^([\d\.]+|[\d,]+)\s*جی بی \(GB\)$/i, replace: '$1 GB' },
        { regex: /^([\d\.]+|[\d,]+)\s*کے بی \(KB\)$/i, replace: '$1 KB' },
        { regex: /^(\d+)\s*تعلیمی وسائل$/i, replace: '$1 content resource(s)' },
        { regex: /^(\d+)\s*فائلیں$/i, replace: '$1 file(s)' },
        { regex: /^(\d+)\s*اشیاء$/i, replace: '$1 items' },
        { regex: /^طالب علم شناختی نمبر \(ID\):\s*(\d+)$/i, replace: 'Student ID: $1' },
        { regex: /^کل:\s*(\d+)\s*\|\s*حاضر:\s*(\d+)\s*\|\s*پی ایف:\s*(\d+)\s*\|\s*غیر حاضر:\s*(\d+)$/i, replace: 'TL-$1 | P-$2 | PF-$3 | Ab-$4' },
        { regex: /^(\d+)\/(\d+)\s+موضوعات مکمل$/i, replace: '$1/$2 Topics Consumed' },
        { regex: /^(\d+)\s+از\s+(\d+)\s+مکمل$/i, replace: '$1 of $2 completed' },
        { regex: /^(\d+)\s+موضوعات مکمل$/i, replace: '$1 topics consumed' },
        { regex: /^(\d+)%\s+مکمل$/i, replace: '$1% Done' },
        { regex: /^(\d+)\s+ابھی فعال ہیں$/i, replace: '$1 active now' },
        { regex: /^(\d+)\s+شیڈول شدہ$/i, replace: '$1 Scheduled' },
        { regex: /^(\d+)\s+مضامین$/i, replace: '$1 Subjects' },
        { regex: /^(\d+)\s+یونٹس$/i, replace: '$1 Units' },
        { regex: /^(\d+)\s+تعلیمی مواد دستیاب ہے$/i, replace: '$1 learning materials available' },
        { regex: /^یونٹ\s+(\d+):?\s*(.+)$/i, replace: 'Unit $1: $2' }
    ];

    // ── Translation Engine ───────────────────────────────────
    function translateText(text, targetLang) {
        if (!text || typeof text !== 'string') return text;
        const trimmed = text.trim();
        if (!trimmed) return text;

        // Skip pure numbers, emails, URLs
        if (/^[0-9\s.,:%/+\-#]+$/.test(trimmed)) return text;
        if (trimmed.indexOf('@') > 0 && trimmed.indexOf('.') > 0) return text;
        if (/^(https?:\/\/|\/|www\.)/i.test(trimmed)) return text;

        const dict = dictionaries[targetLang];
        if (!dict) return text;

        // TARGET: ENGLISH (Urdu -> English fallback)
        if (targetLang === 'en') {
            if (reverseMap[trimmed]) {
                return text.replace(trimmed, reverseMap[trimmed]);
            }
            const rLower = trimmed.toLowerCase();
            if (reverseLowerMap[rLower]) {
                return text.replace(trimmed, reverseLowerMap[rLower]);
            }
            for (let i = 0; i < urToEnPatterns.length; i++) {
                const pat = urToEnPatterns[i];
                if (pat.regex.test(trimmed)) {
                    return text.replace(trimmed, trimmed.replace(pat.regex, pat.replace));
                }
            }
            return text;
        }

        // TARGET: URDU (English -> Urdu)
        // 1. Direct dictionary match
        if (dict[trimmed]) {
            return text.replace(trimmed, dict[trimmed]);
        }

        // 2. Case-insensitive lookup
        const lower = trimmed.toLowerCase();
        if (lowerMap[lower]) {
            return text.replace(trimmed, lowerMap[lower]);
        }

        // 3. Known dynamic patterns
        for (let i = 0; i < enToUrPatterns.length; i++) {
            const pat = enToUrPatterns[i];
            if (pat.regex.test(trimmed)) {
                return text.replace(trimmed, trimmed.replace(pat.regex, pat.replace));
            }
        }

        // 4. Compound bullet delimiter (e.g. "Programs • Courses • Curriculum Hierarchy")
        if (trimmed.indexOf(' • ') > 0) {
            const parts = trimmed.split(' • ');
            const translatedParts = parts.map(p => translateText(p, 'ur'));
            if (translatedParts.some((p, idx) => p !== parts[idx])) {
                return text.replace(trimmed, translatedParts.join(' • '));
            }
        }

        // 5. Leading number + remainder (e.g. "2 Programs Directory" -> "2 پروگرامز ڈائریکٹری")
        const numMatch = trimmed.match(/^(\d+)\s+(.+)$/);
        if (numMatch) {
            const transRest = translateText(numMatch[2], 'ur');
            if (transRest !== numMatch[2]) {
                return text.replace(trimmed, numMatch[1] + ' ' + transRest);
            }
        }

        // 6. Leading / trailing punctuation stripping (e.g. ".Centralized command center...", "...Search student")
        const puncMatch = trimmed.match(/^([\s\.\,\:\;\!\?\•\-\(\)]*)(.*?)([\s\.\,\:\;\!\?\•\-\(\)]*)$/);
        if (puncMatch && puncMatch[2] && puncMatch[2] !== trimmed) {
            const core = puncMatch[2];
            const transCore = dict[core] || lowerMap[core.toLowerCase()];
            if (transCore) {
                return text.replace(trimmed, puncMatch[1] + transCore + puncMatch[3]);
            }
        }

        // Skip machine IDs / GUIDs only if NOT found in dictionary
        if (/^[A-Z0-9_\-]{12,}$/.test(trimmed)) return text;

        return text;
    }

    // ── Dynamic Status Translation ───────────────────────────
    const statusMap = {
        'PENDING': 'زیرِ التوا',
        'Pending': 'زیرِ التوا',
        'ACTIVE': 'فعال',
        'Active': 'فعال',
        'INACTIVE': 'غیر فعال',
        'Inactive': 'غیر فعال',
        'APPROVED': 'منظور شدہ',
        'Approved': 'منظور شدہ',
        'REJECTED': 'مسترد شدہ',
        'Rejected': 'مسترد شدہ',
        'PUBLISHED': 'شائع شدہ',
        'Published': 'شائع شدہ',
        'DRAFT': 'مسودہ',
        'Draft': 'مسودہ',
        'COMPLETED': 'مکمل',
        'Completed': 'مکمل',
        'PRESENT': 'حاضر',
        'Present': 'حاضر',
        'ABSENT': 'غیر حاضر',
        'Absent': 'غیر حاضر',
        'PASS': 'کامیاب',
        'Pass': 'کامیاب',
        'PASSED': 'کامیاب',
        'Passed': 'کامیاب',
        'FAIL': 'ناکام',
        'Fail': 'ناکام',
        'FAILED': 'ناکام',
        'Failed': 'ناکام',
        'UNDER REVIEW': 'زیرِ جائزہ',
        'Under Review': 'زیرِ جائزہ'
    };

    function formatStatus(status) {
        if (currentLanguage === 'ur' && statusMap[status]) {
            return statusMap[status];
        }
        return status;
    }

    // ── Safe DOM Translation Walker ──────────────────────────
    function translateNode(node, lang) {
        if (!node) return;
        if (node.nodeType === Node.TEXT_NODE) {
            const original = node._origText !== undefined ? node._origText : node.nodeValue;
            if (node._origText === undefined) {
                node._origText = original;
            }

            if (lang === 'en') {
                if (node._origText !== undefined) {
                    node.nodeValue = node._origText;
                } else {
                    node.nodeValue = translateText(node.nodeValue, 'en');
                }
            } else {
                const translated = translateText(original, 'ur');
                if (translated !== original) {
                    node.nodeValue = translated;
                }
            }
            return;
        }

        if (node.nodeType === Node.ELEMENT_NODE) {
            // Safeguards: DO NOT touch scripts, styles, code blocks, or protected inputs
            const tagName = node.tagName.toLowerCase();
            if (tagName === 'script' || tagName === 'style' || tagName === 'noscript' ||
                tagName === 'code' || tagName === 'pre' || tagName === 'svg') {
                return;
            }

            // Elements explicitly marked as no-translate or user data fields
            if (node.classList && (node.classList.contains('no-translate') ||
                node.classList.contains('p-name') ||
                node.classList.contains('user-data-field') ||
                node.classList.contains('notranslate'))) {
                return;
            }

            // Translate placeholders
            if (node.hasAttribute('placeholder')) {
                if (node._origPlaceholder === undefined) {
                    node._origPlaceholder = node.getAttribute('placeholder');
                }
                if (lang === 'en') {
                    if (node._origPlaceholder !== undefined) {
                        node.setAttribute('placeholder', node._origPlaceholder);
                    } else {
                        node.setAttribute('placeholder', translateText(node.getAttribute('placeholder'), 'en'));
                    }
                } else {
                    node.setAttribute('placeholder', translateText(node._origPlaceholder, 'ur'));
                }
            }

            // Translate title attributes
            if (node.hasAttribute('title')) {
                if (node._origTitle === undefined) {
                    node._origTitle = node.getAttribute('title');
                }
                if (lang === 'en') {
                    if (node._origTitle !== undefined) {
                        node.setAttribute('title', node._origTitle);
                    } else {
                        node.setAttribute('title', translateText(node.getAttribute('title'), 'en'));
                    }
                } else {
                    node.setAttribute('title', translateText(node._origTitle, 'ur'));
                }
            }

            // Translate aria-label attributes
            if (node.hasAttribute('aria-label')) {
                if (node._origAriaLabel === undefined) {
                    node._origAriaLabel = node.getAttribute('aria-label');
                }
                if (lang === 'en') {
                    if (node._origAriaLabel !== undefined) {
                        node.setAttribute('aria-label', node._origAriaLabel);
                    } else {
                        node.setAttribute('aria-label', translateText(node.getAttribute('aria-label'), 'en'));
                    }
                } else {
                    node.setAttribute('aria-label', translateText(node._origAriaLabel, 'ur'));
                }
            }

            // Translate input buttons (submit/button/reset) but NEVER touch text input values!
            if (tagName === 'input') {
                const inputType = (node.getAttribute('type') || '').toLowerCase();
                if (inputType === 'button' || inputType === 'submit' || inputType === 'reset') {
                    if (node._origValue === undefined) {
                        node._origValue = node.value;
                    }
                    if (lang === 'en') {
                        if (node._origValue !== undefined) {
                            node.value = node._origValue;
                        } else {
                            node.value = translateText(node.value, 'en');
                        }
                    } else {
                        node.value = translateText(node._origValue, 'ur');
                    }
                }
                return;
            }

            // Translate <option> text inside <select>, but NEVER change value attribute!
            if (tagName === 'option') {
                if (node._origText === undefined) {
                    node._origText = node.textContent;
                }
                if (lang === 'en') {
                    if (node._origText !== undefined) {
                        node.textContent = node._origText;
                    } else {
                        node.textContent = translateText(node.textContent, 'en');
                    }
                } else {
                    node.textContent = translateText(node._origText, 'ur');
                }
                return;
            }

            // Traverse child nodes
            let child = node.firstChild;
            while (child) {
                const next = child.nextSibling;
                translateNode(child, lang);
                child = next;
            }
        }
    }

    function translatePage(root) {
        const container = root || document.body;
        if (!container) return;
        translateNode(container, currentLanguage);
    }

    // ── Update Layout and Direction ──────────────────────────
    function applyLayoutDirection(lang) {
        const isUr = (lang === 'ur');
        const dir = isUr ? 'rtl' : 'ltr';

        document.documentElement.lang = lang;
        document.documentElement.dir = dir;
        document.documentElement.setAttribute('data-textdirection', dir);

        if (isUr) {
            document.documentElement.classList.add('lms-rtl');
            document.body.classList.add('lms-rtl');
        } else {
            document.documentElement.classList.remove('lms-rtl');
            document.body.classList.remove('lms-rtl');
        }

        // Notify sidebar fix script if present
        if (window.dispatchEvent) {
            window.dispatchEvent(new Event('resize'));
        }
    }

    // ── Update Switcher Topbar UI ────────────────────────────
    function updateSwitcherUI(lang) {
        const label = document.getElementById('lmsCurrentLangLabel');
        if (label) {
            label.textContent = (lang === 'ur') ? 'اردو' : 'English';
        }

        const pill = document.getElementById('lmsLangPill');
        if (pill) {
            pill.setAttribute('aria-label', (lang === 'ur') ? 'موجودہ زبان: اردو' : 'Current Language: English');
        }

        document.querySelectorAll('.lms-lang-option').forEach(function (opt) {
            const optLang = opt.getAttribute('data-lang');
            const check = opt.querySelector('.lms-lang-check');
            if (optLang === lang) {
                opt.classList.add('active');
                if (check) check.style.display = 'inline-block';
            } else {
                opt.classList.remove('active');
                if (check) check.style.display = 'none';
            }
        });
    }

    // ── Public API ───────────────────────────────────────────
    function setLanguage(lang) {
        if (lang !== 'en' && lang !== 'ur') return;
        currentLanguage = lang;

        // 1. Persistence
        const userKey = getUserStorageKey();
        try {
            localStorage.setItem(userKey, lang);
            localStorage.setItem('lms_lang', lang);
        } catch (e) {}

        const uid = getUserId();
        if (uid) setCookie('lms_lang_' + uid, lang, 365);
        setCookie('lms_lang', lang, 365);

        // 2. Layout direction & styles
        applyLayoutDirection(lang);

        // 3. Switcher UI
        updateSwitcherUI(lang);

        // 4. Translate DOM
        translatePage(document.body);

        // 5. Broadcast Event
        try {
            window.dispatchEvent(new CustomEvent('lms-language-changed', {
                detail: { language: lang, isUrdu: (lang === 'ur') }
            }));
        } catch (e) {}
    }

    function getLanguage() {
        return currentLanguage;
    }

    function t(key, fallback) {
        if (!key || typeof key !== 'string') return fallback || key;
        const res = translateText(key, currentLanguage);
        if (res && res !== key) return res;
        const dict = dictionaries[currentLanguage];
        if (dict && dict[key]) return dict[key];
        return fallback || key;
    }

    // ── Dynamic Content Observer ─────────────────────────────
    let mutationTimer = null;
    const observer = new MutationObserver(function (mutations) {
        if (currentLanguage !== 'ur') return;
        clearTimeout(mutationTimer);
        mutationTimer = setTimeout(function () {
            for (let i = 0; i < mutations.length; i++) {
                const mutation = mutations[i];
                if (mutation.type === 'childList') {
                    for (let j = 0; j < mutation.addedNodes.length; j++) {
                        const node = mutation.addedNodes[j];
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            // Skip tooltips or switcher dropdown itself
                            if (node.classList && (node.classList.contains('lms-instant-tooltip') ||
                                node.classList.contains('lms-lang-menu'))) {
                                continue;
                            }
                            translateNode(node, 'ur');
                        }
                    }
                }
            }
        }, 60);
    });

    // ── Initialize on DOM Ready ──────────────────────────────
    function init() {
        applyLayoutDirection(currentLanguage);
        updateSwitcherUI(currentLanguage);

        if (currentLanguage === 'ur') {
            translatePage(document.body);
        }

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });

        // Setup Topbar Language Switcher interactions
        const pill = document.getElementById('lmsLangPill');
        const menu = document.getElementById('lmsLangDropdown');

        if (pill && menu) {
            function toggleMenu(open) {
                const isOpen = open !== undefined ? open : !menu.classList.contains('show');
                if (isOpen) {
                    menu.classList.add('show');
                    pill.setAttribute('aria-expanded', 'true');
                } else {
                    menu.classList.remove('show');
                    pill.setAttribute('aria-expanded', 'false');
                }
            }

            pill.addEventListener('click', function (e) {
                e.stopPropagation();
                toggleMenu();
            });

            pill.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    toggleMenu();
                } else if (e.key === 'Escape') {
                    toggleMenu(false);
                }
            });

            document.addEventListener('click', function (e) {
                if (!pill.contains(e.target) && !menu.contains(e.target)) {
                    toggleMenu(false);
                }
            });

            document.querySelectorAll('.lms-lang-option').forEach(function (btn) {
                btn.addEventListener('click', function (e) {
                    e.preventDefault();
                    const chosen = this.getAttribute('data-lang');
                    setLanguage(chosen);
                    toggleMenu(false);
                });
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose global API
    window.i18n = {
        setLanguage: setLanguage,
        getLanguage: getLanguage,
        t: t,
        translateText: translateText,
        translatePage: translatePage,
        formatStatus: formatStatus,
        dictionaries: dictionaries
    };

})(window, document);
