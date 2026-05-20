using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace TMS.Web.Areas.Admin.Controllers
{
    public class MastersBaseController<TModel> : BaseController
        where TModel : BaseViewModel, new()
    {
        protected readonly IMasterBaseManager<TModel> _manager;
        protected readonly string _masterListType;
        protected readonly string _masterType;
        protected readonly string[]? _masterGetInclude;
        protected readonly string[]? _masterEditInclude;
        protected string _globalErrorField = "Name";
        public MastersBaseController(IMasterBaseManager<TModel> manager, string masterListType, string masterType,
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
            var list = await _manager.GetAsync(_masterGetInclude);
            return View(list);
        }

        public virtual async Task<IActionResult> Item(string iId)
        {
            ViewData["Title"] = _masterType;
            if (iId == null)
            {
                TModel model = new();
                await SetDropdownViewBag(model);
                return View(model);
            }
            iId = iId.Decrypt(true);
            if (string.IsNullOrWhiteSpace(iId))
                return NotFound();
            var idInt = Convert.ToInt32(iId);
            var item = await _manager.GetAsync(idInt, _masterEditInclude);
            SetProperties(ref item!);
            await SetDropdownViewBag(item!);
            return View(item);
        }
        [HttpPost]
        public virtual async Task<IActionResult> Item(TModel model)
        {
            RemoveFromModelState();

            ViewData["Title"] = _masterType;
            if (!ModelState.IsValid)
            {
                await SetDropdownViewBag(model);
                return View(model);
            }
            if (await ValidateModel(model))
            {
                await SetDropdownViewBag(model);
                return View(model);
            }
            SetEditProperties(ref model);
            var result = await _manager.AddUpdateAsync(model, this.GetUserId());
            if (result)
            {
                SetApplicationResult(true, $"{_masterType} {(model.Id == 0 ? "added" : "updated")} successfully");
                return Redirect("Index");
            }
            ModelState.AddModelError(_globalErrorField, "Something went worng, please contact support");
            await SetDropdownViewBag(model);
            return View(model);
        }
		//[HttpPost]
		//public virtual async Task<IActionResult> UpdateStatus(TModel model)    // added to perform the action.
		//{
		//	var resukt = await _manager.UpdateMyStatusAsync(model, this.GetUserId());

		//	return View(model);
		//}



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
        protected virtual void SetEditProperties(ref TModel model) {
            //do not delete this
            //this is overried in implemented controllers
        }

        protected virtual async Task<bool> ValidateModel(TModel model)
        {
            //var retult = !await _manager.IsValidName(model);
            //if (retult)
            //    ModelState.AddModelError("Name", "Name already in use");
            //return retult;
            return await Task.FromResult(false);
        }

        protected virtual async Task SetDropdownViewBag(TModel model)
        {
            await Task.FromResult(true);
        }
    }
}
