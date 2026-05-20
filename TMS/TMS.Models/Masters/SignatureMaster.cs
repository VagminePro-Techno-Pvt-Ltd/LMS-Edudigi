using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    [Table(nameof(SignatureMaster))]
    public class SignatureMaster : BaseMasterModel
    {
        #region
        [NotMapped]
        public override string? Description { get; set; }
        #endregion
        [StringLength(100)]
        public string? Title { get; set; }
        [StringLength(100)]
        public string? FileName { get; set; }
        [StringLength(50)]
        public string? FilePath { get; set; }
    }
}
