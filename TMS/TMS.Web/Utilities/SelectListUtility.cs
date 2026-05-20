using TMS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMS.Web
{
    public static class SelectListUtility
    {
        public static List<SelectListItem> GetSelectList(this System.Collections.IEnumerable items, object? selectedValue = null, string dataValueField = nameof(BaseMasterViewModel.Id), string dataTextField = nameof(BaseMasterViewModel.Name), string? optionText = null, bool addOptionText = true)
        {
            string? strSelectedValue = Convert.ToString(selectedValue);
            object? selectedItem = null;
            if (!string.IsNullOrWhiteSpace(strSelectedValue)
                && Int32.TryParse(strSelectedValue, out int selectedValueInt))
            {
                if (selectedValueInt > 0)
                    selectedItem = selectedValueInt;
            }
            else
                selectedItem = selectedValue;
            List<SelectListItem> selectList = new SelectList(items, dataValueField, dataTextField, selectedItem).ToList();
            if (addOptionText)
                selectList.Insert(0, new() { Text = string.IsNullOrWhiteSpace(optionText) ? "--Select--" : optionText, Value = "" });
            return selectList;
        }
        public static List<SelectListItem> GetSelectList(this System.Collections.IEnumerable items, string dataValueField, string dataTextField, bool addOptionText = true)
        {
            List<SelectListItem> selectList = new SelectList(items, dataValueField, dataTextField).ToList();
            if (addOptionText)
                selectList.Insert(0, new() { Text = "--Select--", Value = "" });
            return selectList;
        }
        public static List<SelectListItem> GetStatusList()
        {
            List<SelectListItem> selectList = new();
            selectList.Insert(0, new() { Text = "--Select--", Value = "" });
            selectList.Insert(1, new() { Text = "Completed", Value = "Completed" });
            selectList.Insert(2, new() { Text = "Pending", Value = "Pending" });
            return selectList;
        }
    }
}
