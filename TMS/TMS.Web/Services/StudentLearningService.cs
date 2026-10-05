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
        private static readonly string[] QIcons =
        {
            "fa-file-pdf",
            "fa-video",
            "fa-circle-question",
            "fa-comments"
        };

        private static readonly string[] QColors =
        {
            "#ef4444",
            "#3b82f6",
            "#16a34a",
            "#8b5cf6"
        };

        private static readonly string[] QBgs =
        {
            "#fef2f2",
            "#eff6ff",
            "#f0fdf4",
            "#f5f3ff"
        };

        private static readonly string[] QDescs =
        {
            "PDF notes & documents",
            "Video lectures & recordings",
            "Quizzes & assignments",
            "Discussion forums"
        };

        public StudentLearningService(ApplicationDBContext db)
        {
            _db = db;
        }

        // ------------------------------------------------------------
        // Enrollment Check
        // ------------------------------------------------------------

        public async Task<bool> IsStudentEnrolledAsync(int studentId, int courseId)
        {
            return await _db.CourseEnrollments
                .AsNoTracking()
                .AnyAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == courseId &&
                    e.IsActive);
        }

        // ------------------------------------------------------------
        // Subject Hub - Unit Grid
        // ------------------------------------------------------------

        public async Task<SubjectHubViewModel?> GetSubjectHubAsync(
            int studentId,
            int courseId)
        {
            var course = await _db.CourseMasters
                .AsNoTracking()
                .Include(c => c.CourseCategory)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c =>
                    c.Id == courseId &&
                    c.IsActive);

            if (course == null)
            {
                return null;
            }

            var mappings = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Where(m =>
                    m.CourseId == courseId &&
                    m.IsActive)
                .Select(m => new
                {
                    m.UnitId,
                    m.SubjectId,
                    m.LectureMaterialId
                })
                .ToListAsync();

            var unitIds = mappings
                .Where(m => m.UnitId != null)
                .Select(m => m.UnitId!.Value)
                .Distinct()
                .ToList();

            var units = await _db.UnitMasters
                .AsNoTracking()
                .Where(u =>
                    unitIds.Contains(u.Id) &&
                    u.IsActive)
                .OrderBy(u => u.SortOrder)
                .ThenBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Description,
                    u.SortOrder
                })
                .ToListAsync();

            var unitMaterialCounts = mappings
                .Where(m => m.UnitId != null)
                .GroupBy(m => m.UnitId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(x => x.LectureMaterialId)
                        .Distinct()
                        .Count());

            var unitCards = units
                .Select(u => new UnitCardViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Name ?? $"Unit {u.Id}",
                    UnitDescription = u.Description,
                    SortOrder = u.SortOrder,
                    TotalMaterials = unitMaterialCounts.GetValueOrDefault(u.Id, 0),
                    CompletedMaterials = 0
                })
                .ToList();

            // Legacy fallback for materials where UnitId is NULL.
            var legacyMappingCount = mappings.Count(m => m.UnitId == null);

            if (legacyMappingCount > 0)
            {
                unitCards.Insert(0, new UnitCardViewModel
                {
                    UnitId = 0,
                    UnitName = "General Resources & Learning Materials",
                    UnitDescription =
                        "Core course syllabus, notes, files, and lectures mapped from the main curriculum.",
                    SortOrder = -100,
                    TotalMaterials = legacyMappingCount,
                    CompletedMaterials = 0
                });
            }

            var subjectIds = mappings
                .Where(m => m.SubjectId != null)
                .Select(m => m.SubjectId!.Value)
                .Distinct()
                .ToList();

            // Course-to-subject fallback for courses without mappings.
            if (!subjectIds.Any() &&
                !string.IsNullOrEmpty(course.CourseCode))
            {
                var courseCodeClean = course.CourseCode
                    .Replace("-", "")
                    .Replace(" ", "")
                    .ToLower();

                var semesterSubjects = await _db.SubjectMasters
                    .AsNoTracking()
                    .Where(s =>
                        s.SemesterId == course.SemesterId &&
                        s.IsActive)
                    .ToListAsync();

                subjectIds = semesterSubjects
                    .Where(s =>
                        s.SubjectCode?
                            .Replace("-", "")
                            .Replace(" ", "")
                            .ToLower() == courseCodeClean)
                    .Select(s => s.Id)
                    .ToList();
            }

            if (subjectIds.Any())
            {
                var additionalUnits = await _db.UnitMasters
                    .AsNoTracking()
                    .Where(u =>
                        subjectIds.Contains(u.SubjectId) &&
                        u.IsActive &&
                        !unitIds.Contains(u.Id))
                    .OrderBy(u => u.SortOrder)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Description,
                        u.SortOrder
                    })
                    .ToListAsync();

                unitCards.AddRange(
                    additionalUnits.Select(u => new UnitCardViewModel
                    {
                        UnitId = u.Id,
                        UnitName = u.Name ?? $"Unit {u.Id}",
                        UnitDescription = u.Description,
                        SortOrder = u.SortOrder,
                        TotalMaterials = 0,
                        CompletedMaterials = 0
                    }));
            }

            unitCards = unitCards
                .OrderBy(u => u.SortOrder)
                .ThenBy(u => u.UnitId)
                .ToList();

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

        // ------------------------------------------------------------
        // Unit Details - 4 Quadrant Cards
        // ------------------------------------------------------------

        public async Task<UnitHubViewModel?> GetUnitDetailsAsync(
            int studentId,
            int courseId,
            int unitId)
        {
            var course = await _db.CourseMasters
                .AsNoTracking()
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c =>
                    c.Id == courseId &&
                    c.IsActive);

            if (course == null)
            {
                return null;
            }

            bool isLegacyUnit = unitId == 0;

            string unitName = "General Resources & Learning Materials";
            string? unitDescription =
                "Core course syllabus, notes, files, and lectures mapped from the main curriculum.";

            if (!isLegacyUnit)
            {
                var unit = await _db.UnitMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.Id == unitId &&
                        u.IsActive);

                if (unit == null)
                {
                    return null;
                }

                unitName = unit.Name ?? $"Unit {unit.Id}";
                unitDescription = unit.Description;
            }

            var quadrants = await _db.CourseQuadrantMasters
                .AsNoTracking()
                .Where(q => q.IsActive)
                .OrderBy(q => q.QuadrantNumber)
                .Take(4)
                .Select(q => new
                {
                    q.Id,
                    q.QuadrantNumber,
                    q.Name
                })
                .ToListAsync();

            /*
             * Load active mappings with MaterialType.
             *
             * Student-facing classification rules:
             *
             * Document                -> Q1
             * Video / VideoURL        -> Q2
             * Other material types    -> keep saved quadrant
             *
             * This means old VideoURL mappings accidentally saved under Q1
             * are shown under Q2 without requiring a database migration.
             */
            var mappedMaterials = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Where(m =>
                    m.CourseId == courseId &&
                    m.IsActive &&
                    (isLegacyUnit
                        ? m.UnitId == null
                        : m.UnitId == unitId) &&
                    m.LectureMaterial != null &&
                    m.LectureMaterial.IsActive)
                .Select(m => new
                {
                    m.CourseQuadrantId,
                    MaterialType = m.LectureMaterial!.MaterialType
                })
                .ToListAsync();

            var materialCountsByQuadrantId = new Dictionary<int, int>();

            foreach (var quadrant in quadrants)
            {
                int count = 0;

                foreach (var mapping in mappedMaterials)
                {
                    int savedQuadrantNumber = quadrants
                        .Where(q => q.Id == mapping.CourseQuadrantId)
                        .Select(q => q.QuadrantNumber)
                        .FirstOrDefault();

                    int effectiveQuadrantNumber =
                        GetEffectiveQuadrantNumber(
                            mapping.MaterialType,
                            savedQuadrantNumber);

                    if (effectiveQuadrantNumber == quadrant.QuadrantNumber)
                    {
                        count++;
                    }
                }

                materialCountsByQuadrantId[quadrant.Id] = count;
            }

            var quizAttemptQuadrants = await _db.StudentQuizAttempt
                .AsNoTracking()
                .Where(a =>
                    a.StudentId == studentId &&
                    a.CourseId == courseId &&
                    a.IsActive)
                .Select(a => a.CourseQuadrantId)
                .Distinct()
                .ToListAsync();

            var quadrantCards = quadrants
                .Select(q =>
                {
                    int qi = Math.Max(
                        0,
                        Math.Min(q.QuadrantNumber - 1, 3));

                    return new QuadrantCardSummaryViewModel
                    {
                        QuadrantId = q.Id,
                        QuadrantNumber = q.QuadrantNumber,
                        QuadrantName =
                            q.Name ?? $"Quadrant {q.QuadrantNumber}",
                        Icon = QIcons[qi],
                        Color = QColors[qi],
                        BgColor = QBgs[qi],
                        Description = QDescs[qi],
                        MaterialCount =
                            materialCountsByQuadrantId.GetValueOrDefault(q.Id, 0),
                        CompletedCount = 0,
                        HasQuizAttempt =
                            quizAttemptQuadrants.Contains(q.Id)
                    };
                })
                .ToList();

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

        // ------------------------------------------------------------
        // Quadrant Content - Material List
        // ------------------------------------------------------------

        public async Task<QuadrantMaterialListViewModel?> GetQuadrantContentAsync(
            int studentId,
            int courseId,
            int unitId,
            int quadrantId)
        {
            var course = await _db.CourseMasters
                .AsNoTracking()
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c =>
                    c.Id == courseId &&
                    c.IsActive);

            if (course == null)
            {
                return null;
            }

            bool isLegacyUnit = unitId == 0;
            string unitName = "General Resources & Learning Materials";

            if (!isLegacyUnit)
            {
                var unit = await _db.UnitMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.Id == unitId &&
                        u.IsActive);

                if (unit == null)
                {
                    return null;
                }

                unitName = unit.Name ?? $"Unit {unit.Id}";
            }

            var quadrants = await _db.CourseQuadrantMasters
                .AsNoTracking()
                .Where(q => q.IsActive)
                .Select(q => new
                {
                    q.Id,
                    q.QuadrantNumber,
                    q.Name
                })
                .ToListAsync();

            var quadrant = quadrants
                .FirstOrDefault(q => q.Id == quadrantId);

            if (quadrant == null)
            {
                return null;
            }

            /*
             * Do NOT filter by CourseQuadrantId in SQL.
             *
             * Old VideoURL mappings may have been stored with Q1.
             * We load the unit's active mappings and determine the
             * effective student-facing quadrant from MaterialType.
             */
            var mappedMaterials = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Include(m => m.LectureMaterial)
                .Where(m =>
                    m.CourseId == courseId &&
                    (isLegacyUnit
                        ? m.UnitId == null
                        : m.UnitId == unitId) &&
                    m.IsActive &&
                    m.LectureMaterial != null &&
                    m.LectureMaterial.IsActive)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .ToListAsync();

            var filteredMappings = mappedMaterials
                .Where(m =>
                {
                    int savedQuadrantNumber = quadrants
                        .Where(q => q.Id == m.CourseQuadrantId)
                        .Select(q => q.QuadrantNumber)
                        .FirstOrDefault();

                    int effectiveQuadrantNumber =
                        GetEffectiveQuadrantNumber(
                            m.LectureMaterial?.MaterialType,
                            savedQuadrantNumber);

                    return effectiveQuadrantNumber ==
                           quadrant.QuadrantNumber;
                })
                .ToList();

            var materials = filteredMappings
                .Select(m => new MaterialItemViewModel
                {
                    MaterialId = m.LectureMaterial!.Id,
                    MappingId = m.Id,
                    Title = m.LectureMaterial.Title ?? "Untitled",
                    MaterialType =
                        m.LectureMaterial.MaterialType ?? "Document",
                    FilePath = m.LectureMaterial.FilePath,
                    OriginalFileName =
                        m.LectureMaterial.OriginalFileName,
                    Source = m.LectureMaterial.Source ?? "Local",
                    FileSizeBytes =
                        m.LectureMaterial.FileSizeBytes,
                    SortOrder = m.SortOrder,
                    UploadedOn = m.LectureMaterial.UploadedOn,
                    IsCompleted = false
                })
                .ToList();

            int qi = Math.Max(
                0,
                Math.Min(quadrant.QuadrantNumber - 1, 3));

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
                QuadrantName =
                    quadrant.Name ??
                    $"Quadrant {quadrant.QuadrantNumber}",
                QuadrantIcon = QIcons[qi],
                QuadrantColor = QColors[qi],
                QuadrantBgColor = QBgs[qi],
                QuadrantDescription = QDescs[qi],
                Materials = materials
            };
        }

        /// <summary>
        /// Resolves the student-facing quadrant from MaterialType.
        ///
        /// Known content types use their canonical quadrant:
        /// Document -> Q1
        /// Video / VideoURL -> Q2
        ///
        /// Unknown types preserve the quadrant stored in the mapping.
        /// </summary>
        private static int GetEffectiveQuadrantNumber(
            string? materialType,
            int savedQuadrantNumber)
        {
            if (string.Equals(
                    materialType,
                    "Document",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(
                    materialType,
                    "Video",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    materialType,
                    "VideoURL",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return savedQuadrantNumber;
        }
    }
}