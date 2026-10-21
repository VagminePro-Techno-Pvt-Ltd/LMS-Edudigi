using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    [Table("InstitutionalGradingPolicy")]
    public class InstitutionalGradingPolicy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = "Standard Institutional Grading Policy";

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = "CBCS_10P";

        [Column(TypeName = "decimal(5, 2)")]
        public decimal PassingPercentage { get; set; } = 33.00m;

        [Column(TypeName = "decimal(5, 2)")]
        public decimal CompartmentalMinPercentage { get; set; } = 20.00m;

        public int MaxCompartmentSubjects { get; set; } = 2;

        [Column(TypeName = "decimal(5, 2)")]
        public decimal MaxGraceMarks { get; set; } = 0.00m;

        public bool EnableGraceMarks { get; set; } = false;

        [Required]
        [StringLength(50)]
        public string RoundingMethod { get; set; } = "Standard"; // Standard, Ceiling, Floor

        [Required]
        public string GradeBoundariesJson { get; set; } = default!;

        public bool IsDefault { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int CreatedBy { get; set; } = 0;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}
