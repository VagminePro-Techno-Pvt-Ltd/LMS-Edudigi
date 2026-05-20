using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.Web.Controllers;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMS.Web.Areas.Admin.Controllers
{
    public class MastersAjaxController<TModel> : BaseController
        where TModel : BaseMasterViewModel, new()
    {
        protected readonly IMasterManager<TModel> _manager;
        protected readonly string _masterListType;
        protected readonly string _masterType;
        protected readonly string[]? _masterGetInclude;
        protected readonly string[]? _masterEditInclude;
        protected string _globalErrorField = "Name";
        protected string _nameWithStatusField = "NameStatus";
        protected string _nameWithParentField = "NameWithParent";
        public MastersAjaxController(IMasterManager<TModel> manager, string masterListType, string masterType,
            string[]? masterGetInclude = null, string[]? masterEditInclude = null)
        {
            _manager = manager;
            _masterListType = masterListType;
            _masterType = masterType;
            _masterGetInclude = masterGetInclude;
            _masterEditInclude = masterEditInclude;
        }
        public virtual async Task<IActionResult> Index()
        {
            ViewData["Title"] = _masterListType;
            await Task.CompletedTask;
            return View();
        }

        public virtual async Task<IActionResult> List()
        {
            ViewData["Title"] = _masterListType;
            var list = await _manager.GetAsync(_masterGetInclude);
            return PartialView("_ListPartial", list);
        }
        [HttpPost]
        //public async Task<DataTableResponse<TModel>> IndexSearchDataTables()
        //{
        //    var request = JqueryDataTableUtility.GetDataTableRequest(Request);
        //    return await _manager.GetAsync(request, _masterGetInclude, GetFilter(request), GetOrderColumns(request));
        //}

        public async Task<DataTableResponse<TModel>> IndexSearchDataTables()
		{
			var request = JqueryDataTableUtility.GetDataTableRequest(Request);
			return await _manager.GetAsync(request, _masterGetInclude, GetFilter(request), GetOrderColumns(request));
		}

		

		protected virtual Expression<Func<TModel, bool>>? GetFilter(DataTableRequest request)
        {
            return null;
        }
        protected virtual List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            return null;
        }
        protected List<DataTableColumnsOrder>? GetOrderedColumns(string[] columns, DataTableRequest request)
        {
            if (columns.Length == 0 || request.Order == null || request.Order.Length == 0)
                return null;
            List<DataTableColumnsOrder> list = new();
            foreach (var order in request.Order)
            {
                if (order.Column >= 0)
                {
                    list.Add(new() { Column = columns[order.Column], Dir = order.Dir });
                }
            }
  
            return list;
            
        }

        [HttpPost]
        public virtual async Task<IActionResult> Get(string iId)
        {
            ViewData["Title"] = _masterType;
            if (iId == null)
            {
                TModel model = new();
                await SetDropdownViewBag(model);
                return PartialView("Item", model);
            }
            iId = iId.Decrypt(true);
            if (string.IsNullOrWhiteSpace(iId))
                return Json(GetResultModelFail("Record not found"));
            var idInt = Convert.ToInt32(iId);
            var item = await _manager.GetAsync(idInt, _masterEditInclude);
            SetProperties(ref item!);
            await SetDropdownViewBag(item!);
            return PartialView("Item", item);
        }
        [HttpPost]
        public virtual async Task<IActionResult> Item(TModel model)
        {
            RemoveFromModelState();

            ViewData["Title"] = _masterType;
            if (!ModelState.IsValid)
            {
                return Json(GetFirstModelError(ModelState));
            }
            var validationResult = await IsValidModel(model);
            if (!validationResult.Status)
            {
                return Json(validationResult);
            }
            SetEditProperties(ref model);
            var result = await _manager.AddUpdateAsync(model, this.GetUserId());
            if (result)
            {
                return Json(GetResultModelSuccess($"{_masterType} {(model.Id == 0 ? "added" : "updated")} successfully"));
            }
            return Json(GetResultModelFail("Something went worng, please contact support"));
        }

        protected virtual void RemoveFromModelState()
        {
            ModelState.Remove("CreatedOn");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("CreatedByUser");
        }

        /// <summary>
        /// Used to set additional navigational properties
        /// </summary>
        /// <param name="model"></param>
        protected virtual void SetProperties(ref TModel model)
        {
            //do not delete this
            //this is overried in implemented controllers
        }

        /// <summary>
        /// Used to change in existing properties
        /// </summary>
        /// <param name="model"></param>
        protected virtual void SetEditProperties(ref TModel model)
        {
            //do not delete this
            //this is overried in implemented controllers
        }

        protected virtual async Task<AppResultViewModel> IsValidModel(TModel model)
        {
            var retult = !await _manager.IsValidName(model);
            if (retult)
            {
                return GetResultModelFail("Name already in use");
            }
            return GetResultModelSuccess();
        }

        protected virtual async Task SetDropdownViewBag(TModel model)
        {
            await Task.FromResult(true);
        }
        protected virtual IActionResult GetDropdownList(List<SelectListItem> selectList, bool excludeDefault = false)
        {
            ViewBag.ExcludeDefault = excludeDefault;
            return PartialView("~/Views/Shared/_OptionsPartial.cshtml", selectList);
        }
    }
}
