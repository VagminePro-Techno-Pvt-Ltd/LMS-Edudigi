using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Account
{
    [Table(nameof(FormMaster))]
    public class FormMaster 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string? Name { get; set; }
        [StringLength(100)]
        public string? Description { get; set; }
        [StringLength(50)]
        public string? Area { get; set; }
        [Required]
        [StringLength(50)]
        public string? Controller { get; set; }
        [Required]
        [StringLength(50)]
        public string? Action { get; set; }
        [StringLength(50)]
        public string? IconClass { get; set; }
        public int Sequence { get; set; }
        public int? MenuId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool ExcludeFromMenu { get; set; }

        [ForeignKey(nameof(MenuId))]
        public virtual MenuMaster? MenuMaster { get; set; }
    }
}
