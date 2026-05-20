namespace TMS.Web.Extensions
{
    public static class Extensions
    {
        public static DateTime? ToDateNullable(this string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return null;
            DateTime dt;
            if (DateTime.TryParse(date.Trim(), out dt))
                return dt;
            return null;
        }
        public static decimal? ToDecimalNullable(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            decimal decimalValue;
            if (decimal.TryParse(value.Trim(), out decimalValue))
                return decimalValue;
            return null;
        }
        public static string? ToStringNullable(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }
    }
}
