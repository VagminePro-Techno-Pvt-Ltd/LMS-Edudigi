using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Common
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class DecimalAttribute : PrecisionAttribute
    {
        public DecimalAttribute(byte precision = 18, byte scale = 2) : base(precision, scale)
        {

        }
    }
}
