using System.ComponentModel.DataAnnotations;

namespace TMS
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class DecimalNumberAttribute : RegularExpressionAttribute
    {
        public DecimalNumberAttribute(int DigitBeforeDecimal = 10, int DecimalPoints=2) : base(@$"^(\d{{1,{DigitBeforeDecimal}}}|\d{{0,{DigitBeforeDecimal}}}\.\d{{1,{DecimalPoints}}})$")
        {
            if (!string.IsNullOrWhiteSpace(this.ErrorMessage))
                base.ErrorMessage = this.ErrorMessage;
            else base.ErrorMessage = $"Up to {DigitBeforeDecimal} digits allowed before decimal and upto {DecimalPoints} digits allowed after decimal";
        }
    }
}
