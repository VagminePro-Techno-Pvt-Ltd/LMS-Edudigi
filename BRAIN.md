# EduDigi Classes LMS - Verified Project Context

Last source audit: 2026-10-12

This handoff covers the application under `TMS/`. “Implemented” means present in audited source, not proven in deployment. This audit did not connect to SQL Server, run migrations, start the site, seed/delete data, or edit application source/configuration.

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
| Assignments/quizzes | `AssignmentController.cs`, `StudentQuizController.cs`, `FacultyQuizAssessmentController.cs`, Admin Quiz controllers | Creation, submission/attempt and evaluation paths exist. |
| Formal examinations | `FacultyExamController.cs`, `StudentExamController.cs`, `Areas/Admin/Controllers/AdminExamController.cs`; `TMS.Models/Assessments/` | Exam, section, question bank, assignment, attempt, evaluation and `ExamResult` flows exist. They are separate from IA grade cards. |
| Internal Assessment | `InternalAssessmentController.cs`, `Views/InternalAssessment/`, `wwwroot/js/internal-assessment.js` | Scheduling, types, creation/edit, questions, submissions, evaluation and legacy approval/publication exist. |
| Internal marks entry | `InternalMarksEntryController.cs`, `Views/InternalMarksEntry/Index.cshtml` | Updates existing non-question submissions only; it cannot create missing/offline marks rows. |
| Internal grade cards | `InternalGradeCardController.cs`, `ViewModels/InternalGradeCardViewModel.cs`, `Views/InternalGradeCard/` | Interactive dashboard and printable course/semester IA statements exist; they are not official immutable grade cards. |
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
5. Grade cards lack official snapshot/PDF/verification and all external/credit/GPA data.
6. Null-subject semester grouping and inconsistent enrollment `IsActive` filtering can produce incorrect visibility/aggregation.
7. No compartment/repeat/improvement model exists.
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

## Next priorities

1. Implement faculty-direct publication across controller, marks entry, views and confirmation text while preserving all legacy approval values.
2. Add owner/successor/unrelated-faculty/admin/student authorization and publication-history tests.
3. Inventory live schema and produce one additive reviewed IA reconciliation migration.
4. Resolve historical enrollment visibility and subject/course grouping.
5. Obtain grading, external marks, compartment and official-document decisions before building a final grade card.

Recommended immediate task: **replace the legacy Admin approval gate with tested faculty-direct result publication without altering historical approval values**, then browser-test Publish/Unpublish warnings and role visibility.
