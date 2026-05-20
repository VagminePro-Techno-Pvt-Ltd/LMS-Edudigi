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
        public FacultyMappingManager(
            IBaseModelRepository<CourseFacultyMapMaster> repository,
            AutoMapper.IMapper mapper)
            : base(repository, mapper, null, null, null, null)
        {
        }

        /// <summary>
        /// Smart Sync for Faculty-Course Mapping.
        /// No more hard-deletes.
        /// </summary>
        public async Task<BulkEnrollmentResult> SaveMappingAsync(int courseId, List<int> facultyIds, int userId)
        {
            var result = new BulkEnrollmentResult();

            try
            {
                // Get all existing mappings for this course (including inactive ones)
                var query = _repository.GetAsync(null);
                var existingMappings = await query
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
                        map.UpdatedBy = userId;
                        await _repository.UpdateAsync(map);
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
                            existing.UpdatedBy = userId;
                            await _repository.UpdateAsync(existing);
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
                            CreatedBy = userId
                        };
                        await _repository.AddAsync(newMap);
                    }

                    result.SuccessCount++;
                    result.TotalProcessed++;
                }

                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Faculty SaveMapping failed: {ex.Message}");
            }

            return result;
        }
    }
}
