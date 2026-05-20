using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TMS.Web
{
    [HtmlTargetElement("a", Attributes = ForAttributeName)]
    public class AnchorActiveTagHelper : AnchorTagHelper
    {
        private const string ForAttributeName = "asp-controller";

        public AnchorActiveTagHelper(IHtmlGenerator generator) : base(generator)
        {
        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var controller = Convert.ToString(this.ViewContext.RouteData.Values["controller"]);
            var area = Convert.ToString(this.ViewContext.RouteData.Values["area"]);
            var href = output.Attributes.FirstOrDefault(x => x.Name == "href")?.Value.ToString();
            var route = $"/{controller}";
            if(!string.IsNullOrEmpty(area))
                route = $"/{area}/{controller}";
            var existingCssClassValue = output.Attributes.FirstOrDefault(x => x.Name == "class")?.Value.ToString();
            if (controller != null && href != null && route.Equals(href, StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(existingCssClassValue))
                    existingCssClassValue = "active";
                else
                    existingCssClassValue += " active";
                output.Attributes.SetAttribute("class", existingCssClassValue);
            }
            await Task.CompletedTask;
        }
    }
}
