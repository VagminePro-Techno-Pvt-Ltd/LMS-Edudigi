using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.LearningAssistant
{
    [Table("DocumentChunks")]
    public class DocumentChunk : BaseModel
    {
        [Required]
        public int LectureMaterialId { get; set; }

        [ForeignKey(nameof(LectureMaterialId))]
        public virtual LectureMaterial? LectureMaterial { get; set; }

        public int ChunkIndex { get; set; }

        public int? PageNumber { get; set; }

        [StringLength(300)]
        public string? Heading { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int WordCount { get; set; }
    }
}
