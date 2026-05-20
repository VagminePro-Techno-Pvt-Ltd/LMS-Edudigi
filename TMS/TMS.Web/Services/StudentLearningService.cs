using Microsoft.EntityFrameworkCore;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    /// <summary>
    /// Implementation of IStudentLearningService.
    /// Uses AsNoTracking for all student-facing reads for performance.
    /// </summary>
    public class StudentLearningService : IStudentLearningService
    {
        private readonly ApplicationDBContext _db;

        // Quadrant display metadata (indexed by QuadrantNumber - 1)
        private static readonly string[] QIcons  = { "fa-file-pdf",      "fa-video",         "fa-circle-question", "fa-comments" };
        private static readonly string[] QColors = { "#ef4444",           "#3b82f6",          "#16a34a",            "#8b5cf6" };
        private static readonly string[] QBgs    = { "#fef2f2",           "#eff6ff",          "#f0fdf4",            "#f5f3ff" };
        private static readonly string[] QDescs  = { "PDF notes & documents", "Video lectures & recordings", "Quizzes & assignments", "Discussion forums" };

        public StudentLearningService(ApplicationDBContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════
        //  Enrollment Check
        // ═══════════════════════════════════════════════════
        public async Task<bool> IsStudentEnrolledAsync(int studentId, int courseId)
        {
            return await _db.CourseEnrollments
                .AsNoTracking()
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.IsActive);
        }

        // ═══════════════════════════════════════════════════
        //  Subject Hub — Unit Grid
        // ═══════════════════════════════════════════════════
        public async Task<SubjectHubViewModel?> GetSubjectHubAsync(int studentId, int courseId)
        {
            var course = await _db.CourseMasters
                .AsNoTracking()
                .Include(c => c.CourseCategory)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

            if (course == null) return null;

            // Get all units for this course via material mappings
            // Units are linked through CourseMaterialMapping → UnitMaster
            var mappings = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Where(m => m.CourseId == courseId && m.IsActive)
                .Select(m => new { m.UnitId, m.SubjectId, m.LectureMaterialId })
                .ToListAsync();

            var unitIds = mappings.Where(m => m.UnitId != null).Select(m => m.UnitId!.Value).Distinct().ToList();

            var units = await _db.UnitMasters
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.Id) && u.IsActive)
                .OrderBy(u => u.SortOrder)
                .ThenBy(u => u.Id)
                .Select(u => new { u.Id, u.Name, u.Description, u.SortOrder })
                .ToListAsync();

            // Material counts per unit
            var unitMaterialCounts = mappings
                .Where(m => m.UnitId != null)
                .GroupBy(m => m.UnitId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.LectureMaterialId).Distinct().Count());

            // Build unit cards
            var unitCards = units.Select(u => new UnitCardViewModel
            {
                UnitId = u.Id,
                UnitName = u.Name ?? $"Unit {u.Id}",
                UnitDescription = u.Description,
                SortOrder = u.SortOrder,
                TotalMaterials = unitMaterialCounts.GetValueOrDefault(u.Id, 0),
                CompletedMaterials = 0 // TODO: Wire up StudentMaterialProgress
            }).ToList();

            // Legacy fallback: add a virtual unit for materials where UnitId is NULL
            var legacyMappingCount = mappings.Count(m => m.UnitId == null);
            if (legacyMappingCount > 0)
            {
                unitCards.Insert(0, new UnitCardViewModel
                {
                    UnitId = 0, // 0 indicates virtual unit
                    UnitName = "General Resources & Learning Materials",
                    UnitDescription = "Core course syllabus, notes, files, and lectures mapped from the main curriculum.",
                    SortOrder = -100, // Show first
                    TotalMaterials = legacyMappingCount,
                    CompletedMaterials = 0
                });
            }

            // Also get units with NO mappings but exist for this course's subject
            // (so students can see empty units too)
            var subjectIds = mappings.Where(m => m.SubjectId != null).Select(m => m.SubjectId!.Value).Distinct().ToList();

            // Smart course-to-subject code fallback for new/empty courses
            if (!subjectIds.Any() && !string.IsNullOrEmpty(course.CourseCode))
            {
                var courseCodeClean = course.CourseCode.Replace("-", "").Replace(" ", "").ToLower();
                var semesterSubjects = await _db.SubjectMasters
                    .AsNoTracking()
                    .Where(s => s.SemesterId == course.SemesterId && s.IsActive)
                    .ToListAsync();

                subjectIds = semesterSubjects
                    .Where(s => s.SubjectCode?.Replace("-", "").Replace(" ", "").ToLower() == courseCodeClean)
                    .Select(s => s.Id)
                    .ToList();
            }

            if (subjectIds.Any())
            {
                var additionalUnits = await _db.UnitMasters
                    .AsNoTracking()
                    .Where(u => subjectIds.Contains(u.SubjectId) && u.IsActive && !unitIds.Contains(u.Id))
                    .OrderBy(u => u.SortOrder)
                    .Select(u => new { u.Id, u.Name, u.Description, u.SortOrder })
                    .ToListAsync();

                unitCards.AddRange(additionalUnits.Select(u => new UnitCardViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Name ?? $"Unit {u.Id}",
                    UnitDescription = u.Description,
                    SortOrder = u.SortOrder,
                    TotalMaterials = 0,
                    CompletedMaterials = 0
                }));
            }

            // Sort all units consistently
            unitCards = unitCards.OrderBy(u => u.SortOrder).ThenBy(u => u.UnitId).ToList();

            return new SubjectHubViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name ?? "Course",
                CourseCode = course.CourseCode,
                CourseDescription = course.Description,
                CategoryName = course.CourseCategory?.Name ?? "General",
                Credits = course.NoofCredit,
                DurationMonths = course.CourseDurationMonths,
                SemesterId = course.SemesterId ?? 0,
                SemesterName = course.Semester?.Name ?? "Semester",
                Units = unitCards
            };
        }

        // ═══════════════════════════════════════════════════
        //  Unit Details — 4 Quadrant Cards
        // ═══════════════════════════════════════════════════
        public async Task<UnitHubViewModel?> GetUnitDetailsAsync(int studentId, int courseId, int unitId)
        {
            var course = await _db.CourseMasters
                .AsNoTracking()
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

            if (course == null) return null;

            bool isLegacyUnit = (unitId == 0);
            string unitName = "General Resources & Learning Materials";
            string? unitDescription = "Core course syllabus, notes, files, and lectures mapped from the main curriculum.";

            if (!isLegacyUnit)
            {
                var unit = await _db.UnitMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == unitId && u.IsActive);

                if (unit == null) return null;
                unitName = unit.Name ?? $"Unit {unit.Id}";
                unitDescription = unit.Description;
            }

            // Get all quadrants
            var quadrants = await _db.CourseQuadrantMasters
                .AsNoTracking()
                .Where(q => q.IsActive)
                .OrderBy(q => q.QuadrantNumber)
                .Take(4)
                .ToListAsync();

            // Count materials per quadrant for this unit+course
            var materialCounts = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Where(m => m.CourseId == courseId && m.IsActive && (isLegacyUnit ? m.UnitId == null : m.UnitId == unitId))
                .GroupBy(m => m.CourseQuadrantId)
                .Select(g => new { QuadrantId = g.Key, Count = g.Count() })
                .ToListAsync();

            var countLookup = materialCounts.ToDictionary(x => x.QuadrantId, x => x.Count);

            // Check quiz attempts
            var quizAttemptQuadrants = await _db.StudentQuizAttempt
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.CourseId == courseId && a.IsActive)
                .Select(a => a.CourseQuadrantId)
                .Distinct()
                .ToListAsync();

            var quadrantCards = quadrants.Select(q =>
            {
                int qi = Math.Max(0, Math.Min(q.QuadrantNumber - 1, 3));
                return new QuadrantCardSummaryViewModel
                {
                    QuadrantId = q.Id,
                    QuadrantNumber = q.QuadrantNumber,
                    QuadrantName = q.Name ?? $"Quadrant {q.QuadrantNumber}",
                    Icon = QIcons[qi],
                    Color = QColors[qi],
                    BgColor = QBgs[qi],
                    Description = QDescs[qi],
                    MaterialCount = countLookup.GetValueOrDefault(q.Id, 0),
                    CompletedCount = 0, // TODO: Wire up StudentMaterialProgress
                    HasQuizAttempt = quizAttemptQuadrants.Contains(q.Id)
                };
            }).ToList();

            return new UnitHubViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name ?? "Course",
                CourseCode = course.CourseCode,
                SemesterId = course.SemesterId ?? 0,
                SemesterName = course.Semester?.Name ?? "Semester",
                UnitId = unitId,
                UnitName = unitName,
                UnitDescription = unitDescription,
                Quadrants = quadrantCards
            };
        }

        // ═══════════════════════════════════════════════════
        //  Quadrant Content — Material List
        // ═══════════════════════════════════════════════════
        public async Task<QuadrantMaterialListViewModel?> GetQuadrantContentAsync(int studentId, int courseId, int unitId, int quadrantId)
        {
            var course = await _db.CourseMasters
                .AsNoTracking()
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

            if (course == null) return null;

            bool isLegacyUnit = (unitId == 0);
            string unitName = "General Resources & Learning Materials";

            if (!isLegacyUnit)
            {
                var unit = await _db.UnitMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == unitId && u.IsActive);

                if (unit == null) return null;
                unitName = unit.Name ?? $"Unit {unit.Id}";
            }

            var quadrant = await _db.CourseQuadrantMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quadrantId && q.IsActive);

            if (quadrant == null) return null;

            // Get materials for this unit+quadrant+course (supporting legacy null unit)
            var materials = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Include(m => m.LectureMaterial)
                .Where(m => m.CourseId == courseId
                         && (isLegacyUnit ? m.UnitId == null : m.UnitId == unitId)
                         && m.CourseQuadrantId == quadrantId
                         && m.IsActive
                         && m.LectureMaterial != null
                         && m.LectureMaterial.IsActive)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .Select(m => new MaterialItemViewModel
                {
                    MaterialId = m.LectureMaterial!.Id,
                    MappingId = m.Id,
                    Title = m.LectureMaterial.Title ?? "Untitled",
                    MaterialType = m.LectureMaterial.MaterialType ?? "Document",
                    FilePath = m.LectureMaterial.FilePath,
                    OriginalFileName = m.LectureMaterial.OriginalFileName,
                    Source = m.LectureMaterial.Source ?? "Local",
                    FileSizeBytes = m.LectureMaterial.FileSizeBytes,
                    SortOrder = m.SortOrder,
                    UploadedOn = m.LectureMaterial.UploadedOn,
                    IsCompleted = false // TODO: Wire up StudentMaterialProgress
                })
                .ToListAsync();

            int qi = Math.Max(0, Math.Min(quadrant.QuadrantNumber - 1, 3));

            return new QuadrantMaterialListViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name ?? "Course",
                SemesterId = course.SemesterId ?? 0,
                SemesterName = course.Semester?.Name ?? "Semester",
                UnitId = unitId,
                UnitName = unitName,
                QuadrantId = quadrant.Id,
                QuadrantNumber = quadrant.QuadrantNumber,
                QuadrantName = quadrant.Name ?? $"Quadrant {quadrant.QuadrantNumber}",
                QuadrantIcon = QIcons[qi],
                QuadrantColor = QColors[qi],
                QuadrantBgColor = QBgs[qi],
                QuadrantDescription = QDescs[qi],
                Materials = materials
            };
        }
    }
}
