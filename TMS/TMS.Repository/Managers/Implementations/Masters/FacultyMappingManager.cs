using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Models.Masters;
using TMS.Repository.Repositories;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class FacultyMappingManager : MasterBaseManager<CourseFacultyMapViewModel, CourseFacultyMapMaster>, IFacultyMappingManager
    {
        private readonly ApplicationDBContext _context;

        public FacultyMappingManager(
            IBaseModelRepository<CourseFacultyMapMaster> repository,
            AutoMapper.IMapper mapper,
            ApplicationDBContext context)
            : base(repository, mapper, null, null, null, null)
        {
            _context = context;
        }

        /// <summary>
        /// Smart Sync for Faculty-Course Mapping.
        /// Uses direct tracked context entities to avoid detached entity graph collisions.
        /// </summary>
        public async Task<BulkEnrollmentResult> SaveMappingAsync(int courseId, List<int> facultyIds, int userId)
        {
            var result = new BulkEnrollmentResult();

            try
            {
                // Get all existing mappings directly tracked from the DbContext
                var existingMappings = await _context.CourseFacultyMaps
                    .Where(m => m.CourseId == courseId)
                    .ToListAsync();

                var selectedSet = new HashSet<int>(facultyIds ?? new List<int>());

                // 1) Soft-delete (IsActive = false) faculties no longer selected
                foreach (var map in existingMappings)
                {
                    if (map.IsActive && !selectedSet.Contains(map.FacultyId))
                    {
                        map.IsActive = false;
                        map.UpdatedOn = DateTime.Now;
                        map.UpdatedBy = userId > 0 ? userId : null;
                        result.DroppedCount++;
                    }
                }

                // 2) Add or Re-activate selected faculties
                foreach (var facultyId in selectedSet)
                {
                    var existing = existingMappings.FirstOrDefault(m => m.FacultyId == facultyId);

                    if (existing != null)
                    {
                        if (!existing.IsActive)
                        {
                            // Reactivate
                            existing.IsActive = true;
                            existing.UpdatedOn = DateTime.Now;
                            existing.UpdatedBy = userId > 0 ? userId : null;
                            result.ReactivatedCount++;
                        }
                    }
                    else
                    {
                        // New mapping
                        var newMap = new CourseFacultyMapMaster
                        {
                            CourseId = courseId,
                            FacultyId = facultyId,
                            IsActive = true,
                            CreatedOn = DateTime.Now,
                            CreatedBy = userId > 0 ? userId : 1
                        };
                        await _context.CourseFacultyMaps.AddAsync(newMap);
                    }

                    result.SuccessCount++;
                    result.TotalProcessed++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Faculty SaveMapping failed: {ex.Message} {(ex.InnerException != null ? ex.InnerException.Message : string.Empty)}");
            }

            return result;
        }

        /// <summary>
        /// Direct unassign of a single instructor from a course.
        /// </summary>
        public async Task<bool> UnassignFacultyAsync(int courseId, int facultyId, int userId)
        {
            try
            {
                int rows = 0;
                if (userId > 0)
                {
                    rows = await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE CourseFacultyMaps SET IsActive = 0, UpdatedOn = {0}, UpdatedBy = {1} WHERE CourseId = {2} AND FacultyId = {3} AND IsActive = 1",
                        DateTime.Now, userId, courseId, facultyId);
                }
                else
                {
                    rows = await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE CourseFacultyMaps SET IsActive = 0, UpdatedOn = {0} WHERE CourseId = {1} AND FacultyId = {2} AND IsActive = 1",
                        DateTime.Now, courseId, facultyId);
                }

                if (rows > 0) return true;

                // Fallback to tracked EF update
                var existing = await _context.CourseFacultyMaps
                    .Where(m => m.CourseId == courseId && m.FacultyId == facultyId && m.IsActive)
                    .ToListAsync();

                if (!existing.Any()) return true;

                foreach (var map in existing)
                {
                    map.IsActive = false;
                    map.UpdatedOn = DateTime.Now;
                    if (userId > 0) map.UpdatedBy = userId;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Direct unassign of all instructors from a course.
        /// </summary>
        public async Task<int> UnassignAllAsync(int courseId, int userId)
        {
            try
            {
                int rows = 0;
                if (userId > 0)
                {
                    rows = await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE CourseFacultyMaps SET IsActive = 0, UpdatedOn = {0}, UpdatedBy = {1} WHERE CourseId = {2} AND IsActive = 1",
                        DateTime.Now, userId, courseId);
                }
                else
                {
                    rows = await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE CourseFacultyMaps SET IsActive = 0, UpdatedOn = {0} WHERE CourseId = {1} AND IsActive = 1",
                        DateTime.Now, courseId);
                }

                if (rows > 0) return rows;

                // Fallback to tracked EF update
                var existing = await _context.CourseFacultyMaps
                    .Where(m => m.CourseId == courseId && m.IsActive)
                    .ToListAsync();

                if (!existing.Any()) return 0;

                int count = existing.Count;
                foreach (var map in existing)
                {
                    map.IsActive = false;
                    map.UpdatedOn = DateTime.Now;
                    if (userId > 0) map.UpdatedBy = userId;
                }

                await _context.SaveChangesAsync();
                return count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
