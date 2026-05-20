using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Interactive PowerPoint presentations uploaded by faculty.
    /// Linked to Course → Unit → Topic hierarchy.
    /// </summary>
    [Table(nameof(InteractivePPT))]
    public class InteractivePPT
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Course (same as Subject in this system).</summary>
        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        /// <summary>Subject mapped by DB schema.</summary>
        [Required]
        public int SubjectId { get; set; }

        /// <summary>Unit within the course.</summary>
        [Required]
        public int UnitId { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual UnitMaster? Unit { get; set; }

        /// <summary>Topic within the unit.</summary>
        public int? TopicId { get; set; }

        [ForeignKey(nameof(TopicId))]
        public virtual TopicLookup? Topic { get; set; }

        /// <summary>PPT title.</summary>
        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        /// <summary>Optional description.</summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>Relative file path: /uploads/ppts/filename.pptx</summary>
        [Required, StringLength(500)]
        public string FilePath { get; set; } = default!;

        /// <summary>Original uploaded filename.</summary>
        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>Faculty user ID who uploaded this PPT.</summary>
        public int? UploadedBy { get; set; }

        /// <summary>Creation timestamp as per DB schema.</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Last update timestamp as per DB schema.</summary>
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
