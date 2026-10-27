# EduDigi Classes LMS - Verified Project Context

Last source audit: 2026-10-19

This handoff covers the application under `TMS/`. “Implemented” means present in audited source, not proven in deployment.

## Repository and architecture

- Workspace: `E:/LMS`; solution: `TMS/TMS.sln`; web project: `TMS/TMS.Web`.
- `global.json` pins SDK `10.0.201` with `latestPatch`; the five solution projects target `net10.0`; local `dotnet-ef` is `10.0.12`.
- The working tree was already heavily modified and contained untracked IA/grade-card work. `BRAIN.md` was already untracked. Preserve all existing user changes.
- Conventional ASP.NET Core MVC/Razor, cookie authentication, role/claim checks, session, EF Core and SQL Server.
- `TMS.Web/Program.cs` owns DI, middleware and routes. `TMS.Repository/ServiceCollectionExtension.cs` registers repositories/managers; newer IA controllers use `ApplicationDBContext` directly.
- Projects: `TMS.Common/`, `TMS.Models/`, `TMS.ViewModels/`, `TMS.Repository/`, `TMS.Web/`. `TMS.IA.RuntimeChecks/` is an executable check project but is not in `TMS.sln`.
- Main EF/config files: `TMS.Repository/ApplicationDBContext.cs`, `TMS.Repository/DatabaseInitializer.cs`, `TMS.Web/Infrastructure/DatabaseInitializationGuard.cs`, `TMS.Web/appsettings.json`. Never copy committed secrets into documentation/logs; externalize and rotate them before production.
- No standalone `.sql` files exist under `TMS/`; schema history is in `TMS.Repository/Migrations/`, while `DatabaseInitializer.cs` also performs direct bootstrap SQL/data changes.
- Friendly routes include `/MyCourses`, `/SubjectHub/{courseId}`, `/Subject/{courseId}/Unit/{unitId}`, and quadrant content. Other modules use conventional or Admin-area routes.

## Implemented modules and actual status

| Module | Important source | Status |
|---|---|---|
| Auth/users/roles | `TMS.Web/Controllers/AuthController.cs`, `AccountController.cs`, `Areas/Admin/Controllers/UserController.cs`, `RoleController.cs`; `TMS.Models/Account/` | Cookie login, users, roles/permissions and password-change fields exist. |
| Academic structure | `Areas/Admin/Controllers/CourseStructureController.cs`, `CourseController.cs`, `FacultyMappingController.cs`, `CourseEnrollmentController.cs` | Programs/categories, semesters, subjects, courses, credits, faculty mapping and enrollment lifecycle exist. |
| Learning/content | `StudentCourseController.cs`, `StudentUnitController.cs`, `StudentQuadrantController.cs`, `StudentCourseContentController.cs`, `Services/StudentLearningService.cs` | Course/subject/unit/quadrant hubs, materials and progress exist. |
| Classes/attendance | `CalendarController.cs`, `VirtualClassroomController.cs`, `AttendanceController.cs`, `BackgroundServices/AttendanceBackgroundJob.cs` | Scheduling, meetings and attendance paths exist; integrations were not exercised. |
| Assignments/quizzes | `AssignmentController.cs`, `StudentQuizController.cs`, `FacultyQuizAssessmentController.cs`, `Areas/Admin/Controllers/QuizController.cs`, `Areas/Admin/Views/Quiz/Index.cshtml` | Creation, submission/attempt, evaluation, plus full AI Question Bank (Gemini suggested questions), duplicate detection, Question Paper generator, and print preview. |
| Formal examinations | `FacultyExamController.cs`, `StudentExamController.cs`, `Areas/Admin/Controllers/AdminExamController.cs`; `TMS.Models/Assessments/` | Exam, section, question bank, assignment, attempt, evaluation and `ExamResult` flows exist. They are separate from IA grade cards. |
| Internal Assessment | `InternalAssessmentController.cs`, `Views/InternalAssessment/`, `wwwroot/js/internal-assessment.js`, `Services/AutomatedResultService.cs` | Scheduling, types, creation/edit, questions, submissions, evaluation, automated UGC 10-point result calculation, grace marks engine, and compartmental re-assessments. |
| Internal marks entry | `InternalMarksEntryController.cs`, `Views/InternalMarksEntry/Index.cshtml` | Updates existing non-question submissions only; it cannot create missing/offline marks rows. |
| Internal grade cards | `InternalGradeCardController.cs`, `ViewModels/InternalGradeCardViewModel.cs`, `Views/InternalGradeCard/` | Interactive dashboard and printable course/semester previews with UGC 10-point grades (`O`-`F`), grade points (`10`-`0`), and compartmental attempt indicators linked to `ProcessedAssessmentResult`. |
| PPT/video | `InteractivePPTController.cs`, `FacultyPPTController.cs`, `StudentPPTController.cs`, `VideoLearningController.cs` | Interactive PPT, video/lecture progress and embedded quizzes exist. |
| Communications/reports | `AnnouncementController.cs`, `DiscussionController.cs`, `ChatController.cs`, `NotificationsController.cs`, `ProgressReportController.cs` | Communication and progress/monitoring views exist. No registrar transcript subsystem exists. |

## Layouts, roles and navigation

- `TMS.Web/Views/Shared/_Layout.cshtml` uses `_StudentMenu.cshtml` only for a Student role claim; other users receive `_MenuPartial.cshtml`.
- New controllers recognize Admin/Super Admin/Management and Faculty/Exam Coordinator plus `BaseController` faculty aliases. Authorization remains controller-specific.
- Grade-card links are in `_StudentMenu.cshtml`, two staff sections of `_MenuPartial.cshtml`, and `wwwroot/js/global-search.js`.
- Confirmed defect: `_MenuPartial.cshtml` renders an admin-style menu when the role claim is missing/empty. Endpoint checks still apply.
- Shared assets include `wwwroot/css/enhanced-sidebar.css`, `lms-sidebar-fix.css`, `global-search.css`, `wwwroot/js/lms-sidebar-fix.js`, and `global-search.js`.
- Branding remains inconsistent (`Edudigi Class`, `Edudigi Classes`, `EduDigiClass`). Grade-card print views hard-code “EduDigi Classes”.

## Internal Assessment

### Source map

- Entities: `TMS.Models/Academics/InternalAssessment.cs`, `InternalAssessmentPolicy.cs`, `InternalAssessmentQuestion.cs`
- DbSets/model: `TMS.Repository/ApplicationDBContext.cs`
- Views: `TMS.Web/Views/InternalAssessment/AdminDashboard.cshtml`, `FacultyDashboard.cshtml`, `StudentDashboard.cshtml`, `Questions.cshtml`, `Attempt.cshtml`, `Submissions.cshtml`, `GradeQuestions.cshtml`, `_StatusModal.cshtml`
- Rules/validation: `TMS.Web/Services/InternalAssessmentResultPolicy.cs`, `InternalAssessmentUploadValidator.cs`
- Uploads: runtime `TMS.Web/InternalAssessmentUploads/`, outside `wwwroot`, downloaded through an authorized action.

### Scheduling, semesters, courses and assessment types

- Admin creates a semester policy, then a course-specific window. Server checks semester equality, dates, maximum marks and late-submission permission.
- `AdminDashboard.cshtml` lists Semesters 1–8 and disables those without a configured course. Entities accept any positive semester ID and faculty filters derive values from courses, so 1–8 is a UI convention, not a DB constraint.
- `InternalAssessmentWindow.CourseId` remains nullable for legacy semester-wide windows; new windows require a course.
- Faculty sees actively mapped courses; Admin sees active courses. Optional subjects must match the course semester.
- Types are Assignment, Written, Practical, Project, Presentation, Viva and MCQ. Multiple values are comma-separated in `InternalAssessment.Type`.
- Type picker/display, course-subject-window filtering and schedule-end helper exist in Razor/JS but were not browser-tested.
- No compartmental/supplementary/backlog exam workflow, entity flag, route or grade-card rule was found. “Supplementary” elsewhere is only a content-quadrant label.

### Current workflow

1. Admin manages policies and course windows; rescheduling can optionally align linked draft deadlines.
2. Authorized faculty/admin creates a draft. Metadata is editable only while unpublished and without submissions.
3. Question-based and no-question text/file submissions are supported. Publishing requires an active current window; question totals must equal assessment maximum.
4. Students need active enrollment (`IsActive && Status == 1`) to attempt/submit. One submission is protected by checks, a serializable transaction and a unique model index.
5. Uploads allow PDF, DOCX, PPTX, JPG/JPEG and PNG up to 8 MB. Binary signatures/trailers or OOXML parts are checked; renamed/generic files are rejected.
6. MCQs auto-mark; faculty grades written answers or whole submissions. Marks are bounded.
7. `CreatedBy` remains the owner. If that faculty member loses the active mapping, the newest active mapped faculty becomes successor; Admin can manage all.

### Publish/unpublish: actual versus intended

Actual source is still legacy approval:

- `PublishResults` is Admin-only and requires `ResultApproved`.
- Faculty uses `RequestResultApproval`; Admin uses `ApproveResults`; `Submissions.cshtml`, `FacultyDashboard.cshtml` and `InternalMarksEntry/Index.cshtml` still describe Admin review.
- Evaluation/marks entry lock on requested, approved or published state.
- Assessment Publish/Unpublish and approved-result Publish use `.ia-confirm-form`; `internal-assessment.js` opens `#iaConfirmModal` with native `confirm` fallback. Unpublish warns that results will be hidden. Wiring exists but was not browser-tested.

Approved next-state decision:

- Owning/successor faculty publishes directly after readiness validation; routine Admin approval is removed. Admin keeps monitoring and exceptional hide/unpublish authority.
- Preserve `RequireResultApproval`, `ResultApprovalRequested`, `ResultApproved`, `ResultApprovedBy`, `ResultApprovedOn` and all existing values as legacy history. Do not drop, rewrite or clear historical approvals.
- Controller authorization/locking, views, wording and warnings must change together. **Faculty-direct publication is not implemented as of this audit.**

### Result behavior in code

- `ValidateResultsReady` requires published assessment, schedule, elapsed due date/window, and valid evaluation of every received submission; question totals must be complete/consistent.
- It does not require a submission from every enrolled student. After publication, a non-submitter is dynamically ABSENT, 0 and FAIL without a synthetic submission row.
- `InternalAssessmentResultPolicy` hard-codes 33% per assessment; exactly 33% passes. Any failed/absent assessment fails the aggregate; incomplete evaluation is PENDING.
- This rule is implemented/tested but no institutional approval/configuration was found. Do not infer external-exam, weighting, grade-band, SGPA/CGPA, grace, repeat or compartment rules.

## Internal marks entry

- Route `/InternalMarksEntry/Index`; POST save has antiforgery and active mapping/enrollment checks.
- Filters semester/course/assessment and updates only an existing unique non-question submission. It is an evaluation shortcut, not an offline marks register.
- Confirmed stale UI: it says evaluation is followed by Admin approval. It also duplicates 33% calculation rather than using configurable policy.

## Internal grade cards

- Routes: `/InternalGradeCard/Index`, `/InternalGradeCard/Preview?studentId=&courseId=`, `/InternalGradeCard/SemesterPreview?studentId=&semesterId=`.
- Files: `TMS.Web/Controllers/InternalGradeCardController.cs`, `TMS.Web/ViewModels/InternalGradeCardViewModel.cs`, `TMS.Web/Views/InternalGradeCard/Index.cshtml`, `Preview.cshtml`, `SemesterPreview.cshtml`.
- Preview CSS specifies A4 portrait (`210mm × 297mm`, `@page size: A4 portrait`) and uses `window.print()` for browser print/save-PDF.
- Data is derived live from published IA results plus users/enrollments/courses/semesters/subjects. There is no snapshot, issue/version, server PDF, document number, signature, QR, external marks, grade bands, credits-earned, SGPA or CGPA.
- It shows name and numeric `UserMaster.Id`. `UserMaster` has no roll/enrollment number, program/cohort, academic year, semester or institution FK.
- `CourseMaster` has course code, optional semester and `NoofCredit`; category can represent a program, but grade-card queries do not surface program/credits.
- Confirmed risk: semester rows group by `SubjectId`; null-subject assessments from different courses can collapse into one “Subject unassigned” row.
- Grade-card queries generally use enrollment `Status == 1` without consistently requiring `IsActive`, unlike attempt/submission authorization.
- Values are mutable live queries, so this is an internal statement—not an official final grade card.

## Data model and migrations

Core identity/mapping:

- `UserMaster`: name, email, password, optional role/contact and password-change audit; missing official student identifiers listed above.
- `CourseEnrollment`: Course + Student, `IsActive`, Status 1 active/2 pending/3 dropped/4 completed/5 waitlisted, lifecycle dates/source; unique `(CourseId, StudentId)`.
- `CourseMaster`: category/program, code, duration, optional semester, credits. `SubjectMaster` belongs to Semester; `CourseFacultyMapMaster` maps course/faculty.

IA Fluent relationships in `ApplicationDBContext.cs`:

- Assessment -> Course, optional Subject/Window, creator and optional approver.
- Submission -> Assessment, Student, optional evaluator; unique `(AssessmentId, StudentId)`.
- Policy -> Semester; Window -> Policy and optional Course; Question -> Assessment; Answer -> Submission and Question with unique `(SubmissionId, QuestionId)`.
- IA FKs use `DeleteBehavior.NoAction`.

Formal exam entities (`ExamMaster`, sections, question bank/options/maps, assignments, attempts/answers, evaluations, `ExamResult`) remain separate. `ExamResult` stores marks/percentage/grade/status/published time, but no bridge to internal grade cards or external-marks/semester-transcript table was found.

Migration hazards:

- Both `20261009084856_DiagnoseInternalAssessmentModel.cs` and `20261009130000_AddInternalAssessment.cs` create the two base IA tables and can collide.
- The latter has no designer and lacks policies, windows, questions, answers, approval/window fields and current FKs/indexes.
- `ApplicationDBContextModelSnapshot.cs` is not aligned with the current six-table IA model. Entity `Type` allows 100 characters; the migrations use 30.
- No grade-card table/migration exists because cards are query-derived.
- Live schema and `__EFMigrationsHistory` remain unverified. Back up, clone, inventory and create one reviewed additive reconciliation migration; never apply current IA migrations blindly.

Startup safety changed: `Program.cs` no longer initializes unconditionally. `DatabaseInitializationGuard` requires explicit enablement, non-Production, disposable confirmation, and exact expected/actual DB name; initializer API also rejects unconfirmed calls. If enabled, `DatabaseInitializer.cs` still makes broad catalog/demo/user/mapping/enrollment changes, so never enable it for production/irreplaceable data.

## Documents and verification

- `DocumentViewerController.cs` streams PDFs and converts Office files using LibreOffice; PPT helpers also use PyMuPDF-related processing. `Services/ViewRenderService.cs` renders Razor to HTML.
- These are reusable preview capabilities, not an official grade-card PDF pipeline. No active QR verification route, document registry, signature service or verified server PDF renderer was found.
- Login uses `wwwroot/images/logo.jpg`; layout branding/logo claims are not consistently used by sidebars/grade cards.

## Recent changes visible on 2026-10-12

- Internal grade-card controller/view models/dashboard/course and semester previews, menus and global search.
- Internal marks-entry page.
- Shared 33% result helper and ABSENT/PENDING/PASS/FAIL aggregation.
- Signature-aware upload validation and safer temp-file/transaction handling.
- Successor-faculty management while retaining original creator.
- Explicit startup initializer guard.
- `TMS.IA.RuntimeChecks` for result rules, initializer safety and uploads.
- Metadata edit, course schedules/rescheduling, semesters 1–8 UI, multi-type picker and confirmation-modal wiring.

## Pending bugs / known issues

Confirmed from source:

1. Publication still requires Faculty request -> Admin approval -> Admin publish, contrary to the approved direction.
2. Several unpublish/evaluation/marks paths clear approval fields, risking loss of historical approval state.
3. IA migrations/snapshot mismatch and duplicate table creation block safe deployment.
4. Marks entry cannot record offline/missing submissions; help/approval wording is misleading.
5. [Partially Resolved 2026-10-19] Grade cards now calculate and render institutional UGC 10-point scale letter grades (`O`, `A+`, `A`, `B+`, `B`, `C`, `P`, `F`) and grade points (`10.0` to `0.0`) backed by `InstitutionalGradingPolicy` and `ProcessedAssessmentResult`. (Formal registrar snapshot/PDF verification remains pending).
6. Null-subject semester grouping and inconsistent enrollment `IsActive` filtering can produce incorrect visibility/aggregation.
7. [Resolved 2026-10-19] Compartmental examination model implemented: `InternalAssessment.IsCompartmental`, `AttemptNumber`, `OriginalAssessmentId`, and automated re-assessment creation (`CreateCompartmentalAssessmentAsync`).
8. Official student/institution identity fields are missing.
9. UX mixes modals with raw `BadRequest`; `_StatusModal.cshtml` contains a mis-encoded checkmark literal.
10. Missing-role admin-style menu and inconsistent branding remain.
11. `TMS.Web/Views/InternalGradeCard/Index.cshtml.bak-20261012-160721` is an untracked backup artifact, not source of truth.

Unverified/integration questions:

- Type dropdown visibility, linked filters, confirmation popups and A4 layout need browser testing.
- Live schema, migration order, legacy approval data and real mappings are unknown.
- Authenticated role E2E, concurrency, filesystem permissions, LibreOffice and reverse proxy were not exercised.
- Formal exam runtime/publication semantics were not validated end-to-end.

## Decisions required; do not invent

- Institutional approval/configuration of 33%, rounding, grades, weighting, minimum components, grace and incomplete rules.
- External marks source/import, internal/external weighting, earned credits and SGPA/CGPA formula.
- Compartment/repeat/improvement identity and replacement/best/latest rules.
- Faculty publication scope/readiness roster, retract/correction authority, audit log and notifications.
- Immutable handling of legacy approval history.
- Authoritative roll/enrollment number, program/cohort, academic year, semester, institution/logo/signatory.
- Official document number, snapshot/version/hash, QR verification payload/lifetime, revocation/regeneration.

## Build/test status and safe operation

On 2026-10-12 a disposable repository copy was built, excluding build/publish/upload outputs:

- `dotnet build TMS.sln`: **passed**, 0 errors, 232 warnings, ~34 seconds.
- Warnings include nullable/member-hiding/analyzer issues and `NU1900` because NuGet vulnerability metadata was unreachable.
- `dotnet run --project TMS.IA.RuntimeChecks/TMS.IA.RuntimeChecks.csproj`: **24/24 passed**, exit 0.
- Checks cover 33% boundaries and aggregation, initializer guard, PDF/DOCX validation and renamed/generic/oversize rejection.
- No site, browser, SQL connection, migration or seed ran.

```powershell
cd E:\LMS\TMS
dotnet tool restore
dotnet restore .\TMS.sln
dotnet build .\TMS.sln
dotnet run --project .\TMS.IA.RuntimeChecks\TMS.IA.RuntimeChecks.csproj
dotnet ef migrations list --project .\TMS.Repository\TMS.Repository.csproj --startup-project .\TMS.Web\TMS.Web.csproj
```

Do not run `database update`, add/remove migrations, enable `DatabaseInitialization`, or start against production merely to test UI. Back up and restore-test, externalize secrets, secure/scan durable upload storage, and run authenticated role smoke/print tests on a disposable DB before release.

## Gemini AI-Powered Internal Assessment Evaluation (Added 2026-10-15)

### Architecture & Components
- **Service Interface & Implementation**:
  - `TMS.Web/Services/IAiEvaluationService.cs`: Interface defining `EvaluateQuestionAsync(AiEvaluationRequest, CancellationToken)`.
  - `TMS.Web/Services/GeminiAiEvaluationService.cs`: Implementation calling Google Gemini REST API (`generateContent`) using `HttpClient` with typed DI registration in `Program.cs`.
- **DTOs / Models**:
  - `TMS.Models/Academics/AiEvaluationModels.cs`: `AiEvaluationRequest`, `AiEvaluationResult`, `GeminiEvaluationPayload`.
- **Database Schema**:
  - `InternalAssessmentQuestions`: Added `ExpectedAnswer` (NVARCHAR(MAX)), `Rubric` (NVARCHAR(MAX)), `RubricVersion` (INT NOT NULL DEFAULT 1).
  - `InternalAssessmentQuestionAnswers`: Added `AiConceptualMatchPercentage` (DECIMAL(5,2)), `AiSuggestedMarks` (DECIMAL(10,2)), `AiThresholdMet` (BIT), `AiFeedback` (NVARCHAR(1000)), `AiStrengths` (NVARCHAR(MAX)), `AiMissingConcepts` (NVARCHAR(MAX)), `AiConfidence` (NVARCHAR(50)), `AiModel` (NVARCHAR(100)), `AiEvaluatedOn` (DATETIME2), `AiAccepted` (BIT NOT NULL DEFAULT 0), `AiRubricVersion` (INT), `AiEvaluationMetadata` (NVARCHAR(MAX)), and `FilePath` (NVARCHAR(500) NULL).
  - `InternalAssessmentSubmissions`: `FilePath` (NVARCHAR(500)) for overall assessment document uploads.
  - Schema was applied idempotently via safe non-destructive SQL (`IF NOT EXISTS ADD COLUMN...`). No existing tables or data were dropped or reset.
- **Controller Endpoints (`InternalAssessmentController.cs`)**:
  - `EvaluateQuestionAi(int submissionId, int questionId)` [HttpPost, ValidateAntiForgeryToken, Staff Only]
  - `EvaluateAllQuestionsAi(int submissionId)` [HttpPost, ValidateAntiForgeryToken, Staff Only]
  - `SaveQuestionRubric(int submissionId, int questionId, string? expectedAnswer, string? rubric)` [HttpPost, ValidateAntiForgeryToken, Staff Only]
  - `AcceptAiMarks(int submissionId, int questionId)` [HttpPost, ValidateAntiForgeryToken, Staff Only, Locked check]
  - `AcceptAllAiMarks(int submissionId)` [HttpPost, ValidateAntiForgeryToken, Staff Only, Locked check]
  - `SubmitAnswers(int id, Dictionary<int, string>? answers)` [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(32MB), Student Only]: Supports `multipart/form-data` document uploads per question for Project/Presentation and general attachments.
  - `DownloadAnswerFile(int answerId)` [HttpGet, Student & Staff authorized]: Secure download of student-uploaded project/presentation documents.
- **Prompt Injection Defense & Security**:
  - Student submissions are enclosed in strict `<student_submission>` tags.
  - A strict security directive informs Gemini to disregard commands or score demands within student text.
  - Evaluation focuses purely on conceptual merit, completeness, correctness, and rubric weighting.
- **Threshold Rules**:
  - Conceptual match benchmark: 50%.
  - `Match >= 50%`: `ThresholdMet = true`. Suggested marks are calculated based on rubric depth, NOT automatically 50%.
  - `Match < 50%`: `ThresholdMet = false`. Flagged for mandatory faculty review; partial credit preserved.
- **UI & Workflow Refinements (Unbranded Conceptual Evaluation)**:
  - **Question Creation / Edit Time Feeding (`Questions.cshtml`)**: Faculty can input Expected Answer / Key Concepts and Marking Rubric directly when creating or editing questions. `SaveQuestion` persists both fields so they are ready before evaluation.
  - **Zero "AI" / "Gemini" Branding**: The words "Gemini", "AI", and model names are completely hidden from the user interface. Clean, professional academic terminology is used throughout ("Conceptual Understanding Evaluation", "Evaluate Understanding", "Apply Evaluated Marks").
  - **Streamlined Evaluation Display (`GradeQuestions.cshtml`)**: The separate suggested marks box has been removed in favor of a clean Conceptual Understanding Match indicator (green for >=50%, red for <50%), Demonstrated Strengths, Missing or Incomplete Concepts, and Constructive Feedback, allowing faculty to focus on conceptual assessment and award marks directly or via "Apply Evaluated Marks".
- **Natural Academic Scoring & Maximum Mark Prevention**:
  - In subjective/written university evaluations, descriptive answers are never awarded 100% full marks.
  - Evaluation prompt guides Gemini to award natural academic marks (80%–88% for exceptional answers, 68%–78% for good, 50%–65% for average) using realistic increments (.00, .25, .50, .75).
  - Server-side deterministic guard in `GeminiAiEvaluationService.ParseAndValidateResponse` enforces `suggestedMarks < MaxMarks` with a natural ceiling (87.5% for >5 marks, 85% for 2-5 marks) and quarter-mark rounding so marks look authentic and human.
  - Rubric status badge in `GradeQuestions.cshtml` and `Questions.cshtml` fixed to display clean `Rubric Configured` without literal `(v@q.RubricVersion)` string syntax.
- **Dynamic MCQ Visibility in Question Management**:
  - In [Questions.cshtml](file:///E:/LMS/TMS/TMS.Web/Views/InternalAssessment/Questions.cshtml), MCQ-specific inputs (Option A, B, C, D and Correct Option select) only display when the selected question type is `"MCQ"`. For all other types (`Written`, `Project`, `Presentation`, `Practical`, `Viva`), MCQ inputs are cleanly hidden, keeping the interface uncluttered.
- **Document Upload for Project & Presentation Submissions**:
  - In [Attempt.cshtml](file:///E:/LMS/TMS/TMS.Web/Views/InternalAssessment/Attempt.cshtml), students attempting `Project` or `Presentation` questions receive a dedicated document upload field (`.pdf`, `.docx`, `.pptx`, `.jpg`, `.jpeg`, `.png`, max 8MB) with optional summary/notes textarea.
  - In [InternalAssessmentController.cs](file:///E:/LMS/TMS/TMS.Web/Controllers/InternalAssessmentController.cs), `SubmitAnswers` supports `multipart/form-data` uploads, validates binary signatures with `InternalAssessmentUploadValidator`, persists `FilePath` on `InternalAssessmentQuestionAnswers` and `InternalAssessmentSubmissions`, and provides secure `DownloadAnswerFile`.
  - In [GradeQuestions.cshtml](file:///E:/LMS/TMS/TMS.Web/Views/InternalAssessment/GradeQuestions.cshtml), faculty can directly download and review the student's submitted project/presentation document when evaluating answers.
- **Verification Status**:
  - `dotnet build TMS.sln`: 0 errors.
  - `TMS.IA.RuntimeChecks`: **65/65 passed (100% success)**.
  - `TMS.Web`: Running live on `http://localhost:5022`.

## AI Question Bank, Paper Generator & Automated UGC Grading Engine (Added 2026-10-19)

### Architecture & Components
- **Services & Dependency Injection (`TMS.Web/Program.cs`)**:
  - `ISuggestedQuestionService` & `GeminiSuggestedQuestionService`: Google Gemini REST integration for curriculum-aligned question generation, option generation, and real-time single-question regeneration.
  - `IAutomatedResultService` & `AutomatedResultService`: Automated evaluation processing engine supporting UGC 10-point grading, grace marks distribution, and compartmental exam generation.
- **Entities & Schema (`TMS.Models/` & `TMS.Repository/ApplicationDBContext.cs`)**:
  - `QuestionBank`: Extended with `SemesterId`, `SubjectId`, `UnitId`, `TopicId`, `LearningOutcome`, `NormalizedText`, `NegativeMarks`, `CorrectAnswer`, and `Status`.
  - `QuestionPaper`: Formal exam paper entity storing title, code, course, semester, subject, faculty, total marks, pass marks, duration minutes, instructions, and status (`Draft`, `Published`).
  - `QuestionPaperQuestion`: Junction entity mapping `QuestionPaper` to `QuestionBank` with `SectionName` (e.g., Section A, Section B), `QuestionOrder`, and `MarksAllocated`.
  - `InstitutionalGradingPolicy`: Stores grading scheme rules (UGC 10-point scale, percentage cutoffs, letter grades `O` through `F`, grade points `10.0` through `0.0`, and max allowable grace marks).
  - `ProcessedAssessmentResult`: Persists evaluated student assessment records with raw marks, percentage, UGC letter grade, grade points, grace marks awarded, attempt number, compartmental status, and publication flags.
  - `InternalAssessment`: Added `IsCompartmental` (BIT), `AttemptNumber` (INT DEFAULT 1), and `OriginalAssessmentId` self-referential foreign key.
- **DTOs / ViewModels (`TMS.ViewModels/Academics/`)**:
  - `QuestionBankViewModels.cs`: `QuestionBankFilterViewModel`, `QuestionBankItemViewModel`, `SuggestedQuestionRequestViewModel`, `SuggestedQuestionItemViewModel`, `SaveSuggestedQuestionsRequestViewModel`, `QuestionPaperCreateViewModel`, `QuestionPaperItemViewModel`.
  - `ResultProcessingViewModels.cs`: `CalculateResultsRequestDto`, `PublishProcessedResultsRequestDto`, `ResultCalculationSummaryDto`, `StudentProcessedResultDto`, `CompartmentalAssessmentCreateDto`, `InstitutionalGradingPolicyViewModel`.

### Core Capabilities Delivered

#### 1. Gemini AI Suggested Question Generator
- **Curriculum-Grounded Prompts**: Prompts are contextualized by Course, Semester, Subject, Unit/Module, Topic, Bloom's Taxonomy level (Remember, Understand, Apply, Analyze, Evaluate, Create), and Difficulty (Easy, Medium, Hard).
- **Interactive Review & Batch Saving**: Faculty can review AI generated questions, inspect suggested options, edit question text, and save in bulk directly to the institutional `QuestionBank`.
- **In-Place Question Regeneration**: `RegenerateQuestion` allows faculty to replace a single unsatisfactory question in real time while preserving the rest of the generated batch.

#### 2. Question Bank Hierarchy, Duplicate Detection & Analytics
- **Curriculum Mapping**: Every question maps to Course -> Semester -> Subject -> Unit -> Topic.
- **Duplicate Question Detection**: `CheckDuplicateQuestion` normalizes question text (case folding, punctuation stripping, whitespace condensing) and checks for duplicates within the target topic/subject to avoid clutter.
- **Question Bank Intelligence**: `GetQuestionBankAnalytics` aggregates questions by difficulty, Bloom's taxonomy distribution, and question type (`MCQ`, `Short Answer`, `Long Answer`).

#### 3. Formal Examination Paper Generator & Print Center
- **Section-Based Paper Builder**: Faculty can assemble formal papers with configurable sections (Section A: MCQs, Section B: Short Notes, Section C: Long Problems), allocating custom marks per question.
- **Institutional Hardcopy Print View**: Formatted with print-specific CSS (`@media print`) generating clean, standardized exam papers containing college header, instructions, student roll number blanks, duration, and maximum marks.
- **Lifecycle Management**: Supports drafting, editing, previewing, and publishing question papers.

#### 4. Automated Result Calculation & UGC 10-Point Grading Engine
- **UGC Standard 10-Point Scale**: Implemented statutory grading bands:
  - `O` (Outstanding): ≥ 90% (Grade Point: 10.0)
  - `A+` (Excellent): 80% – 89.99% (Grade Point: 9.0)
  - `A` (Very Good): 70% – 79.99% (Grade Point: 8.0)
  - `B+` (Good): 60% – 69.99% (Grade Point: 7.0)
  - `B` (Above Average): 50% – 59.99% (Grade Point: 6.0)
  - `C` (Average): 40% – 49.99% (Grade Point: 5.0)
  - `P` (Pass): 33% / 40% based on policy (Grade Point: 4.0)
  - `F` (Fail): Below passing mark (Grade Point: 0.0)
- **Grace Marks Engine**: Automatically identifies borderline failing students within the configured maximum grace mark threshold (e.g., 1–5 marks) and applies discretionary grace marks with audit logging.
- **Guarded Publication Policy**: Results cannot be prematurely published unless all active enrolled students have submitted, or the scheduled deadline has fully elapsed.

#### 5. Compartmental Examination Workflow
- **Failed Candidate Roster**: `GetCompartmentEligibleStudents` identifies students with `Outcome == "FAIL"` or grade `F`.
- **Automated Re-Assessment Creation**: `CreateCompartmentalAssessment` clones the assessment as a supplementary exam with `IsCompartmental = true`, increments `AttemptNumber = 2`, links `OriginalAssessmentId`, and restricts eligibility to compartmental candidates.

#### 6. Student Grade Card & Preview Upgrades
- `InternalGradeCardController.cs`, `Preview.cshtml`, and `SemesterPreview.cshtml` now render processed letter grades, grade points, overall percentage, and compartmental attempt badges (`Attempt #2`).

#### 7. Assessment Center UI & Accessibility Contrast Fixes
- `Quiz/Index.cshtml` overhauled with hero statistics counters, modern tab navigation (*Question Bank*, *Suggested Questions*, *Create Question Paper*, *Active Course Quizzes*), and global CSS overrides ensuring select dropdowns, inputs, and options have high contrast and readability.

#### 8. Database & Cloud Resiliency
- Configured resilient Azure SQL Server connection provider and retry execution strategies in `appsettings.json` and startup (`bb2c95f`).
- `DatabaseInitializer.cs` updated to seed default institutional grading policies and sample question bank taxonomy items.

---

## Real Browser E2E Testing & Button Functional Fixes (Completed 2026-10-21)

### Features In-Scope
- **Feature 25: AI Question Paper Generator** (1-Click Blueprint Auto-Assembly, Add from Bank, Preview, Save Draft, Publish, PDF Export/Print)
- **Feature 26: Question Bank Intelligence** (Analytics, Reset Filters, Add Question Form, Bulk CSV Import, AI Auto-Tag, Use Selected (0) Dynamic Counter)
- **Feature 29: AI Descriptive Answer Evaluation** (Evaluate AI, Evaluate All AI, Rubric Saving, 50% Conceptual Threshold, Faculty Override)
- **Feature 36: Automated Result Processing** (UGC CBCS 10-Point Grading, Grace Marks Engine, Compartmental Exam Scheduling, Result Publishing Guard, Student Grade Card)
*(Note: Feature 31 strictly excluded from this phase).*

### Key Bugs Resolved
1. **Database Schema Persistence (Feature 25)**:
   - Added DDL nullable columns `DurationMinutes INT NULL` and `Instructions NVARCHAR(MAX) NULL` to table `QuestionPaper`, and `SectionName NVARCHAR(100) NULL` and `SectionInstructions NVARCHAR(MAX) NULL` to table `QuestionPaperQuestion`.
   - Removed `[NotMapped]` from `QuestionPaper.cs` and `QuestionPaperQuestion.cs`, added migration/bootstrapping guards in `DatabaseInitializer.cs`. Paper durations, instructions, and section metadata now persist 100% to SQL Server.
2. **Tab Navigation Selector Mismatch (Features 25 & 26)**:
   - Fixed `Index.cshtml` tab switcher which previously targeted `#tab-papers-link` (plural) instead of the actual DOM ID `#tab-paper-link` (singular), preventing silent navigation failures.
3. **Modal Course & Subject Dropdown Populators (Feature 26)**:
   - Fixed Bulk Import and AI Auto-Tag modals which were reading from non-existent DOM IDs `#filterCourse` / `#filterSubject` instead of `#bankCourseFilter` / `#bankSubjectFilter`.
4. **"Use Selected (0)" Button & Dynamic Counter (Feature 26)**:
   - Wired up click event and change listener for `#btnCreatePaperWithSelected`. Button is disabled when 0 questions are checked, dynamically updates counter (`0` -> `N`), enables upon selection, accumulates question items into paper builder, and navigates to the builder tab without duplicate entries.
5. **Blueprint Auto-Assembly DTO Validation (Feature 25)**:
   - Relaxed overly strict `[Required] int SubjectId` in `AutoAssemblePaperRequestDto` to `int? SubjectId` to match database schema where `SubjectId` is optional for course-level papers.
   - Refined `AutoAssemblePaperFromBlueprint` in `QuizController.cs` to match questions gracefully when Bloom's taxonomy LearningOutcome is untagged.
6. **Evaluation Controller Null Reference (Feature 29)**:
   - Added null guards in `FacultyExamController.EvaluateAttempt` to avoid 500 NRE when `attempt.Exam` is null or faculty is unassigned.

### Verification Status
- `dotnet build TMS/TMS.sln`: **0 Errors, 0 Warnings** (excluding third-party package advisories).
- Automated Chrome E2E Suite (`scratch/run_full_browser_e2e.js`): All 16 action points across Admin, Faculty, and Student portals verified passing in real browser execution.
- Documentation: Detailed test results recorded in [FEATURE_FUNCTIONAL_TEST_REPORT.md](file:///e:/LMS/FEATURE_FUNCTIONAL_TEST_REPORT.md).




