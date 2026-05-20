using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Account;
using TMS.Models.Assessments;
using TMS.Models.Masters;

namespace TMS.Repository.Managers.Implementations
{
    public class AssessmentManager : IAssessmentManager
    {
        private readonly ApplicationDBContext _context;

        public AssessmentManager(ApplicationDBContext context)
        {
            _context = context;
        }

        // =========================================================================
        // --- EXAM MASTER MANAGEMENT ---
        // =========================================================================

        public async Task<List<ExamMaster>> GetExamsAsync(int facultyId = 0)
        {
            var query = _context.ExamMasters
                .Include(e => e.Course)
                .Include(e => e.Faculty)
                .AsNoTracking();

            if (facultyId > 0)
            {
                query = query.Where(e => e.FacultyId == facultyId);
            }

            return await query.OrderByDescending(e => e.CreatedOn).ToListAsync();
        }

        public async Task<ExamMaster?> GetExamDetailsAsync(int examId)
        {
            return await _context.ExamMasters
                .Include(e => e.Course)
                .Include(e => e.Faculty)
                .Include(e => e.Sections)
                    .ThenInclude(s => s.QuestionMaps)
                        .ThenInclude(qm => qm.Question)
                            .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.Id == examId);
        }

        public async Task<int> CreateOrUpdateExamAsync(ExamMaster exam, int userId)
        {
            if (exam.Id == 0)
            {
                exam.CreatedBy = userId;
                exam.CreatedOn = DateTime.UtcNow;
                exam.IsActive = true;
                _context.ExamMasters.Add(exam);
            }
            else
            {
                var existing = await _context.ExamMasters.FindAsync(exam.Id);
                if (existing == null) return 0;

                existing.Title = exam.Title;
                existing.Description = exam.Description;
                existing.CourseId = exam.CourseId;
                existing.FacultyId = exam.FacultyId;
                existing.DurationMinutes = exam.DurationMinutes;
                existing.TotalMarks = exam.TotalMarks;
                existing.PassingMarks = exam.PassingMarks;
                existing.StartDate = exam.StartDate;
                existing.EndDate = exam.EndDate;
                existing.AllowedAttempts = exam.AllowedAttempts;
                existing.ShuffleQuestions = exam.ShuffleQuestions;
                existing.ShowResultImmediately = exam.ShowResultImmediately;
                existing.Status = exam.Status;
                existing.UpdatedBy = userId;
                existing.UpdatedOn = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return exam.Id;
        }

        public async Task<bool> UpdateExamStatusAsync(int examId, string status)
        {
            var exam = await _context.ExamMasters.FindAsync(examId);
            if (exam == null) return false;

            exam.Status = status;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddExamSectionAsync(ExamSection section)
        {
            _context.ExamSections.Add(section);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteExamSectionAsync(int sectionId)
        {
            var section = await _context.ExamSections.FindAsync(sectionId);
            if (section == null) return false;

            _context.ExamSections.Remove(section);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> MapQuestionsToExamAsync(int examId, int sectionId, List<int> questionIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove existing mappings for this section
                var existing = await _context.ExamQuestionMaps
                    .Where(qm => qm.ExamId == examId && qm.SectionId == sectionId)
                    .ToListAsync();
                _context.ExamQuestionMaps.RemoveRange(existing);

                int order = 1;
                foreach (var qid in questionIds)
                {
                    _context.ExamQuestionMaps.Add(new ExamQuestionMap
                    {
                        ExamId = examId,
                        SectionId = sectionId,
                        QuestionId = qid,
                        DisplayOrder = order++
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<List<ExamSection>> GetExamSectionsAsync(int examId)
        {
            return await _context.ExamSections
                .Include(s => s.QuestionMaps)
                    .ThenInclude(qm => qm.Question)
                .Where(s => s.ExamId == examId)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
        }

        // =========================================================================
        // --- QUESTION BANK SYSTEM ---
        // =========================================================================

        public async Task<List<QuestionBank>> GetQuestionBankAsync(int courseId = 0, int facultyId = 0)
        {
            var query = _context.QuestionBanks
                .Include(q => q.Course)
                .Include(q => q.Options)
                .AsNoTracking();

            if (courseId > 0)
            {
                query = query.Where(q => q.CourseId == courseId);
            }

            if (facultyId > 0)
            {
                query = query.Where(q => q.FacultyId == facultyId);
            }

            return await query.OrderByDescending(q => q.CreatedOn).ToListAsync();
        }

        public async Task<QuestionBank?> GetQuestionDetailsAsync(int questionId)
        {
            return await _context.QuestionBanks
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public async Task<int> CreateOrUpdateQuestionAsync(QuestionBank question, List<QuestionOption> options, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (question.Id == 0)
                {
                    question.CreatedBy = userId;
                    question.CreatedOn = DateTime.UtcNow;
                    question.IsActive = true;
                    _context.QuestionBanks.Add(question);
                    await _context.SaveChangesAsync(); // Generates question.Id

                    foreach (var opt in options)
                    {
                        opt.QuestionId = question.Id;
                        _context.QuestionOptions.Add(opt);
                    }
                }
                else
                {
                    var existing = await _context.QuestionBanks.FindAsync(question.Id);
                    if (existing == null) return 0;

                    existing.QuestionText = question.QuestionText;
                    existing.QuestionType = question.QuestionType;
                    existing.DifficultyLevel = question.DifficultyLevel;
                    existing.Marks = question.Marks;
                    existing.NegativeMarks = question.NegativeMarks;
                    existing.Explanation = question.Explanation;
                    existing.CourseId = question.CourseId;
                    existing.FacultyId = question.FacultyId;
                    existing.UpdatedBy = userId;
                    existing.UpdatedOn = DateTime.UtcNow;

                    // Recreate options
                    var existingOpts = await _context.QuestionOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
                    _context.QuestionOptions.RemoveRange(existingOpts);

                    foreach (var opt in options)
                    {
                        opt.QuestionId = question.Id;
                        opt.Id = 0; // Clear so EF generates new PK
                        _context.QuestionOptions.Add(opt);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return question.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                return 0;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            var question = await _context.QuestionBanks.FindAsync(questionId);
            if (question == null) return false;

            _context.QuestionBanks.Remove(question);
            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================================================
        // --- EXAM ASSIGNMENT SYSTEM ---
        // =========================================================================

        public async Task<bool> AssignExamToStudentsAsync(int examId, List<int> studentIds, int assignedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var sid in studentIds)
                {
                    // Check if already assigned
                    var exists = await _context.ExamAssignments
                        .AnyAsync(a => a.ExamId == examId && a.StudentId == sid);

                    if (!exists)
                    {
                        _context.ExamAssignments.Add(new ExamAssignment
                        {
                            ExamId = examId,
                            StudentId = sid,
                            AssignedBy = assignedByUserId,
                            AssignedOn = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> AssignExamToCourseEnrollmentsAsync(int examId, int courseId, int assignedByUserId)
        {
            var studentIds = await _context.CourseEnrollments
                .Where(e => e.CourseId == courseId && e.IsActive)
                .Select(e => e.StudentId)
                .ToListAsync();

            return await AssignExamToStudentsAsync(examId, studentIds, assignedByUserId);
        }

        public async Task<List<ExamAssignment>> GetAssignmentsForExamAsync(int examId)
        {
            return await _context.ExamAssignments
                .Include(a => a.Student)
                .Include(a => a.Assigner)
                .Where(a => a.ExamId == examId)
                .ToListAsync();
        }

        // =========================================================================
        // --- STUDENT EXAM ENGINE ---
        // =========================================================================

        public async Task<List<ExamMaster>> GetAssignedExamsForStudentAsync(int studentId)
        {
            var examIds = await _context.ExamAssignments
                .Where(a => a.StudentId == studentId)
                .Select(a => a.ExamId)
                .Distinct()
                .ToListAsync();

            return await _context.ExamMasters
                .Include(e => e.Course)
                .Include(e => e.Faculty)
                .Where(e => examIds.Contains(e.Id) && e.Status == "Active" && e.StartDate <= DateTime.UtcNow && e.EndDate >= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<StudentExamAttempt?> StartExamAttemptAsync(int examId, int studentId)
        {
            var exam = await _context.ExamMasters.FindAsync(examId);
            if (exam == null || exam.Status != "Active") return null;

            // Check if already assigned
            var assigned = await _context.ExamAssignments.AnyAsync(a => a.ExamId == examId && a.StudentId == studentId);
            if (!assigned) return null;

            // Check attempts count
            var attemptCount = await _context.StudentExamAttempts
                .CountAsync(a => a.ExamId == examId && a.StudentId == studentId);

            if (attemptCount >= exam.AllowedAttempts) return null;

            // Check if there is an active unsubmitted attempt
            var activeAttempt = await _context.StudentExamAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.ExamId == examId && a.StudentId == studentId && !a.IsSubmitted);

            if (activeAttempt != null)
            {
                // Resume previous attempt, check duration window
                var secondsElapsed = (DateTime.UtcNow - activeAttempt.StartedAt).TotalSeconds;
                var totalSeconds = exam.DurationMinutes * 60;
                var remaining = totalSeconds - secondsElapsed;

                if (remaining <= 0)
                {
                    // Past deadline! Submit it
                    activeAttempt.RemainingSeconds = 0;
                    activeAttempt.IsSubmitted = true;
                    activeAttempt.SubmittedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await SubmitExamAttemptAsync(activeAttempt.Id, true);
                    return null;
                }

                activeAttempt.RemainingSeconds = (int)remaining;
                await _context.SaveChangesAsync();
                return activeAttempt;
            }

            // Create new attempt
            var newAttempt = new StudentExamAttempt
            {
                ExamId = examId,
                StudentId = studentId,
                AttemptNumber = attemptCount + 1,
                StartedAt = DateTime.UtcNow,
                RemainingSeconds = exam.DurationMinutes * 60,
                IsSubmitted = false
            };

            _context.StudentExamAttempts.Add(newAttempt);
            await _context.SaveChangesAsync();
            return newAttempt;
        }

        public async Task<bool> SaveStudentAnswerAsync(int attemptId, int questionId, int? selectedOptionId, string? subjectiveAnswer, int remainingSeconds)
        {
            var attempt = await _context.StudentExamAttempts.FindAsync(attemptId);
            if (attempt == null || attempt.IsSubmitted) return false;

            // Enforce server-side deadline
            var timeLimit = attempt.StartedAt.AddSeconds((attempt.RemainingSeconds + 60)); // 60s grace buffer
            if (DateTime.UtcNow > timeLimit && remainingSeconds > 0)
            {
                attempt.IsSubmitted = true;
                attempt.SubmittedAt = DateTime.UtcNow;
                attempt.RemainingSeconds = 0;
                await _context.SaveChangesAsync();
                await SubmitExamAttemptAsync(attemptId, true);
                return false;
            }

            var answer = await _context.StudentExamAnswers
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId);

            if (answer == null)
            {
                answer = new StudentExamAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = questionId,
                    SelectedOptionId = selectedOptionId,
                    SubjectiveAnswer = subjectiveAnswer
                };
                _context.StudentExamAnswers.Add(answer);
            }
            else
            {
                answer.SelectedOptionId = selectedOptionId;
                answer.SubjectiveAnswer = subjectiveAnswer;
            }

            attempt.RemainingSeconds = remainingSeconds;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<ExamResult?> SubmitExamAttemptAsync(int attemptId, bool forceSubmit = false)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var attempt = await _context.StudentExamAttempts
                    .Include(a => a.Exam)
                    .Include(a => a.Answers)
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt == null || (attempt.IsSubmitted && !forceSubmit)) return null;

                attempt.IsSubmitted = true;
                attempt.SubmittedAt = DateTime.UtcNow;
                attempt.RemainingSeconds = 0;

                // Load all mapped exam questions to verify answers
                var examQuestions = await _context.ExamQuestionMaps
                    .Include(qm => qm.Question)
                        .ThenInclude(q => q.Options)
                    .Where(qm => qm.ExamId == attempt.ExamId)
                    .Select(qm => qm.Question)
                    .ToListAsync();

                int totalQuestions = examQuestions.Count;
                int attemptedQuestions = 0;
                int correctAnswers = 0;
                int wrongAnswers = 0;
                decimal obtainedMarks = 0;
                decimal totalAvailableMarks = attempt.Exam!.TotalMarks;
                bool hasSubjective = false;

                foreach (var q in examQuestions)
                {
                    var ans = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                    if (q.QuestionType == "Subjective")
                    {
                        hasSubjective = true;
                        if (ans != null && !string.IsNullOrWhiteSpace(ans.SubjectiveAnswer))
                        {
                            attemptedQuestions++;
                        }
                        // Subjective scoring requires faculty manual input later
                        continue;
                    }

                    // Auto-grade Objective MCQ / MSQ
                    if (ans == null || ans.SelectedOptionId == null)
                    {
                        // Unattempted MCQ
                        continue;
                    }

                    attemptedQuestions++;
                    var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrect);
                    if (correctOpt != null && ans.SelectedOptionId == correctOpt.Id)
                    {
                        correctAnswers++;
                        ans.IsCorrect = true;
                        ans.ObtainedMarks = q.Marks;
                        obtainedMarks += q.Marks;
                    }
                    else
                    {
                        wrongAnswers++;
                        ans.IsCorrect = false;
                        ans.ObtainedMarks = -q.NegativeMarks;
                        obtainedMarks -= q.NegativeMarks; // Deduct negative marking
                    }
                }

                // Floor marks to zero (cannot have overall negative marks in standard assessments)
                if (obtainedMarks < 0) obtainedMarks = 0;

                attempt.Score = obtainedMarks;
                attempt.Percentage = totalAvailableMarks > 0 ? (obtainedMarks / totalAvailableMarks) * 100 : 0;
                attempt.ResultStatus = hasSubjective ? "Pending" : (attempt.Percentage >= attempt.Exam.PassingMarks ? "Pass" : "Fail");

                // Save or update Result
                var result = await _context.ExamResults.FirstOrDefaultAsync(r => r.AttemptId == attemptId);
                if (result == null)
                {
                    result = new ExamResult
                    {
                        AttemptId = attemptId,
                        TotalQuestions = totalQuestions,
                        AttemptedQuestions = attemptedQuestions,
                        CorrectAnswers = correctAnswers,
                        WrongAnswers = wrongAnswers,
                        ObtainedMarks = obtainedMarks,
                        Percentage = attempt.Percentage ?? 0,
                        Grade = GetGrade(attempt.Percentage ?? 0),
                        ResultStatus = attempt.ResultStatus,
                        PublishedOn = DateTime.UtcNow
                    };
                    _context.ExamResults.Add(result);
                }
                else
                {
                    result.AttemptedQuestions = attemptedQuestions;
                    result.CorrectAnswers = correctAnswers;
                    result.WrongAnswers = wrongAnswers;
                    result.ObtainedMarks = obtainedMarks;
                    result.Percentage = attempt.Percentage ?? 0;
                    result.Grade = GetGrade(attempt.Percentage ?? 0);
                    result.ResultStatus = attempt.ResultStatus;
                    result.PublishedOn = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<StudentExamAttempt?> GetAttemptDetailsAsync(int attemptId)
        {
            return await _context.StudentExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Student)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == attemptId);
        }

        public async Task<ExamResult?> GetExamResultAsync(int attemptId)
        {
            return await _context.ExamResults
                .Include(r => r.Attempt)
                    .ThenInclude(a => a.Exam)
                .Include(r => r.Attempt)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(r => r.AttemptId == attemptId);
        }

        private string GetGrade(decimal percentage)
        {
            if (percentage >= 90) return "A+";
            if (percentage >= 80) return "A";
            if (percentage >= 70) return "B";
            if (percentage >= 60) return "C";
            if (percentage >= 50) return "D";
            return "F";
        }

        // =========================================================================
        // --- FACULTY EVALUATION ---
        // =========================================================================

        public async Task<List<StudentExamAttempt>> GetAttemptsForEvaluationAsync(int facultyId)
        {
            var examIds = await _context.ExamMasters
                .Where(e => e.FacultyId == facultyId)
                .Select(e => e.Id)
                .ToListAsync();

            return await _context.StudentExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Student)
                .Where(a => examIds.Contains(a.ExamId) && a.IsSubmitted)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();
        }

        public async Task<bool> SaveFacultyEvaluationAsync(int attemptId, int facultyId, string? feedback, decimal totalMarksAwarded, Dictionary<int, decimal> questionMarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var attempt = await _context.StudentExamAttempts
                    .Include(a => a.Answers)
                    .FirstOrDefaultAsync(a => a.Id == attemptId);
                if (attempt == null) return false;

                // 1. Update individual answers marks
                decimal objectiveMarks = 0;
                foreach (var ans in attempt.Answers)
                {
                    if (questionMarks.ContainsKey(ans.QuestionId))
                    {
                        ans.ObtainedMarks = questionMarks[ans.QuestionId];
                        ans.IsCorrect = ans.ObtainedMarks > 0; // Conceptual boolean
                    }
                    else
                    {
                        objectiveMarks += ans.ObtainedMarks ?? 0;
                    }
                }

                // 2. Sum subjective marks with existing objective scores
                decimal finalMarks = objectiveMarks + totalMarksAwarded;
                var exam = await _context.ExamMasters.FindAsync(attempt.ExamId);
                decimal totalAvailable = exam?.TotalMarks ?? 100;

                attempt.Score = finalMarks;
                attempt.Percentage = (finalMarks / totalAvailable) * 100;
                attempt.ResultStatus = attempt.Percentage >= (exam?.PassingMarks ?? 50) ? "Pass" : "Fail";

                // 3. Save Faculty Evaluation
                var evaluation = await _context.FacultyEvaluations
                    .FirstOrDefaultAsync(e => e.AttemptId == attemptId);

                if (evaluation == null)
                {
                    evaluation = new FacultyEvaluation
                    {
                        AttemptId = attemptId,
                        FacultyId = facultyId,
                        Feedback = feedback,
                        EvaluatedOn = DateTime.UtcNow,
                        TotalMarksAwarded = totalMarksAwarded
                    };
                    _context.FacultyEvaluations.Add(evaluation);
                }
                else
                {
                    evaluation.Feedback = feedback;
                    evaluation.TotalMarksAwarded = totalMarksAwarded;
                    evaluation.EvaluatedOn = DateTime.UtcNow;
                }

                // 4. Update Exam Result
                var result = await _context.ExamResults.FirstOrDefaultAsync(r => r.AttemptId == attemptId);
                if (result != null)
                {
                    result.ObtainedMarks = finalMarks;
                    result.Percentage = attempt.Percentage ?? 0;
                    result.Grade = GetGrade(attempt.Percentage ?? 0);
                    result.ResultStatus = attempt.ResultStatus;
                    result.PublishedOn = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // =========================================================================
        // --- ANALYTICS DASHBOARD ---
        // =========================================================================

        public async Task<ExamAnalyticsViewModel> GetExamAnalyticsAsync(int examId)
        {
            var exam = await _context.ExamMasters.FindAsync(examId);
            if (exam == null) return new ExamAnalyticsViewModel();

            var assignments = await _context.ExamAssignments
                .Include(a => a.Student)
                .Where(a => a.ExamId == examId)
                .ToListAsync();

            var attempts = await _context.StudentExamAttempts
                .Include(a => a.Student)
                .Where(a => a.ExamId == examId && a.IsSubmitted)
                .ToListAsync();

            int assignedCount = assignments.Count;
            int attemptsCount = attempts.Count;
            double attendance = assignedCount > 0 ? ((double)attemptsCount / assignedCount) * 100 : 0;

            decimal averageScore = attemptsCount > 0 ? attempts.Average(a => a.Score ?? 0) : 0;
            decimal highestScore = attemptsCount > 0 ? attempts.Max(a => a.Score ?? 0) : 0;

            int passed = attempts.Count(a => a.ResultStatus == "Pass");
            int failed = attempts.Count(a => a.ResultStatus == "Fail");
            double passRatio = attemptsCount > 0 ? ((double)passed / attemptsCount) * 100 : 0;

            // Fetch top performers
            var topPerformers = attempts
                .OrderByDescending(a => a.Score)
                .Take(5)
                .Select(a => new StudentAttemptSummary
                {
                    StudentId = a.StudentId,
                    StudentName = a.Student?.Name ?? "Student",
                    Email = a.Student?.Email ?? "",
                    Score = a.Score ?? 0,
                    Percentage = a.Percentage ?? 0,
                    ResultStatus = a.ResultStatus ?? "Pending"
                })
                .ToList();

            // Fetch question statistics
            var questionMaps = await _context.ExamQuestionMaps
                .Include(qm => qm.Question)
                .Where(qm => qm.ExamId == examId)
                .ToListAsync();

            var questionAnalysis = new List<QuestionDifficultySummary>();
            foreach (var qm in questionMaps)
            {
                var answers = await _context.StudentExamAnswers
                    .Where(a => a.AttemptId != 0 && a.QuestionId == qm.QuestionId)
                    .ToListAsync();

                int totalAns = answers.Count;
                int correctAns = answers.Count(a => a.IsCorrect == true);
                double correctRatio = totalAns > 0 ? ((double)correctAns / totalAns) * 100 : 0;

                string difficulty = "Easy";
                if (correctRatio < 40) difficulty = "Hard";
                else if (correctRatio < 75) difficulty = "Medium";

                questionAnalysis.Add(new QuestionDifficultySummary
                {
                    QuestionId = qm.QuestionId,
                    QuestionText = qm.Question?.QuestionText ?? "",
                    QuestionType = qm.Question?.QuestionType ?? "",
                    CorrectResponsePercentage = Math.Round(correctRatio, 2),
                    DeducedDifficulty = difficulty
                });
            }

            return new ExamAnalyticsViewModel
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                TotalAssigned = assignedCount,
                TotalAttempts = attemptsCount,
                AttendancePercentage = Math.Round(attendance, 2),
                AverageScore = Math.Round(averageScore, 2),
                HighestScore = Math.Round(highestScore, 2),
                PassedCount = passed,
                FailedCount = failed,
                PassPercentage = Math.Round(passRatio, 2),
                TopPerformers = topPerformers,
                QuestionAnalysis = questionAnalysis
            };
        }

        public async Task<List<StudentExamAttempt>> GetLiveMonitoringAttemptsAsync(int examId)
        {
            return await _context.StudentExamAttempts
                .Include(a => a.Student)
                .Where(a => a.ExamId == examId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }
    }
}
