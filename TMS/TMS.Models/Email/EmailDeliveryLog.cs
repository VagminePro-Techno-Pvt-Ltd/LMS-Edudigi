using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Email
{
    [Table("EmailDeliveryLog")]
    public class EmailDeliveryLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int EmailOutboxJobId { get; set; }

        public int AttemptNumber { get; set; }

        public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Success"; // "Success", "TransientFailure", "PermanentFailure"

        public string? ErrorMessage { get; set; }

        public int DurationMs { get; set; }

        [ForeignKey(nameof(EmailOutboxJobId))]
        public virtual EmailOutboxJob? EmailOutboxJob { get; set; }
    }
}
