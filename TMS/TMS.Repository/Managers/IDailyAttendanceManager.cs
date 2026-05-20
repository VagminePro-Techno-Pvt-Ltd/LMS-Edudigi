using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMS.ViewModels.Academics;

namespace TMS.Repository.Managers
{
    public interface IDailyAttendanceManager : IMasterBaseManager<DailyAttendanceViewModel>
    {
        /// <summary>
        /// Marks a student as present for the current day.
        /// To be called upon successful login.
        /// </summary>
        Task<bool> MarkLoginAttendance(int studentId);

        /// <summary>
        /// Identifies all students who haven't logged in today and marks them as absent.
        /// To be called by a scheduled background job.
        /// </summary>
        Task<int> ProcessDailyAbsentees();

        /// <summary>
        /// Fetches attendance records for a specific date or range.
        /// </summary>
        Task<List<DailyAttendanceViewModel>> GetAttendanceReport(DateTime date, int? studentId = null);
    }
}
