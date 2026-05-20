using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TMS.Models.Academics;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Repository.Extensions;

using TMS.Repository.Repositories;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using AutoMapper;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class DailyAttendanceManager : MasterBaseManager<DailyAttendanceViewModel, DailyAttendance>, IDailyAttendanceManager
    {
        private readonly IRepository<UserMaster> _userRepository;

        public DailyAttendanceManager(
            IBaseModelRepository<DailyAttendance> repository, 
            IMapper mapper, 
            IBaseModelRepository<QuizMaster> quizRepository, 
            IBaseModelRepository<StudentQuizAttempt> studentQuizAttemptRepository, 
            IBaseModelRepository<StudentQuizAnswer> studentQuizAnswerRepository, 
            IBaseModelRepository<FacultyQuizAssessment> facultyQuizAssessmentRepository,
            IRepository<UserMaster> userRepository) 
            : base(repository, mapper, quizRepository, studentQuizAttemptRepository, studentQuizAnswerRepository, facultyQuizAssessmentRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> MarkLoginAttendance(int studentId)
        {
            var today = DateTime.Today;
            // Check if already marked for today
            var existing = await _repository.Find(t => t.StudentId == studentId && t.Date == today);
            if (existing.Any())
                return true; // Already marked

            var record = new DailyAttendance
            {
                StudentId = studentId,
                Date = today,
                LoginTime = DateTime.Now,
                AttendanceStatus = 1, // Present
                IsActive = true,
                CreatedOn = DateTime.Now,
                CreatedBy = studentId // Marked by self via login
            };

            var result = await _repository.AddAsync(record);
            await _repository.SaveChangesAsync();
            return result;
        }

        public async Task<int> ProcessDailyAbsentees()
        {
            var today = DateTime.Today;
            
            // Get all active students
            var students = await _userRepository.Find(t => t.IsActive && t.Role.Name.ToLower() == "student", new[] { "Role" });
            
            // Get students who logged in today
            var presentIds = (await _repository.Find(t => t.Date == today)).Select(t => t.StudentId).ToList();
            
            var absentees = students.Where(s => !presentIds.Contains(s.Id)).ToList();
            
            foreach (var student in absentees)
            {
                var record = new DailyAttendance
                {
                    StudentId = student.Id,
                    Date = today,
                    AttendanceStatus = 0, // Absent
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = 1 // System/Admin ID
                };
                await _repository.AddAsync(record);
            }
            
            await _repository.SaveChangesAsync();
            return absentees.Count;
        }

        public async Task<List<DailyAttendanceViewModel>> GetAttendanceReport(DateTime date, int? studentId = null)
        {
            var query = _repository.GetAsync(new[] { "Student" }).Where(t => t.Date == date.Date);
            if (studentId.HasValue)
            {
                query = query.Where(t => t.StudentId == studentId.Value);
            }

            var records = await query.ToListAsync();
            return _mapper.Map<List<DailyAttendanceViewModel>>(records);
        }
    }
}
