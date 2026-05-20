using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.Web.Controllers;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.ViewModels.Masters;

namespace TMS.Web.Areas.Admin.Controllers
{
   // [ValidateFormAccess(Common.FormDefination.Quiz)]
    [Area("Admin")]
    public class QuestionController : MastersAjaxBaseController<QuestionMasterViewModel>
    {
        public QuestionController(IMasterBaseManager<QuestionMasterViewModel> manager)
            : base(manager, "Question", "Question", new[] { "QuestionOptions" }, new[] { "QuestionOptions" })
        {
        }
        protected override Expression<Func<QuestionMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<QuestionMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Question) && t.Question.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Question", "QuestionType", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(QuestionMasterViewModel model)
        {
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                  &&  t.Question == model.Question);
            if (resultIdCheck)
            {
                return GetResultModelFail("Question already exists");
            }
            return GetResultModelSuccess();
        }
        public IActionResult GetQuestionOptions(int questionType)
        {
            QuestionMasterViewModel model = new();
            List<QuestionOptionMasterViewModel> returnModel = new();
            for (int j = 0; j < questionType; j++)
            {
                QuestionOptionMasterViewModel option = new();
                returnModel.Add(option);
            }
            model.QuestionOptions = returnModel;
            return PartialView("_OptionsList", model);
        }
    }
}
