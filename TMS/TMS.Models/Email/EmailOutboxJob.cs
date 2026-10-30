using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Email
{
    [Table("EmailOutboxJob")]
    public class EmailOutboxJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        [StringLength(255)]
        public string? RecipientName { get; set; }

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        public string? SafePayloadJson { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Processing", "Sent", "Failed", "Cancelled"

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ScheduledForUtc { get; set; } = DateTime.UtcNow;

        public int AttemptCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 5;

        public DateTime? NextAttemptAtUtc { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public string? LastError { get; set; }

        [StringLength(255)]
        public string? IdempotencyKey { get; set; }

        [StringLength(100)]
        public string? EventType { get; set; }

        [StringLength(100)]
        public string? CorrelationId { get; set; }

        public DateTime? LockedUntilUtc { get; set; }
    }
}
