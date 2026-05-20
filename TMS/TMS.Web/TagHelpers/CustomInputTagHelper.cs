using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TMS.Web.TagHelpers
{
    [HtmlTargetElement("input", Attributes = ForAttributeName)]
    public class CustomInputTagHelper : InputTagHelper
    {
        private const string ForAttributeName = "asp-for";
        public CustomInputTagHelper(IHtmlGenerator generator) : base(generator)
        {

        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);
            var modelExplorer = For.ModelExplorer;
            string inputType;
            string? inputTypeHint;
            if (string.IsNullOrEmpty(InputTypeName))
            {
                // Note GetInputType never returns null.
                inputType = GetInputType(modelExplorer, out inputTypeHint);
            }
            else
            {
                inputType = InputTypeName.ToLowerInvariant();
                inputTypeHint = null;
            }
            if (inputType.ToLowerInvariant() == "checkbox" || inputType.ToLowerInvariant() == "radio")
                return;
            var existingCssClassValue = output.Attributes.FirstOrDefault(x => x.Name == "class")?.Value.ToString();
            if (string.IsNullOrWhiteSpace(existingCssClassValue) || !existingCssClassValue.Contains("form-control", StringComparison.InvariantCultureIgnoreCase))
            {
                if (existingCssClassValue == null)
                    existingCssClassValue = "form-control";
                else
                    existingCssClassValue += " form-control";
                output.Attributes.SetAttribute("class", existingCssClassValue);
            }
        }
    }
}
