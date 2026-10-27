using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("ProctoringIncident")]
    public class ProctoringIncident : BaseIdModel
    {
        [Required]
        public int SessionId { get; set; }

        [ForeignKey(nameof(SessionId))]
        public virtual ProctoringSession? Session { get; set; }

        public int? AttemptId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = default!; // TAB_SWITCH, WINDOW_BLUR, FULLSCREEN_EXIT, CAMERA_OFF, MULTIPLE_FACES, NO_FACE, CONNECTION_INTERRUPTED

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

        [Required]
        public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;

        public DateTime? ClientTimestamp { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        public bool RequiresReview { get; set; } = true;

        public bool IsReviewed { get; set; } = false;

        public int? ReviewedBy { get; set; }

        [StringLength(500)]
        public string? ReviewNotes { get; set; }
    }
}
