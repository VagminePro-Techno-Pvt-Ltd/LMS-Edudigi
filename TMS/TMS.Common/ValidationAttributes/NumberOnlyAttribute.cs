using System.ComponentModel.DataAnnotations;

namespace TMS
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class NumberOnlyAttribute : RegularExpressionAttribute
    {
        public NumberOnlyAttribute() : base(@"^[0-9]+$")
        {
            if (!string.IsNullOrWhiteSpace(this.ErrorMessage))
                base.ErrorMessage = this.ErrorMessage;
            else base.ErrorMessage = "Only numbers allowed";
        }
    }
}
