using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    [Table(nameof(RankingMaster))]
    public class RankingMaster : BaseMasterModel
    {
        //[StringLength(20)]
        //public string? RankId { get; set; }
    }
}
