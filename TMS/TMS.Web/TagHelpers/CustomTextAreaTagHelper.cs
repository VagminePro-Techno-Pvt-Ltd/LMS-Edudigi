using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TMS.Web.TagHelpers
{
    [HtmlTargetElement("textarea", Attributes = ForAttributeName)]
    public class CustomTextAreaTagHelper : TextAreaTagHelper
    {
        private const string ForAttributeName = "asp-for";
        public CustomTextAreaTagHelper(IHtmlGenerator generator) : base(generator)
        {

        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);
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
