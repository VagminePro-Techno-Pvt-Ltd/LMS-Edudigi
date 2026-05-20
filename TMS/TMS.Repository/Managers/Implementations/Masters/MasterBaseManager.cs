using Azure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using TMS.Models;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository.Extensions;
using TMS.Repository.Repositories;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class MasterBaseManager<TViewModel, TMasterModel> : IMasterBaseManager<TViewModel>
        where TViewModel : BaseViewModel, new()
        where TMasterModel : BaseModel, new()
    {
        protected readonly IBaseModelRepository<TMasterModel> _repository;
        protected IBaseModelRepository<QuizMaster> _quizRepository;
        protected IBaseModelRepository<StudentQuizAttempt> _studentQuizAttemptRepository;
        private IBaseModelRepository<StudentQuizAnswer> _studentQuizAnswerRepository;
        private IBaseModelRepository<FacultyQuizAssessment> _facultyQuizAssessmentRepository;
        
        protected readonly AutoMapper.IMapper _mapper;
        public MasterBaseManager(IBaseModelRepository<TMasterModel> repository, AutoMapper.IMapper mapper, IBaseModelRepository<QuizMaster> quizRepository, IBaseModelRepository<StudentQuizAttempt> studentQuizAttemptRepository, IBaseModelRepository<StudentQuizAnswer> studentQuizAnswerRepository, IBaseModelRepository<FacultyQuizAssessment> facultyQuizAssessmentRepository)
        {
            _repository = repository;
            _mapper = mapper;
            _quizRepository = quizRepository;
            _studentQuizAttemptRepository = studentQuizAttemptRepository;
            _studentQuizAnswerRepository = studentQuizAnswerRepository;
            _facultyQuizAssessmentRepository = facultyQuizAssessmentRepository;
        }
        public virtual async Task<List<TViewModel>> GetAsync(string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null)
        {
            var query = _repository.GetAsync(includes);
            if (predicate != null)
            {
                var masterExpression = predicate.Convert<TViewModel, TMasterModel>();
                query = query.Where(masterExpression);
            }
            var list = await query.ToListAsync();
            var finalList = _mapper.Map<List<TViewModel>>(list);
            return finalList;
        }
        public virtual async Task<DataTableResponse<TViewModel>> GetallData(DataTableRequest request, string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null, List<DataTableColumnsOrder>? orderColumns = null)
        {
            DataTableResponse<TViewModel> response = new();
            return response;
        }

        public async Task<DataTableResponse<TViewModel>> GetAsync(DataTableRequest request, string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null, List<DataTableColumnsOrder>? orderColumns = null)
        {
            var totalCount = await _repository.CountAsync();
            var query = _repository.GetAsync(includes);
            if (predicate != null)
            {
                var masterExpression = predicate.Convert<TViewModel, TMasterModel>();
                query = query.Where(masterExpression);
            }
            if (orderColumns != null && orderColumns.Any())
            {
                query = query.OrderBy(orderColumns);
            }
            try
            {
                var list = await query.Skip(request.Start).Take(request.Length).ToListAsync();
                var finalList = _mapper.Map<List<TViewModel>>(list);
                var filteredCount = await query.CountAsync();
                DataTableResponse<TViewModel> response = new()
                {
                    Draw = request.Draw,
                    RecordsTotal = totalCount,
                    RecordsFiltered = filteredCount,
                    Data = finalList.ToArray()

                };
                return response;
            }
            catch (Exception ex) { throw ex; }

        }



        public virtual async Task<TViewModel?> GetAsync(int id, string[]? includes = null)
        {
            try
            {
                var model = await _repository.GetAsync(id, includes);
                return _mapper.Map<TViewModel>(model);

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public virtual async Task<bool> AddUpdateRangeAsync(List<TViewModel> model, int userId)
        {
            throw new NotImplementedException();
        }
        public virtual async Task<bool> UpdateNominationStatusAsync(TViewModel model, int userId)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<bool> AddUpdateAsync(TViewModel model, int userId)
        {
            try
            {
                //Add mode
                if (model.Id == 0)
                {
                    var createModel = model.Map<TViewModel, TMasterModel>();
                    createModel.IsActive = true;
                    createModel.CreatedOn = DateTime.Now;
                    createModel.CreatedBy = userId;
                    var result = await _repository.AddAsync(createModel);
                    if (!result)
                        return false;
                }
                else
                {  //Edit mode
                    var oldModel = await _repository.GetAsync(model.Id);
                    var updatedModel = model.MapToDTO(oldModel!);
                    updatedModel.UpdatedOn = DateTime.Now;
                    updatedModel.UpdatedBy = userId;
                    var editResult = await _repository.UpdateAsync(updatedModel);
                    if (!editResult)
                        return false;
                }
                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public virtual async Task<bool> UpdateMyStatusAsync(TViewModel model, int userId)     // for status virtual function 
        {
            //Add mode
            if (model.Id == 0)
            {
                var createModel = model.Map<TViewModel, TMasterModel>();
                createModel.IsActive = true;
                createModel.CreatedOn = DateTime.Now;
                createModel.CreatedBy = userId;
                var result = await _repository.AddAsync(createModel);
                if (!result)
                    return false;
            }
            else
            {  //Edit mode
                var oldModel = await _repository.GetAsync(model.Id);
                var updatedModel = model.MapToDTO(oldModel!);
                updatedModel.UpdatedOn = DateTime.Now;
                updatedModel.UpdatedBy = userId;
                var editResult = await _repository.UpdateAsync(updatedModel);
                if (!editResult)
                    return false;
            }
            await _repository.SaveChangesAsync();
            return true;
        }


        //      public virtual async Task<bool> AddUpdateNomination(TViewModel model, int userId)
        //      {
        //	if (model.Id == 0)
        //	{
        //		var createModel = model.Map<TViewModel, TMasterModel>();
        //		createModel.IsActive = true;
        //		createModel.CreatedOn = DateTime.Now;
        //		createModel.CreatedBy = userId;
        //		var result = await _repository.AddAsync(createModel);
        //		if (!result)
        //			return false;
        //	}
        //	else
        //	{  //Edit mode
        //		var oldModel = await _repository.GetAsync(model.Id);
        //		var updatedModel = model.MapToDTO(oldModel!);
        //		updatedModel.UpdatedOn = DateTime.Now;
        //		updatedModel.UpdatedBy = userId;
        //		var editResult = await _repository.UpdateAsync(updatedModel);
        //		if (!editResult)
        //			return false;
        //	}
        //	await _repository.SaveChangesAsync();
        //	return true;
        //}
        public virtual async Task<bool> AddUpdateAsync(List<TViewModel> model, int userId)
        {
            foreach (var modelItem in model)
            {
                //Add mode
                if (modelItem.Id == 0)
                {
                    var createModel = modelItem.Map<TViewModel, TMasterModel>();
                    createModel.IsActive = true;
                    createModel.CreatedOn = DateTime.Now;
                    createModel.CreatedBy = userId;
                    var result = await _repository.AddAsync(createModel);
                    if (!result)
                        return false;
                }
                else
                {  //Edit mode
                    var oldModel = await _repository.GetAsync(modelItem.Id);
                    var updatedModel = modelItem.MapToDTO(oldModel!);
                    updatedModel.UpdatedOn = DateTime.Now;
                    updatedModel.UpdatedBy = userId;
                    var editResult = await _repository.UpdateAsync(updatedModel);
                    if (!editResult)
                        return false;
                }
            }
            await _repository.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> CheckExpression(Expression<Func<TViewModel, bool>> expression)
        {
            var masterExpression = expression.Convert<TViewModel, TMasterModel>();
            return (await _repository.Find(masterExpression)).Any();
        }

        public virtual async Task<ModelResultViewModel<TViewModel>> AddUpdateResultAsync(TViewModel model, int userId)
        {
            var createModel = model.Map<TViewModel, TMasterModel>();
            //Add mode
            if (model.Id == 0)
            {
                createModel.IsActive = true;
                createModel.CreatedOn = DateTime.Now;
                createModel.CreatedBy = userId;
                var result = await _repository.AddAsync(createModel);
                if (!result)
                    new ModelResultViewModel<TViewModel>() { Result = false };
            }
            else
            {  //Edit mode
                var oldModel = await _repository.GetAsync(model.Id);
                var updatedModel = model.MapToDTO(oldModel!);
                updatedModel.UpdatedOn = DateTime.Now;
                updatedModel.UpdatedBy = userId;
                var editResult = await _repository.UpdateAsync(updatedModel);
                if (!editResult)
                    new ModelResultViewModel<TViewModel>() { Result = false };
            }
            await _repository.SaveChangesAsync();
            if (model.Id == 0)
            {
                model = createModel.Map<TMasterModel, TViewModel>();
            }
            return new ModelResultViewModel<TViewModel>() { Result = true, Model = model }; ;
        }
        public Task<int> CountAsync(Expression<Func<TViewModel, bool>>? predicate = null)
        {
            if (predicate == null)
                return _repository.CountAsync();
            var expression = predicate.Convert<TViewModel, TMasterModel>();
            return _repository.CountAsync(expression);
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var existing = await _repository.GetAsync(id);
                if (existing == null)
                    return false;

                var result = await _repository.DeleteAsync(existing);
                if (!result)
                    return false;

                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        //public async Task<bool> AddUpdateQuizAsync(QuizMasterViewModel model, int userId)
        //{
        //    if (model == null) return false;

        //    // -------------------- Add Mode --------------------
        //    if (model.Id == 0)
        //    {
        //        var quizEntity = model.Map<QuizMasterViewModel, QuizMaster>();
        //        quizEntity.CreatedOn = DateTime.Now;
        //        quizEntity.CreatedBy = userId;
        //        quizEntity.IsActive = true;
        //        quizEntity.Questions = new List<QuizQuestion>();

        //        foreach (var qVm in model.Questions ?? Enumerable.Empty<QuizQuestionViewModel>())
        //        {
        //            var questionEntity = qVm.MapToDTO(new QuizQuestion());
        //            questionEntity.CreatedOn = DateTime.Now;
        //            questionEntity.CreatedBy = userId;
        //            questionEntity.IsActive = true;
        //            questionEntity.Options = new List<QuizOption>();

        //            foreach (var oVm in qVm.Options ?? Enumerable.Empty<QuizOptionViewModel>())
        //            {
        //                var optionEntity = oVm.MapToDTO(new QuizOption());
        //                optionEntity.CreatedOn = DateTime.Now;
        //                optionEntity.CreatedBy = userId;
        //                optionEntity.IsActive = true;
        //                questionEntity.Options.Add(optionEntity);
        //            }

        //            // ❌ Don't assign CorrectOptionId yet — wait until options have IDs
        //            quizEntity.Questions.Add(questionEntity);
        //        }

        //        await _quizRepository.AddAsync(quizEntity);
        //        await _quizRepository.SaveChangesAsync();

        //        // ✅ Now that IDs exist, assign CorrectOptionId safely
        //        foreach (var qVm in model.Questions ?? Enumerable.Empty<QuizQuestionViewModel>())
        //        {
        //            var questionEntity = quizEntity.Questions.FirstOrDefault(q => q.QuestionText == qVm.QuestionText);
        //            if (questionEntity != null && qVm.CorrectOptionId >= 0 && questionEntity.Options.Count > qVm.CorrectOptionId)
        //            {
        //                var correctOption = questionEntity.Options.ElementAt(qVm.CorrectOptionId.Value);
        //                questionEntity.CorrectOptionId = correctOption.Id;
        //            }
        //        }

        //        await _quizRepository.SaveChangesAsync();
        //    }

        //    // -------------------- Edit Mode --------------------
        //    else
        //    {
        //        var oldQuiz = await _quizRepository.GetAsync(model.Id, new[] { "Questions.Options" });
        //        if (oldQuiz == null) return false;

        //        // Update quiz master
        //        oldQuiz.Title = model.Title;
        //        oldQuiz.CourseId = model.CourseId;
        //        oldQuiz.CourseQuadrantId = model.CourseQuadrantId;
        //        oldQuiz.UpdatedOn = DateTime.Now;
        //        oldQuiz.UpdatedBy = userId;
        //        oldQuiz.IsActive = true;

        //        if (oldQuiz.Questions == null) oldQuiz.Questions = new List<QuizQuestion>();

        //        foreach (var qVm in model.Questions ?? Enumerable.Empty<QuizQuestionViewModel>())
        //        {
        //            QuizQuestion questionEntity;

        //            if (qVm.Id > 0)
        //            {
        //                // Existing question
        //                questionEntity = oldQuiz.Questions.FirstOrDefault(q => q.Id == qVm.Id);
        //                if (questionEntity != null)
        //                {
        //                    questionEntity.QuestionText = qVm.QuestionText;
        //                    questionEntity.QuestionType = qVm.QuestionType;
        //                    questionEntity.UpdatedOn = DateTime.Now;
        //                    questionEntity.UpdatedBy = userId;
        //                    questionEntity.IsActive = true;

        //                    // Sync options
        //                    foreach (var oVm in qVm.Options ?? Enumerable.Empty<QuizOptionViewModel>())
        //                    {
        //                        if (oVm.Id > 0)
        //                        {
        //                            var existingOption = questionEntity.Options.FirstOrDefault(o => o.Id == oVm.Id);
        //                            if (existingOption != null)
        //                            {
        //                                existingOption.OptionText = oVm.OptionText;
        //                                existingOption.UpdatedOn = DateTime.Now;
        //                                existingOption.UpdatedBy = userId;
        //                                existingOption.IsActive = true;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            var newOption = oVm.MapToDTO(new QuizOption());
        //                            newOption.CreatedOn = DateTime.Now;
        //                            newOption.CreatedBy = userId;
        //                            newOption.IsActive = true;
        //                            questionEntity.Options.Add(newOption);
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                // New question
        //                questionEntity = qVm.MapToDTO(new QuizQuestion());
        //                questionEntity.CreatedOn = DateTime.Now;
        //                questionEntity.CreatedBy = userId;
        //                questionEntity.IsActive = true;
        //                questionEntity.Options = new List<QuizOption>();

        //                foreach (var oVm in qVm.Options ?? Enumerable.Empty<QuizOptionViewModel>())
        //                {
        //                    var newOption = oVm.MapToDTO(new QuizOption());
        //                    newOption.CreatedOn = DateTime.Now;
        //                    newOption.CreatedBy = userId;
        //                    newOption.IsActive = true;
        //                    questionEntity.Options.Add(newOption);
        //                }

        //                oldQuiz.Questions.Add(questionEntity);
        //            }
        //        }

        //        await _quizRepository.SaveChangesAsync();

        //        // ✅ Reassign CorrectOptionId safely after option IDs exist
        //        foreach (var qVm in model.Questions ?? Enumerable.Empty<QuizQuestionViewModel>())
        //        {
        //            var questionEntity = oldQuiz.Questions.FirstOrDefault(q => q.Id == qVm.Id || q.QuestionText == qVm.QuestionText);
        //            if (questionEntity != null && qVm.CorrectOptionId >= 0 && questionEntity.Options.Count > qVm.CorrectOptionId)
        //            {
        //                var correctOption = questionEntity.Options.ElementAt(qVm.CorrectOptionId.Value);
        //                questionEntity.CorrectOptionId = correctOption.Id;
        //            }
        //        }

        //        await _quizRepository.SaveChangesAsync();
        //    }

        //    return true;
        //}

        public async Task<bool> AddUpdateQuizAsync(QuizMasterViewModel model, int userId)
        {
            if (model == null) return false;

            // -------------------- ADD MODE --------------------
            if (model.Id == 0)
            {
                var quizEntity = model.Map<QuizMasterViewModel, QuizMaster>();
                quizEntity.CreatedOn = DateTime.Now;
                quizEntity.CreatedBy = userId;
                quizEntity.IsActive = true;
                quizEntity.Questions = new List<QuizQuestion>();

                foreach (var qVm in model.Questions ?? Enumerable.Empty<QuizQuestionViewModel>())
                {
                    var questionEntity = qVm.MapToDTO(new QuizQuestion());
                    questionEntity.CreatedOn = DateTime.Now;
                    questionEntity.CreatedBy = userId;
                    questionEntity.IsActive = true;
                    questionEntity.Options = new List<QuizOption>();

                    foreach (var oVm in qVm.Options ?? Enumerable.Empty<QuizOptionViewModel>())
                    {
                        var optionEntity = oVm.MapToDTO(new QuizOption());
                        optionEntity.CreatedOn = DateTime.Now;
                        optionEntity.CreatedBy = userId;
                        optionEntity.IsActive = true;
                        // ✅ Map IsCorrectOption directly
                        optionEntity.IsCorrectOption = oVm.IsCorrectOption ?? false;
                        questionEntity.Options.Add(optionEntity);
                    }

                    quizEntity.Questions.Add(questionEntity);
                }

                await _quizRepository.AddAsync(quizEntity);
                await _quizRepository.SaveChangesAsync();
            }

            // -------------------- EDIT MODE --------------------
            else
            {
                var oldQuiz = await _quizRepository.GetAsync(model.Id, new[] { "Questions.Options" });
                if (oldQuiz == null) return false;

                // ✅ Update quiz master
                oldQuiz.Title = model.Title;
                oldQuiz.CourseId = model.CourseId;
                oldQuiz.CourseQuadrantId = model.CourseQuadrantId;
                oldQuiz.UpdatedOn = DateTime.Now;
                oldQuiz.UpdatedBy = userId;
                oldQuiz.IsActive = true;
                oldQuiz.TotalMarks = model.TotalMarks;
                oldQuiz.PassingMarks = model.PassingMarks;

                if (oldQuiz.Questions == null) oldQuiz.Questions = new List<QuizQuestion>();

                foreach (var qVm in model.Questions ?? Enumerable.Empty<QuizQuestionViewModel>())
                {
                    QuizQuestion questionEntity;

                    if (qVm.Id > 0)
                    {
                        // ✅ Existing question
                        questionEntity = oldQuiz.Questions.FirstOrDefault(q => q.Id == qVm.Id);
                        if (questionEntity != null)
                        {
                            questionEntity.QuestionText = qVm.QuestionText;
                            questionEntity.QuestionType = qVm.QuestionType;
                            questionEntity.UpdatedOn = DateTime.Now;
                            questionEntity.UpdatedBy = userId;
                            questionEntity.MaxMarks = qVm.MaxMarks;
                            questionEntity.IsActive = true;

                            // ✅ Sync options
                            foreach (var oVm in qVm.Options ?? Enumerable.Empty<QuizOptionViewModel>())
                            {
                                if (oVm.Id > 0)
                                {
                                    var existingOption = questionEntity.Options.FirstOrDefault(o => o.Id == oVm.Id);
                                    if (existingOption != null)
                                    {
                                        existingOption.OptionText = oVm.OptionText;
                                        existingOption.IsCorrectOption = oVm.IsCorrectOption ?? false;
                                        existingOption.UpdatedOn = DateTime.Now;
                                        existingOption.UpdatedBy = userId;
                                        existingOption.IsActive = true;
                                    }
                                }
                                else
                                {
                                    var newOption = oVm.MapToDTO(new QuizOption());
                                    newOption.CreatedOn = DateTime.Now;
                                    newOption.CreatedBy = userId;
                                    newOption.IsActive = true;
                                    newOption.IsCorrectOption = oVm.IsCorrectOption ?? false;
                                    questionEntity.Options.Add(newOption);
                                }
                            }

                            // ✅ Remove deleted options
                            var optionIdsFromVM = qVm.Options?.Select(o => o.Id).ToList() ?? new List<int>();
                            var toRemove = questionEntity.Options.Where(o => o.Id > 0 && !optionIdsFromVM.Contains(o.Id)).ToList();
                            foreach (var rem in toRemove)
                                questionEntity.Options.Remove(rem);
                        }
                    }
                    else
                    {
                        // ✅ New question
                        questionEntity = qVm.MapToDTO(new QuizQuestion());
                        questionEntity.CreatedOn = DateTime.Now;
                        questionEntity.CreatedBy = userId;
                        questionEntity.IsActive = true;
                        questionEntity.Options = new List<QuizOption>();

                        foreach (var oVm in qVm.Options ?? Enumerable.Empty<QuizOptionViewModel>())
                        {
                            var newOption = oVm.MapToDTO(new QuizOption());
                            newOption.CreatedOn = DateTime.Now;
                            newOption.CreatedBy = userId;
                            newOption.IsActive = true;
                            newOption.IsCorrectOption = oVm.IsCorrectOption ?? false;
                            questionEntity.Options.Add(newOption);
                        }

                        oldQuiz.Questions.Add(questionEntity);
                    }
                }

                // ✅ Remove deleted questions
                var questionIdsFromVM = model.Questions?.Select(q => q.Id).ToList() ?? new List<int>();
                var questionsToRemove = oldQuiz.Questions.Where(q => q.Id > 0 && !questionIdsFromVM.Contains(q.Id)).ToList();
                foreach (var remQ in questionsToRemove)
                    oldQuiz.Questions.Remove(remQ);

                await _quizRepository.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> AddUpdateStudentQuizAttemptAsync(StudentQuizAttemptViewModel model, int userId)
        {
            if (model == null)
                return false;

            try
            {
                // -------------------- ADD MODE --------------------
                if (model.Id == 0)
                {
                    var attemptEntity = model.Map<StudentQuizAttemptViewModel, StudentQuizAttempt>();

                    attemptEntity.CreatedOn = DateTime.Now;
                    attemptEntity.CreatedBy = userId;
                    attemptEntity.IsActive = true;
                    attemptEntity.AttemptDate = DateTime.Now;
                    attemptEntity.Answers = new List<StudentQuizAnswer>();

                    await _studentQuizAttemptRepository.AddAsync(attemptEntity);
                    await _studentQuizAttemptRepository.SaveChangesAsync(); // ✅ Save first to generate AttemptId

                    int calculatedScore = 0;
                    var quiz = await _quizRepository.GetAsync(attemptEntity.QuizId, new[] { "Questions", "Questions.Options" });

                    // ✅ Now add answers with correct FK
                    foreach (var ansVm in model.Answers ?? Enumerable.Empty<StudentQuizAnswerViewModel>())
                    {
                        var ansEntity = ansVm.MapToDTO(new StudentQuizAnswer());
                        ansEntity.StudentQuizAttemptId = attemptEntity.Id; // ✅ Set FK
                        ansEntity.CreatedOn = DateTime.Now;
                        ansEntity.CreatedBy = userId;
                        ansEntity.IsActive = true;

                        if (quiz != null)
                        {
                            var question = quiz.Questions.FirstOrDefault(q => q.Id == ansVm.QuizQuestionId);
                            if (question != null)
                            {
                                if (question.QuestionType == QuestionType.Objective)
                                {
                                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrectOption == true);
                                    if (correctOption != null && ansVm.SelectedOptionId == correctOption.Id)
                                    {
                                        ansEntity.IsCorrect = true;
                                        calculatedScore++;
                                    }
                                    else
                                    {
                                        ansEntity.IsCorrect = false;
                                    }
                                }
                            }
                        }

                        await _studentQuizAnswerRepository.AddAsync(ansEntity);
                    }

                    await _studentQuizAnswerRepository.SaveChangesAsync();

                    // ✅ Update the final calculated score on attempt
                    attemptEntity.Score = calculatedScore;
                    await _studentQuizAttemptRepository.UpdateAsync(attemptEntity);
                    await _studentQuizAttemptRepository.SaveChangesAsync();
                }
                // -------------------- EDIT MODE --------------------
                else
                {
                    var existingAttempt = await _studentQuizAttemptRepository.GetAsync(
                        model.Id,
                        new[] { "Answers" }
                    );

                    if (existingAttempt == null)
                        return false;

                    existingAttempt.QuizId = model.QuizId;
                    existingAttempt.StudentId = model.StudentId;
                    existingAttempt.IsSubmitted = model.IsSubmitted;
                    existingAttempt.Score = model.Score;
                    existingAttempt.UpdatedOn = DateTime.Now;
                    existingAttempt.UpdatedBy = userId;
                    existingAttempt.CourseId = model.CourseId;
                    existingAttempt.CourseQuadrantId = model.CourseQuadrantId;

                    if (existingAttempt.Answers == null)
                        existingAttempt.Answers = new List<StudentQuizAnswer>();

                    foreach (var ansVm in model.Answers ?? Enumerable.Empty<StudentQuizAnswerViewModel>())
                    {
                        StudentQuizAnswer ansEntity;

                        if (ansVm.Id > 0)
                        {
                            ansEntity = existingAttempt.Answers.FirstOrDefault(a => a.Id == ansVm.Id);
                            if (ansEntity != null)
                            {
                                ansEntity.QuizQuestionId = ansVm.QuizQuestionId;
                                ansEntity.SelectedOptionId = ansVm.SelectedOptionId;
                                ansEntity.WrittenAnswer = ansVm.WrittenAnswer;
                                ansEntity.UpdatedOn = DateTime.Now;
                                ansEntity.UpdatedBy = userId;
                                ansEntity.IsActive = true;
                            }
                        }
                        else
                        {
                            ansEntity = ansVm.MapToDTO(new StudentQuizAnswer());
                            ansEntity.StudentQuizAttemptId = existingAttempt.Id; // ✅ Set FK
                            ansEntity.CreatedOn = DateTime.Now;
                            ansEntity.CreatedBy = userId;
                            ansEntity.IsActive = true;
                            existingAttempt.Answers.Add(ansEntity);
                        }
                    }

                    // Remove deleted answers
                    var answerIdsFromVM = model.Answers?.Select(a => a.Id).ToList() ?? new List<int>();
                    var toRemove = existingAttempt.Answers
                        .Where(a => a.Id > 0 && !answerIdsFromVM.Contains(a.Id))
                        .ToList();

                    foreach (var rem in toRemove)
                        existingAttempt.Answers.Remove(rem);

                    await _studentQuizAttemptRepository.UpdateAsync(existingAttempt);
                    await _studentQuizAttemptRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddUpdateQuizAssessmentAsync(FacultyQuizAssessmentViewModel model, int userId)
        {
            if (model == null)
                return false;

            try
            {
                // -------------------- ADD MODE --------------------
                if (model.Id == 0)
                {
                    var assessmentEntity = model.Map<FacultyQuizAssessmentViewModel, FacultyQuizAssessment>();
                    assessmentEntity.CreatedOn = DateTime.Now;
                    assessmentEntity.CreatedBy = userId;
                    assessmentEntity.IsActive = true;
                    assessmentEntity.Questions = new List<FacultyQuizQuestionAssessment>();

                    foreach (var qVm in model.Questions ?? Enumerable.Empty<FacultyQuizQuestionAssessmentViewModel>())
                    {
                        var questionEntity = qVm.MapToDTO(new FacultyQuizQuestionAssessment());
                        questionEntity.CreatedOn = DateTime.Now;
                        questionEntity.CreatedBy = userId;
                        questionEntity.IsActive = true;

                        assessmentEntity.Questions.Add(questionEntity);
                    }

                    await _facultyQuizAssessmentRepository.AddAsync(assessmentEntity);
                    await _facultyQuizAssessmentRepository.SaveChangesAsync();
                }

                // -------------------- EDIT MODE --------------------
                else
                {
                    var oldAssessment = await _facultyQuizAssessmentRepository.GetAsync(
                        model.Id,
                        new[] { "Questions" }
                    );

                    if (oldAssessment == null)
                        return false;

                    // ✅ Update assessment header
                    oldAssessment.QuizId = model.QuizId;
                    oldAssessment.StudentId = model.StudentId;
                    oldAssessment.CourseId = model.CourseId;
                    oldAssessment.QuadrantId = model.QuadrantId;
                    oldAssessment.TotalScore = model.TotalScore ?? 0;
                    oldAssessment.UpdatedOn = DateTime.Now;
                    oldAssessment.UpdatedBy = userId;
                    oldAssessment.IsActive = true;

                    if (oldAssessment.Questions == null)
                        oldAssessment.Questions = new List<FacultyQuizQuestionAssessment>();

                    // ✅ Update or Add Questions
                    foreach (var qVm in model.Questions ?? Enumerable.Empty<FacultyQuizQuestionAssessmentViewModel>())
                    {
                        FacultyQuizQuestionAssessment questionEntity;

                        if (qVm.Id > 0)
                        {
                            // Existing question
                            questionEntity = oldAssessment.Questions.FirstOrDefault(q => q.Id == qVm.Id);
                            if (questionEntity != null)
                            {
                                questionEntity.QuestionId = qVm.QuestionId;
                                questionEntity.QuestionText = qVm.QuestionText;
                                questionEntity.MaxScore = qVm.MaxScore;
                                questionEntity.StudentScore = qVm.StudentScore;
                                questionEntity.UpdatedOn = DateTime.Now;
                                questionEntity.UpdatedBy = userId;
                                questionEntity.IsActive = true;
                            }
                        }
                        else
                        {
                            // New question
                            questionEntity = qVm.MapToDTO(new FacultyQuizQuestionAssessment());
                            questionEntity.CreatedOn = DateTime.Now;
                            questionEntity.CreatedBy = userId;
                            questionEntity.IsActive = true;
                            oldAssessment.Questions.Add(questionEntity);
                        }
                    }

                    // ✅ Remove deleted questions
                    var questionIdsFromVM = model.Questions?.Select(q => q.Id).ToList() ?? new List<int>();
                    var toRemove = oldAssessment.Questions.Where(q => q.Id > 0 && !questionIdsFromVM.Contains(q.Id)).ToList();
                    foreach (var rem in toRemove)
                        oldAssessment.Questions.Remove(rem);

                    await _facultyQuizAssessmentRepository.SaveChangesAsync();

                    
                }
                var attempt = await _studentQuizAttemptRepository.GetAsync(model.AttemptId);
                if (attempt != null)
                {
                    attempt.Score = Convert.ToInt32(model.TotalScore ?? 0);
                    attempt.UpdatedOn = DateTime.Now;
                    attempt.UpdatedBy = userId;
                    await _studentQuizAttemptRepository.UpdateAsync(attempt);
                    await _studentQuizAttemptRepository.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in AddUpdateQuizAssessmentAsync", ex);
            }
        }







    }
}
