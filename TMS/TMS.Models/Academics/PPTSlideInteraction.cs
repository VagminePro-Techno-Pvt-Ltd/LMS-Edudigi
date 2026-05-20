using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Maps an LMS item (Quiz / Video / Document / Assignment) to a specific slide
    /// of an Interactive PPT. Shown as an action button when the student reaches that slide.
    /// </summary>
    [Table("PPTSlideInteraction")]
    public class PPTSlideInteraction
    {
        [Key]
        public int Id { get; set; }

        /// <summary>FK → InteractivePPT.Id</summary>
        [Required]
        public int PPTId { get; set; }

        [ForeignKey(nameof(PPTId))]
        public virtual InteractivePPT? PPT { get; set; }

        /// <summary>1-based slide number this interaction is attached to.</summary>
        [Required]
        [Range(1, 9999)]
        public int SlideNumber { get; set; }

        /// <summary>Quiz | Video | Document | Assignment</summary>
        [Required, StringLength(50)]
        public string ItemType { get; set; } = default!;

        /// <summary>ID of the linked LMS item (QuizId / VideoId / LectureMaterialId / AssignmentId).</summary>
        [Required]
        public int ItemId { get; set; }

        /// <summary>Label shown on the overlay button, e.g. "Take Quiz 1".</summary>
        [Required, StringLength(150)]
        public string ButtonText { get; set; } = default!;

        /// <summary>Sort order within the same slide.</summary>
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
