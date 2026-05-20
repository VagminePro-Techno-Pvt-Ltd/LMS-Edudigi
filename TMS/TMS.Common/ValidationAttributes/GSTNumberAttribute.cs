using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TMS
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class GSTNumberAttribute : RegularExpressionAttribute, IClientModelValidator
    {
        public GSTNumberAttribute() : base(@"[0-9]{2}[A-Z]{3}[ABCFGHLJPTF]{1}[A-Z]{1}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}")
        {
            if (!string.IsNullOrWhiteSpace(this.ErrorMessage))
                base.ErrorMessage = this.ErrorMessage;
            else base.ErrorMessage = "Invalid GST Number";
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(base.ErrorMessage))
                base.ErrorMessage = "Invalid GST Number";
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
