using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.ContentSync
{
    [Table("ContentFolderMapping")]
    public class ContentFolderMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = default!; // "Program", "Semester", "Course", "CourseSubfolder"

        public int EntityId { get; set; }

        public int? ParentEntityId { get; set; }

        [StringLength(50)]
        public string? SubfolderType { get; set; } // "Videos", "PDFs", "Assignments", "Forum_Discussion"

        [Required]
        [StringLength(50)]
        public string StorageProvider { get; set; } = default!; // "LocalDrive", "GoogleDrive"

        [Required]
        [StringLength(500)]
        public string ProviderFolderId { get; set; } = default!;

        [StringLength(500)]
        public string? ProviderParentFolderId { get; set; }

        [Required]
        [StringLength(250)]
        public string FolderName { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // "Active", "Archived", "Error"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
