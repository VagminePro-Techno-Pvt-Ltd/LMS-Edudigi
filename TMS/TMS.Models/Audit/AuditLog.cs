using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Audit
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [StringLength(150)]
        public string? Username { get; set; }

        [StringLength(50)]
        public string? UserRole { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityType { get; set; }

        [StringLength(100)]
        public string? EntityId { get; set; }

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

        public string? ChangedFieldsJson { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "SUCCESS";

        [StringLength(100)]
        public string? CorrelationId { get; set; }

        public string? Details { get; set; }
    }
}

