using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    /// <summary>
    /// ViewModel for editing Interactive PPT.
    /// </summary>
    public class InteractivePPTEditViewModel
    {
        public int Id { get; set; }

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

        // ── File (Optional for Edit) ─────────────────────────────────

        [Display(Name = "Replace PPT File (Optional)")]
        public IFormFile? PPTFile { get; set; }

        // ── Existing File Info ───────────────────────────────────────

        public string? ExistingFilePath { get; set; }
        public string? ExistingFileName { get; set; }
    }
}
