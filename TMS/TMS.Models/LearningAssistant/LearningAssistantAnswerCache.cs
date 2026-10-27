using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.LearningAssistant
{
    [Table("LearningAssistantAnswerCaches")]
    public class LearningAssistantAnswerCache : BaseModel
    {
        [Required, StringLength(128)]
        public string CacheKey { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string NormalizedQuery { get; set; } = string.Empty;

        [StringLength(50)]
        public string Intent { get; set; } = "Tutoring";

        public int? CourseId { get; set; }

        public int? LectureMaterialId { get; set; }

        [StringLength(100)]
        public string? DocumentHash { get; set; }

        [StringLength(50)]
        public string ModelName { get; set; } = "gemini-1.5-flash";

        [StringLength(20)]
        public string Language { get; set; } = "en";

        [Required]
        public string Reply { get; set; } = string.Empty;

        public string? SourcesJson { get; set; }

        public string? SuggestedActionsJson { get; set; }

        public int HitCount { get; set; } = 0;

        public int PromptTokens { get; set; } = 0;

        public int CandidateTokens { get; set; } = 0;

        public int TotalTokens { get; set; } = 0;

        public DateTime ExpiresOn { get; set; } = DateTime.UtcNow.AddHours(24);
    }
}
