using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class LocationMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO]
       
        [RegularExpression(@"^LOC-\d{1,5}$")]
        public string? LocationCode { get; set; }

        [NotMapped]
        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{LocationCode} ({Name})";
            }
        }
    }
}
