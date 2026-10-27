using Microsoft.EntityFrameworkCore;
using TMS.Common;
using TMS.Models.Academics;
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
        // Enrollment Checks & Course/Content Authorization
        // ------------------------------------------------------------

        public async Task<bool> IsStudentEnrolledAsync(int studentId, int courseId)
        {
            if (studentId <= 0 || courseId <= 0) return false;

            return await _db.CourseEnrollments
                .AsNoTracking()
                .AnyAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == courseId &&
                    e.IsActive &&
                    (e.Status == 0 || e.Status == (int)EnrollmentStatus.Active));
        }

        public async Task<List<int>> GetEnrolledCourseIdsAsync(int studentId)
        {
            if (studentId <= 0) return new List<int>();

            return await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e =>
                    e.StudentId == studentId &&
                    e.IsActive &&
                    (e.Status == 0 || e.Status == (int)EnrollmentStatus.Active))
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> IsStudentEnrolledInMaterialAsync(int studentId, int materialId)
        {
            if (studentId <= 0 || materialId <= 0) return false;

            var enrolledCourseIds = await GetEnrolledCourseIdsAsync(studentId);
            if (!enrolledCourseIds.Any()) return false;

            // 1. Direct course mapping on LectureMaterial
            bool directMatch = await _db.LectureMaterials
                .AsNoTracking()
                .AnyAsync(m => m.Id == materialId && m.IsActive && m.CourseId.HasValue && enrolledCourseIds.Contains(m.CourseId.Value));

            if (directMatch) return true;

            // 2. Mapping via CourseMaterialMapping
            bool mappingMatch = await _db.CourseMaterialMappings
                .AsNoTracking()
                .AnyAsync(m => m.LectureMaterialId == materialId && m.IsActive && enrolledCourseIds.Contains(m.CourseId));

            return mappingMatch;
        }

        public async Task<bool> IsStudentEnrolledInPPTAsync(int studentId, int pptId)
        {
            if (studentId <= 0 || pptId <= 0) return false;

            var enrolledCourseIds = await GetEnrolledCourseIdsAsync(studentId);
            if (!enrolledCourseIds.Any()) return false;

            return await _db.InteractivePPTs
                .AsNoTracking()
                .AnyAsync(p => p.Id == pptId && p.IsActive && enrolledCourseIds.Contains(p.CourseId));
        }

        public async Task<bool> IsStudentEnrolledInQuizAsync(int studentId, int quizId)
        {
            if (studentId <= 0 || quizId <= 0) return false;

            var enrolledCourseIds = await GetEnrolledCourseIdsAsync(studentId);
            if (!enrolledCourseIds.Any()) return false;

            return await _db.QuizMasters
                .AsNoTracking()
                .AnyAsync(q => q.Id == quizId && q.IsActive && enrolledCourseIds.Contains(q.CourseId));
        }

        public async Task<bool> IsStudentEnrolledInAssignmentAsync(int studentId, int assignmentId)
        {
            if (studentId <= 0 || assignmentId <= 0) return false;

            var enrolledCourseIds = await GetEnrolledCourseIdsAsync(studentId);
            if (!enrolledCourseIds.Any()) return false;

            var assignment = await _db.AssignmentMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

            if (assignment == null) return false;

            if (assignment.CourseId.HasValue && enrolledCourseIds.Contains(assignment.CourseId.Value))
                return true;

            var enrolledProgramIds = await _db.CourseMasters
                .AsNoTracking()
                .Where(c => enrolledCourseIds.Contains(c.Id))
                .Select(c => c.CourseCategoryId)
                .Distinct()
                .ToListAsync();

            return enrolledProgramIds.Contains(assignment.ProgramId);
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

            // 1. Fetch completed materials for this student
            var completedMatIds = await _db.LectureVideoProgresses
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.IsActive && (p.IsCompleted || p.ProgressPercentage >= 75))
                .Select(p => p.LectureMaterialId)
                .Distinct()
                .ToListAsync();

            var completedMatSet = new HashSet<int>(completedMatIds);

            var unitCompletedCounts = mappings
                .Where(m => m.UnitId != null && completedMatSet.Contains(m.LectureMaterialId))
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
                    CompletedMaterials = unitCompletedCounts.GetValueOrDefault(u.Id, 0)
                })
                .ToList();

            // Legacy fallback for materials where UnitId is NULL.
            var legacyMappingCount = mappings.Count(m => m.UnitId == null);

            if (legacyMappingCount > 0)
            {
                var legacyCompleted = mappings
                    .Where(m => m.UnitId == null && completedMatSet.Contains(m.LectureMaterialId))
                    .Select(x => x.LectureMaterialId)
                    .Distinct()
                    .Count();

                unitCards.Insert(0, new UnitCardViewModel
                {
                    UnitId = 0,
                    UnitName = "General Resources & Learning Materials",
                    UnitDescription =
                        "Core course syllabus, notes, files, and lectures mapped from the main curriculum.",
                    SortOrder = -100,
                    TotalMaterials = legacyMappingCount,
                    CompletedMaterials = legacyCompleted
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
                    m.LectureMaterialId,
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

            var completedMatIds = await GetCompletedMaterialIdsSafeAsync(studentId);
            var completedMatSet = new HashSet<int>(completedMatIds);

            var completedCountsByQuadrantId = new Dictionary<int, int>();
            foreach (var quadrant in quadrants)
            {
                int compCount = 0;
                foreach (var mapping in mappedMaterials)
                {
                    int savedQuadrantNumber = quadrants
                        .Where(q => q.Id == mapping.CourseQuadrantId)
                        .Select(q => q.QuadrantNumber)
                        .FirstOrDefault();

                    int effectiveQuadrantNumber = GetEffectiveQuadrantNumber(mapping.MaterialType, savedQuadrantNumber);
                    if (effectiveQuadrantNumber == quadrant.QuadrantNumber && completedMatSet.Contains(mapping.LectureMaterialId))
                    {
                        compCount++;
                    }
                }
                completedCountsByQuadrantId[quadrant.Id] = compCount;
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
                        CompletedCount =
                            completedCountsByQuadrantId.GetValueOrDefault(q.Id, 0),
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

            var completedMatIds = await GetCompletedMaterialIdsSafeAsync(studentId);
            var completedMatSet = new HashSet<int>(completedMatIds);

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
                    IsCompleted = completedMatSet.Contains(m.LectureMaterial.Id)
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

        public async Task<int> GetCourseProgressPercentAsync(int studentId, int courseId)
        {
            var map = await GetCoursesProgressMapAsync(studentId, new[] { courseId });
            return map.TryGetValue(courseId, out var stat) ? stat.Percent : 0;
        }

        public async Task<Dictionary<int, (int Total, int Completed, int Percent)>> GetCoursesProgressMapAsync(int studentId, IEnumerable<int> courseIds)
        {
            var result = new Dictionary<int, (int Total, int Completed, int Percent)>();
            var cIds = courseIds?.Distinct().ToList() ?? new List<int>();
            if (!cIds.Any() || studentId <= 0) return result;

            try
            {
                var mappings = await _db.CourseMaterialMappings
                    .AsNoTracking()
                    .Where(m => cIds.Contains(m.CourseId) && m.IsActive && m.LectureMaterial != null && m.LectureMaterial.IsActive)
                    .Select(m => new { m.CourseId, m.LectureMaterialId })
                    .ToListAsync();

                var directMats = await _db.LectureMaterials
                    .AsNoTracking()
                    .Where(m => m.CourseId.HasValue && cIds.Contains(m.CourseId.Value) && m.IsActive)
                    .Select(m => new { CourseId = m.CourseId.Value, LectureMaterialId = m.Id })
                    .ToListAsync();

                var allCourseMaterialPairs = mappings.Concat(directMats).Distinct().ToList();

                var completedMatIds = await GetCompletedMaterialIdsSafeAsync(studentId);
                var completedSet = new HashSet<int>(completedMatIds);

                foreach (var cid in cIds)
                {
                    var courseMats = allCourseMaterialPairs.Where(m => m.CourseId == cid).Select(m => m.LectureMaterialId).Distinct().ToList();
                    int total = courseMats.Count;
                    if (total == 0)
                    {
                        result[cid] = (0, 0, 0);
                        continue;
                    }
                    int completed = courseMats.Count(id => completedSet.Contains(id));
                    int pct = (int)Math.Round((double)completed / total * 100);
                    result[cid] = (total, completed, pct);
                }
            }
            catch
            {
                foreach (var cid in cIds)
                {
                    result[cid] = (0, 0, 0);
                }
            }

            return result;
        }

        public async Task MarkMaterialViewedAsync(int studentId, int materialId)
        {
            if (studentId <= 0 || materialId <= 0) return;

            try
            {
                var existing = await _db.LectureVideoProgresses
                    .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureMaterialId == materialId);

                if (existing == null)
                {
                    existing = new LectureVideoProgress
                    {
                        StudentId = studentId,
                        LectureMaterialId = materialId,
                        WatchedSeconds = 100,
                        DurationSeconds = 100,
                        ProgressPercentage = 100m,
                        IsCompleted = true,
                        LastPositionSeconds = 100,
                        LastWatchedAt = DateTime.UtcNow,
                        CreatedBy = studentId,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    };
                    _db.LectureVideoProgresses.Add(existing);
                }
                else
                {
                    existing.IsCompleted = true;
                    existing.ProgressPercentage = 100m;
                    existing.LastWatchedAt = DateTime.UtcNow;
                    existing.UpdatedBy = studentId;
                    existing.UpdatedOn = DateTime.UtcNow;
                    _db.Entry(existing).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();
            }
            catch
            {
                try
                {
                    await DatabaseInitializer.EnsureTablesCreatedAsync(_db);
                }
                catch { }
            }
        }

        private async Task<List<int>> GetCompletedMaterialIdsSafeAsync(int studentId)
        {
            if (studentId <= 0) return new List<int>();
            try
            {
                return await _db.LectureVideoProgresses
                    .AsNoTracking()
                    .Where(p => p.StudentId == studentId && p.IsActive && (p.IsCompleted || p.ProgressPercentage >= 75 || p.WatchedSeconds > 0))
                    .Select(p => p.LectureMaterialId)
                    .Distinct()
                    .ToListAsync();
            }
            catch
            {
                try
                {
                    await DatabaseInitializer.EnsureTablesCreatedAsync(_db);
                    return await _db.LectureVideoProgresses
                        .AsNoTracking()
                        .Where(p => p.StudentId == studentId && p.IsActive && (p.IsCompleted || p.ProgressPercentage >= 75 || p.WatchedSeconds > 0))
                        .Select(p => p.LectureMaterialId)
                        .Distinct()
                        .ToListAsync();
                }
                catch
                {
                    return new List<int>();
                }
            }
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