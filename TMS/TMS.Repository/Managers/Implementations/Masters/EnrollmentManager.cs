using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Common;
using TMS.Models.Academics;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Models.Training;
using TMS.Repository.Repositories;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers.Implementations.Masters
{
    /// <summary>
    /// Enrollment manager with status-based lifecycle, duplicate prevention,
    /// and bulk enrollment support. Replaces the old delete-and-reinsert pattern.
    /// </summary>
    internal class EnrollmentManager : MasterBaseManager<CourseEnrollmentViewModel, CourseEnrollment>, IEnrollmentManager
    {
        private readonly IBaseModelRepository<UserMaster> _userRepository;
        private readonly IBaseModelRepository<RoleMaster> _roleRepository;
        private readonly IBaseModelRepository<CourseMaster> _courseRepository;

        public EnrollmentManager(
            IBaseModelRepository<CourseEnrollment> repository,
            AutoMapper.IMapper mapper,
            IBaseModelRepository<QuizMaster> quizRepository,
            IBaseModelRepository<StudentQuizAttempt> studentQuizAttemptRepository,
            IBaseModelRepository<StudentQuizAnswer> studentQuizAnswerRepository,
            IBaseModelRepository<FacultyQuizAssessment> facultyQuizAssessmentRepository,
            IBaseModelRepository<UserMaster> userRepository,
            IBaseModelRepository<RoleMaster> roleRepository,
            IBaseModelRepository<CourseMaster> courseRepository)
            : base(repository, mapper, quizRepository, studentQuizAttemptRepository, studentQuizAnswerRepository, facultyQuizAssessmentRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _courseRepository = courseRepository;
        }

        /// <summary>
        /// Smart enroll: checks for existing enrollment first.
        /// If found (any status) → reactivate. If not → insert new.
        /// </summary>
        public async Task<EnrollmentResult> EnrollStudentAsync(int courseId, int studentId, int userId, string source = "Admin")
        {
            try
            {
                if (courseId <= 0) return new EnrollmentResult { Success = false, Message = "Invalid Course ID." };

                // Check if enrollment already exists (any status)
                var query = _repository.GetAsync(null);
                var existing = await query
                    .Where(e => e.CourseId == courseId && e.StudentId == studentId)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    // Already active? No action needed
                    if (existing.Status == (int)EnrollmentStatus.Active)
                    {
                        return new EnrollmentResult
                        {
                            Success = true,
                            Message = "Student is already enrolled.",
                            WasReactivated = false
                        };
                    }

                    // Reactivate: set status back to Active
                    existing.Status = (int)EnrollmentStatus.Active;
                    existing.EnrolledOn = DateTime.UtcNow;
                    existing.ApprovedOn = DateTime.UtcNow;
                    existing.DroppedOn = null;
                    existing.CompletedOn = null;
                    existing.Source = source;
                    existing.IsActive = true;
                    existing.UpdatedOn = DateTime.Now;
                    existing.UpdatedBy = userId;

                    await _repository.UpdateAsync(existing);
                    await _repository.SaveChangesAsync();

                    return new EnrollmentResult
                    {
                        Success = true,
                        Message = "Student re-enrolled successfully.",
                        WasReactivated = true
                    };
                }

                // New enrollment
                var enrollment = new CourseEnrollment
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    EnrolledOn = DateTime.UtcNow,
                    Status = (int)EnrollmentStatus.Active,
                    ApprovedOn = DateTime.UtcNow,
                    Source = source,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };

                await _repository.AddAsync(enrollment);
                await _repository.SaveChangesAsync();

                return new EnrollmentResult
                {
                    Success = true,
                    Message = "Student enrolled successfully.",
                    WasReactivated = false
                };
            }
            catch (Exception ex)
            {
                return new EnrollmentResult
                {
                    Success = false,
                    Message = $"Enrollment failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Soft-drop: sets status to Dropped + DroppedOn timestamp.
        /// Record is preserved for history/analytics.
        /// </summary>
        public async Task<bool> DropStudentAsync(int courseId, int studentId, int userId)
        {
            try
            {
                var query = _repository.GetAsync(null);
                var existing = await query
                    .Where(e => e.CourseId == courseId && e.StudentId == studentId && e.Status == (int)EnrollmentStatus.Active)
                    .FirstOrDefaultAsync();

                if (existing == null)
                    return false;

                existing.Status = (int)EnrollmentStatus.Dropped;
                existing.DroppedOn = DateTime.UtcNow;
                existing.IsActive = false;
                existing.UpdatedOn = DateTime.Now;
                existing.UpdatedBy = userId;

                await _repository.UpdateAsync(existing);
                await _repository.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Mark enrollment as completed.
        /// </summary>
        public async Task<bool> CompleteEnrollmentAsync(int courseId, int studentId, int userId)
        {
            try
            {
                var query = _repository.GetAsync(null);
                var existing = await query
                    .Where(e => e.CourseId == courseId && e.StudentId == studentId && e.Status == (int)EnrollmentStatus.Active)
                    .FirstOrDefaultAsync();

                if (existing == null)
                    return false;

                existing.Status = (int)EnrollmentStatus.Completed;
                existing.CompletedOn = DateTime.UtcNow;
                existing.UpdatedOn = DateTime.Now;
                existing.UpdatedBy = userId;

                await _repository.UpdateAsync(existing);
                await _repository.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Smart save mapping: syncs selected students with course enrollment.
        /// - Students in list but not enrolled → enroll
        /// - Students enrolled but not in list → drop (soft delete)
        /// - Students already active in list → no change
        /// </summary>
        public async Task<BulkEnrollmentResult> SaveMappingAsync(int courseId, List<int> selectedStudentIds, int userId, string source = "Admin")
        {
            var result = new BulkEnrollmentResult();

            try
            {
                // Get all enrollments for this course (any status)
                var query = _repository.GetAsync(null);
                var courseEnrollments = await query
                    .Where(e => e.CourseId == courseId)
                    .ToListAsync();

                var selectedSet = new HashSet<int>(selectedStudentIds ?? new List<int>());

                // 1) Drop students that are no longer selected
                foreach (var enrollment in courseEnrollments)
                {
                    if (enrollment.Status == (int)EnrollmentStatus.Active && !selectedSet.Contains(enrollment.StudentId))
                    {
                        enrollment.Status = (int)EnrollmentStatus.Dropped;
                        enrollment.DroppedOn = DateTime.UtcNow;
                        enrollment.IsActive = false;
                        enrollment.UpdatedOn = DateTime.Now;
                        enrollment.UpdatedBy = userId;
                        await _repository.UpdateAsync(enrollment);
                        result.DroppedCount++;
                    }
                }

                // 2) Enroll or reactivate selected students
                foreach (var studentId in selectedSet)
                {
                    var existing = courseEnrollments.FirstOrDefault(e => e.StudentId == studentId);

                    if (existing != null)
                    {
                        if (existing.Status != (int)EnrollmentStatus.Active)
                        {
                            // Reactivate
                            existing.Status = (int)EnrollmentStatus.Active;
                            existing.EnrolledOn = DateTime.UtcNow;
                            existing.ApprovedOn = DateTime.UtcNow;
                            existing.DroppedOn = null;
                            existing.CompletedOn = null;
                            existing.Source = source;
                            existing.IsActive = true;
                            existing.UpdatedOn = DateTime.Now;
                            existing.UpdatedBy = userId;
                            await _repository.UpdateAsync(existing);
                            result.ReactivatedCount++;
                        }
                        // else already active — no change
                    }
                    else
                    {
                        // New enrollment
                        var newEnrollment = new CourseEnrollment
                        {
                            CourseId = courseId,
                            StudentId = studentId,
                            EnrolledOn = DateTime.UtcNow,
                            Status = (int)EnrollmentStatus.Active,
                            ApprovedOn = DateTime.UtcNow,
                            Source = source,
                            IsActive = true,
                            CreatedOn = DateTime.Now,
                            CreatedBy = userId
                        };
                        await _repository.AddAsync(newEnrollment);
                    }

                    result.SuccessCount++;
                    result.TotalProcessed++;
                }

                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"SaveMapping failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Bulk enroll from parsed CSV rows. Each row processed individually
        /// with error tracking.
        /// </summary>
        public async Task<BulkEnrollmentResult> BulkEnrollFromCsvAsync(List<CsvEnrollmentRow> rows, int userId)
        {
            var result = new BulkEnrollmentResult();

            foreach (var row in rows)
            {
                result.TotalProcessed++;
                try
                {
                    var enrollResult = await EnrollStudentAsync(row.CourseId, row.StudentId, userId, "CSV");

                    if (enrollResult.Success)
                    {
                        result.SuccessCount++;
                        if (enrollResult.WasReactivated)
                            result.ReactivatedCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {result.TotalProcessed}: {enrollResult.Message} (Student: {row.StudentName ?? row.StudentId.ToString()}, Course: {row.CourseName ?? row.CourseId.ToString()})");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Row {result.TotalProcessed}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Smart Bulk Enroll: Creates student if doesn't exist (by email) and then enrolls them.
        /// Support for Multi-Course Enrollment if courseId parameter is 0.
        /// </summary>
        public async Task<BulkEnrollmentResult> BulkEnrollWithAutoCreateAsync(int courseId, List<StudentUploadRow> rows, int userId)
        {
            var result = new BulkEnrollmentResult();
            
            // 1) Find Student Role
            var roleQuery = _roleRepository.GetAsync(null);
            var studentRole = await roleQuery.Where(r => r.Name == "Student").FirstOrDefaultAsync();
            if (studentRole == null)
            {
                result.Errors.Add("Critical Error: 'Student' role not found in database.");
                return result;
            }

            // 2) Cache courses for fast lookup if multi-course
            var allCourses = await _courseRepository.GetAsync(null).ToListAsync();

            foreach (var row in rows)
            {
                result.TotalProcessed++;
                try
                {
                    if (string.IsNullOrWhiteSpace(row.Email))
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {result.TotalProcessed}: Email is required.");
                        continue;
                    }

                    // A) Determine target CourseId
                    int targetCourseId = courseId;
                    if (targetCourseId <= 0 && !string.IsNullOrWhiteSpace(row.CourseCode))
                    {
                        var course = allCourses.FirstOrDefault(c => c.CourseCode != null && c.CourseCode.Trim().Equals(row.CourseCode.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (course != null) targetCourseId = course.Id;
                    }

                    if (targetCourseId <= 0)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {result.TotalProcessed}: Course not identified (CourseCode: {row.CourseCode ?? "N/A"}).");
                        continue;
                    }

                    // B) Find or Create Student
                    var userQuery = _userRepository.GetAsync(null);
                    var student = await userQuery.Where(u => u.Email == row.Email.Trim()).FirstOrDefaultAsync();

                    if (student == null)
                    {
                        // Create New Student
                        student = new UserMaster
                        {
                            Name = row.Name ?? row.Email.Split('@')[0],
                            Email = row.Email.Trim(),
                            ContactNo = string.IsNullOrWhiteSpace(row.ContactNo) ? null : row.ContactNo.Trim(),
                            RoleId = studentRole.Id,
                            Password = SecurityUtility.HashPassword("Welcome@123"), // Default Password
                            PasswordLastChanged = DateTime.Now,
                            IsActive = true,
                            CreatedOn = DateTime.Now,
                            CreatedBy = userId
                        };
                        await _userRepository.AddAsync(student);
                        await _userRepository.SaveChangesAsync();
                    }

                    // C) Enroll Student
                    var enrollResult = await EnrollStudentAsync(targetCourseId, student.Id, userId, "Document Upload");

                    if (enrollResult.Success)
                    {
                        result.SuccessCount++;
                        if (enrollResult.WasReactivated)
                            result.ReactivatedCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {result.TotalProcessed}: {enrollResult.Message} (Student: {row.Name})");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Row {result.TotalProcessed}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Count active enrollments for a course.
        /// </summary>
        public async Task<int> GetActiveCountAsync(int courseId)
        {
            var query = _repository.GetAsync(null);
            return await query
                .Where(e => e.CourseId == courseId && e.Status == (int)EnrollmentStatus.Active)
                .CountAsync();
        }
    }
}
