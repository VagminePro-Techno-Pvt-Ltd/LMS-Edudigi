using TMS.Models.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models
{
    public class BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column(Order = 0)]
        public virtual int Id { get; set; }

        [DefaultValue(true)]
        [Column(Order = 10001)]
        public virtual bool IsActive { get; set; }

        [Column(Order = 10002)]
        public int CreatedBy { get; set; }
        [Column(Order = 10003)]
        public DateTime CreatedOn { get; set; }

        [Column(Order = 10004)]
        public virtual int? UpdatedBy { get; set; }
        [Column(Order = 10005)]
        public virtual DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual UserMaster? CreatedByUser { get; set; }
        [ForeignKey(nameof(UpdatedBy))]
        public virtual UserMaster? UpdatedByUser { get; set; }
    }
}
