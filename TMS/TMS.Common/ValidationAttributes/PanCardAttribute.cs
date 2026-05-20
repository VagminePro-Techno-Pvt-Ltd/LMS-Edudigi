using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TMS
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class PanCardAttribute : RegularExpressionAttribute, IClientModelValidator
    {
        public PanCardAttribute() : base(@"[A-Z]{5}[0-9]{4}[A-Z]{1}")
        {
            if (!string.IsNullOrWhiteSpace(this.ErrorMessage))
                base.ErrorMessage = this.ErrorMessage;
            else base.ErrorMessage = "Invalid PAN Number";
        }
        public void AddValidation(ClientModelValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(base.ErrorMessage))
                base.ErrorMessage = "Invalid PAN Number";
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-regex", base.ErrorMessage);
            MergeAttribute(context.Attributes, "data-val-regex-pattern", base.Pattern);
            MergeAttribute(context.Attributes, "oninput", "changeToUpperCase(this);");
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }
            attributes.Add(key, value);
            return true;
        }
    }
}
