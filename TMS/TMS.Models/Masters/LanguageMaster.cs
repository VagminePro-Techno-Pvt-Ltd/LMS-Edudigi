using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    public class LanguageMaster : BaseMasterModel
    {
        [StringLength(20)]
        public string? CountryCode { get; set; }
        [StringLength(20)]
        public string? LanguageCode { get; set; }
    }
}
