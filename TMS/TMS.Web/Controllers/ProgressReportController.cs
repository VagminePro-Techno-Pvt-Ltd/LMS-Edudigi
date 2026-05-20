using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMS.Repository;
using TMS.ViewModels;
using TMS.Models.Masters;
using TMS.Models.Academics;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class ProgressReportController : BaseController
    {
        private readonly ApplicationDBContext _db;

        public ProgressReportController(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string type = "all")
        {
            int studentId = GetUserId();
            if (studentId == 0)
                return RedirectToAction("Login", "Auth");

            var student = await _db.UserMasters.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student == null)
                return NotFound();

            var model = new ProgressReportViewModel
            {
                StudentName = student.Name ?? "Student",
                ReportType = type.ToLower()
            };

            // Get student's enrolled courses
            var enrolledCourseIds = await _db.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.IsActive)
                .Select(e => e.CourseId)
                .ToListAsync();

            if (enrolledCourseIds.Any())
            {
                // Calculate Quiz Progress
                model.QuizProgress = await CalculateQuizProgress(studentId, enrolledCourseIds);

                // Calculate Assignment Progress
                model.AssignmentProgress = await CalculateAssignmentProgress(studentId, enrolledCourseIds);

                // Calculate Study Material Progress
                model.StudyMaterialProgress = await CalculateStudyMaterialProgress(studentId, enrolledCourseIds);

                // Calculate E-Content Progress
                model.EContentProgress = await CalculateEContentProgress(studentId, enrolledCourseIds);

                // Calculate Overall Progress
                var progressValues = new[] {
                    model.QuizProgress.CompletionPercentage,
                    model.AssignmentProgress.CompletionPercentage,
                    model.StudyMaterialProgress.CompletionPercentage,
                    model.EContentProgress.CompletionPercentage
                };
                model.OverallProgress = progressValues.Any() ? progressValues.Average() : 0;
            }

            return View(model);
        }

        private async Task<QuizProgressData> CalculateQuizProgress(int studentId, System.Collections.Generic.List<int> courseIds)
        {
            var data = new QuizProgressData();

            try
            {
                // Get all quizzes for enrolled courses - try QuizMaster first, then QuizMasters
                var allQuizzes = new List<QuizMaster>();
                
                try
                {
                    allQuizzes = await _db.QuizMaster
                        .Where(q => courseIds.Contains(q.CourseId) && q.IsActive)
                        .ToListAsync();
                }
                catch
                {
                    // Fallback to QuizMasters if QuizMaster doesn't work
                    allQuizzes = await _db.QuizMasters
                        .Where(q => courseIds.Contains(q.CourseId) && q.IsActive)
                        .ToListAsync();
                }

                data.TotalQuizzes = allQuizzes.Count();

                if (data.TotalQuizzes > 0)
                {
                    // Get attempted quizzes
                    var attemptedQuizIds = await _db.StudentQuizAttempt
                        .Where(a => a.StudentId == studentId && a.IsActive)
                        .Select(a => a.QuizId)
                        .Distinct()
                        .ToListAsync();

                    data.AttemptedQuizzes = attemptedQuizIds.Count();
                    data.PendingQuizzes = data.TotalQuizzes - data.AttemptedQuizzes;
                    data.CompletionPercentage = Math.Round((decimal)data.AttemptedQuizzes / data.TotalQuizzes * 100, 2);

                    // Simple unit-wise progress
                    data.UnitWiseProgress.Add(new UnitProgress
                    {
                        UnitName = "All Units",
                        Total = data.TotalQuizzes,
                        Completed = data.AttemptedQuizzes,
                        Percentage = data.CompletionPercentage
                    });
                }
            }
            catch (Exception)
            {
                // If quiz tables don't exist, return empty data
                data.TotalQuizzes = 0;
                data.AttemptedQuizzes = 0;
                data.PendingQuizzes = 0;
                data.CompletionPercentage = 0;
            }

            return data;
        }

        private async Task<AssignmentProgressData> CalculateAssignmentProgress(int studentId, System.Collections.Generic.List<int> courseIds)
        {
            var data = new AssignmentProgressData();

            // Get all assignments for enrolled courses
            var allAssignments = await _db.AssignmentMasters
                .Where(a => a.CourseId.HasValue && courseIds.Contains(a.CourseId.Value) && a.IsActive)
                .ToListAsync();

            data.TotalAssignments = allAssignments.Count();

            if (data.TotalAssignments > 0)
            {
                // Get submitted assignments
                var submittedAssignmentIds = await _db.AssignmentSubmissions
                    .Where(s => s.StudentId == studentId && s.IsActive)
                    .Select(s => s.AssignmentId)
                    .Distinct()
                    .ToListAsync();

                data.SubmittedAssignments = submittedAssignmentIds.Count();
                data.PendingAssignments = data.TotalAssignments - data.SubmittedAssignments;
                data.CompletionPercentage = Math.Round((decimal)data.SubmittedAssignments / data.TotalAssignments * 100, 2);

                // Simple unit-wise progress
                data.UnitWiseProgress.Add(new UnitProgress
                {
                    UnitName = "All Units",
                    Total = data.TotalAssignments,
                    Completed = data.SubmittedAssignments,
                    Percentage = data.CompletionPercentage
                });
            }

            return data;
        }

        private async Task<StudyMaterialProgressData> CalculateStudyMaterialProgress(int studentId, System.Collections.Generic.List<int> courseIds)
        {
            var data = new StudyMaterialProgressData();

            // Get all study materials for enrolled courses
            var allMaterials = await _db.LectureMaterials
                .Where(m => m.CourseId.HasValue && courseIds.Contains(m.CourseId.Value) && m.IsActive)
                .ToListAsync();

            data.TotalMaterials = allMaterials.Count();

            if (data.TotalMaterials > 0)
            {
                // For now, assume 50% completion as we don't have direct tracking
                data.ReadMaterials = (int)(data.TotalMaterials * 0.5);
                data.PendingMaterials = data.TotalMaterials - data.ReadMaterials;
                data.CompletionPercentage = Math.Round((decimal)data.ReadMaterials / data.TotalMaterials * 100, 2);

                // Simple unit-wise progress
                data.UnitWiseProgress.Add(new UnitProgress
                {
                    UnitName = "All Units",
                    Total = data.TotalMaterials,
                    Completed = data.ReadMaterials,
                    Percentage = data.CompletionPercentage
                });
            }

            return data;
        }

        private async Task<EContentProgressData> CalculateEContentProgress(int studentId, System.Collections.Generic.List<int> courseIds)
        {
            var data = new EContentProgressData();

            try
            {
                // 1. Fetch course names to display next to videos
                var coursesDict = await _db.CourseMasters
                    .Where(c => courseIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.Name ?? "Course");

                // 2. Fetch ALL mapped materials for enrolled courses
                var mappings = await _db.CourseMaterialMappings
                    .Include(m => m.LectureMaterial)
                    .Where(m => courseIds.Contains(m.CourseId) && m.IsActive && m.LectureMaterial != null && m.LectureMaterial.IsActive)
                    .ToListAsync();

                // Extract and filter items to match the same signature logic as QuadrantContent
                var videoMaterialsList = new List<(LectureMaterial Material, int CourseId)>();
                var seenMaterialIds = new HashSet<int>();

                foreach (var m in mappings)
                {
                    var mat = m.LectureMaterial;
                    if (mat == null || seenMaterialIds.Contains(mat.Id)) continue;

                    var mType = (mat.MaterialType ?? "").ToLower();
                    var fPath = (mat.FilePath ?? "").ToLower();
                    var fExt = string.IsNullOrEmpty(fPath) ? "" : System.IO.Path.GetExtension(fPath).ToLower();

                    bool isVideo = mType == "video" 
                                || mType == "videourl"
                                || new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" }.Contains(fExt)
                                || fPath.Contains("youtu") 
                                || fPath.Contains("vimeo");

                    if (isVideo)
                    {
                        videoMaterialsList.Add((mat, m.CourseId));
                        seenMaterialIds.Add(mat.Id);
                    }
                }

                // 3. Fallback: Fetch directly linked materials (if any are not in mappings)
                var directVideoMaterials = await _db.LectureMaterials
                    .Where(m => m.CourseId.HasValue && courseIds.Contains(m.CourseId.Value) && m.IsActive 
                             && !seenMaterialIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var mat in directVideoMaterials)
                {
                    var mType = (mat.MaterialType ?? "").ToLower();
                    var fPath = (mat.FilePath ?? "").ToLower();
                    var fExt = string.IsNullOrEmpty(fPath) ? "" : System.IO.Path.GetExtension(fPath).ToLower();

                    bool isVideo = mType == "video" 
                                || mType == "videourl"
                                || new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" }.Contains(fExt)
                                || fPath.Contains("youtu") 
                                || fPath.Contains("vimeo");

                    if (isVideo && mat.CourseId.HasValue)
                    {
                        videoMaterialsList.Add((mat, mat.CourseId.Value));
                        seenMaterialIds.Add(mat.Id);
                    }
                }

                data.TotalContent = videoMaterialsList.Count;

                if (data.TotalContent > 0)
                {
                    var videoMaterialIds = videoMaterialsList.Select(v => v.Material.Id).ToList();

                    // 4. Get detailed progress records
                    var progressRecords = await _db.LectureVideoProgresses
                        .Where(p => p.StudentId == studentId 
                                  && videoMaterialIds.Contains(p.LectureMaterialId) 
                                  && p.IsActive)
                        .ToListAsync();

                    var progressMap = progressRecords
                        .GroupBy(p => p.LectureMaterialId)
                        .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.ProgressPercentage).FirstOrDefault());

                    var completedVideoCount = 0;
                    decimal totalProgressSum = 0m;

                    // 5. Populate itemized lists
                    foreach (var pair in videoMaterialsList)
                    {
                        var mat = pair.Material;
                        var cid = pair.CourseId;

                        var item = new VideoProgressItem
                        {
                            VideoTitle = mat.Title ?? "Untitled Video",
                            CourseName = coursesDict.ContainsKey(cid) ? coursesDict[cid] : "Course Content"
                        };

                        if (progressMap.TryGetValue(mat.Id, out var record) && record != null)
                        {
                            item.WatchedSeconds = record.WatchedSeconds;
                            item.DurationSeconds = record.DurationSeconds;
                            item.IsCompleted = record.IsCompleted;
                            item.LastWatchedAt = record.LastWatchedAt;
                            
                            item.WatchedPercentage = record.ProgressPercentage;
                            if (item.WatchedPercentage > 100m) item.WatchedPercentage = 100m;
                            if (item.WatchedPercentage < 0m) item.WatchedPercentage = 0m;
                            item.WatchedPercentage = Math.Round(item.WatchedPercentage, 2);

                            if (record.IsCompleted) completedVideoCount++;
                            totalProgressSum += item.WatchedPercentage;
                        }
                        else
                        {
                            item.WatchedPercentage = 0m;
                            item.IsCompleted = false;
                        }

                        data.VideoItems.Add(item);
                    }

                    data.CompletedContent = completedVideoCount;
                    data.PendingContent = data.TotalContent - data.CompletedContent;
                    
                    // Overall completion is computed as the mathematical average of percentage watched across ALL videos
                    data.CompletionPercentage = Math.Round(totalProgressSum / data.TotalContent, 2);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CalculateEContentProgress error: {ex.Message}");
                data.TotalContent = 0;
                data.CompletedContent = 0;
                data.PendingContent = 0;
                data.CompletionPercentage = 0;
            }

            // Simple unit-wise progress
            if (data.TotalContent > 0)
            {
                data.UnitWiseProgress.Add(new UnitProgress
                {
                    UnitName = "All Units",
                    Total = data.TotalContent,
                    Completed = data.CompletedContent,
                    Percentage = data.CompletionPercentage
                });
            }

            return data;
        }
    }
}
