using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace TMS
{
    public static class Utilitiy
    {
        public static string ToTwoDecimal(this decimal? value)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{0:#,0.00}", value.Value);
        }
        public static string ToTwoDecimal(this decimal value)
        {
            return string.Format("{0:#,0.00}", value);
        }
        public static string ToNoDecimal(this decimal? value)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{0:#,0}", decimal.Round(value.Value, 0, MidpointRounding.ToEven));
        }
        public static string ToNoDecimal(this decimal value)
        {
            return string.Format("{0:#,0}", decimal.Round(value, 0, MidpointRounding.ToEven));
        }

        public static string ToMoney(this decimal? value, string? currencySymbol = null)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{1}{0:#,0.00}", value.Value, GetMoneySymbol(currencySymbol));
        }
        public static string ToMoney(this decimal value, string? currencySymbol = null)
        {
            return string.Format("{1}{0:#,0.00}", value, GetMoneySymbol(currencySymbol));
        }
        public static string ToMoneyNoDecimal(this decimal? value, string? currencySymbol = null)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{1}{0:#,0}", decimal.Round(value.Value, 0, MidpointRounding.ToEven), GetMoneySymbol(currencySymbol));
        }
        public static string ToMoneyNoDecimal(this decimal value, string? currencySymbol = null)
        {
            return string.Format("{1}{0:#,0}", decimal.Round(value, 0, MidpointRounding.ToEven), GetMoneySymbol(currencySymbol));
        }

        public static string ToMoney4(this decimal? value, string? currencySymbol = null)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{1}{0:#,0.0000}", value.Value, GetMoneySymbol(currencySymbol));
        }
        public static string ToMoney4(this decimal value, string? currencySymbol = null)
        {
            return string.Format("{1}{0:#,0.0000}", value, GetMoneySymbol(currencySymbol));
        }       

        public static string ToMoneyUnicode(this decimal? value, string? currencySymbol = null)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{1}{0:#,0.00}", value.Value, GetMoneySymbolUnicode(currencySymbol));
        }
        public static string ToMoneyUnicode(this decimal value, string? currencySymbol = null)
        {
            return string.Format("{1}{0:#,0.00}", value, GetMoneySymbolUnicode(currencySymbol));
        }
        public static string ToMoneyUnicodeNoDecimal(this decimal? value, string? currencySymbol = null)
        {
            if (!value.HasValue)
                return string.Empty;
            return string.Format("{1}{0:#,0}", decimal.Round(value.Value, 0, MidpointRounding.ToEven), GetMoneySymbolUnicode(currencySymbol));
        }
        public static string ToMoneyUnicodeNoDecimal(this decimal value, string? currencySymbol = null)
        {
            return string.Format("{1}{0:#,0}", decimal.Round(value, 0, MidpointRounding.ToEven), GetMoneySymbolUnicode(currencySymbol));
        }

        public static string GetMoneySymbol(string? currencySymbol = null)
        {
            return string.Format("{0}{1}", !string.IsNullOrWhiteSpace(currencySymbol) ? currencySymbol : "₹", "&nbsp;");
        }
        public static string GetMoneySymbolUnicode(string? currencySymbol = null)
        {
            return string.Format("{0}{1}", !string.IsNullOrWhiteSpace(currencySymbol) ? currencySymbol : "&#8377;", "&#x0020;");
        }
        public static string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }
    }
}
