using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    /// <summary>
    /// ViewModel for the Faculty Interactive PPT Upload page.
    /// Cascade: Program → Course → Unit → Topic
    /// </summary>
    public class InteractivePPTViewModel
    {
        // ── Cascade mapping ─────────────────────────────────────────

        [Required(ErrorMessage = "Please select a Program.")]
        [Display(Name = "Program")]
        public int ProgramId { get; set; }

        [Required(ErrorMessage = "Please select a Course.")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Please select a Unit.")]
        [Display(Name = "Unit")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "Please select a Topic.")]
        [Display(Name = "Topic")]
        public int TopicId { get; set; }

        // ── PPT metadata ─────────────────────────────────────────────

        [Required(ErrorMessage = "PPT Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        [Display(Name = "PPT Title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // ── File ─────────────────────────────────────────────────────

        [Required(ErrorMessage = "Please select a PPT or PPTX file.")]
        [Display(Name = "PPT File (.ppt / .pptx)")]
        public IFormFile? PPTFile { get; set; }
    }
}
