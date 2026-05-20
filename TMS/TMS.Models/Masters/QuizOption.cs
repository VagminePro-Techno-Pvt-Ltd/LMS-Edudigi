using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.Models;
using TMS.Models.Masters;
[Table(nameof(QuizOption))]
public class QuizOption : BaseModel
{
    [Required]
    public int QuizQuestionId { get; set; }

    // ✅ This FK + ForeignKey attribute removes all ambiguity
    [ForeignKey(nameof(QuizQuestionId))]
    public virtual QuizQuestion QuizQuestion { get; set; } = default!;

    
    public string? OptionText { get; set; } = default!;
    [DefaultValue(false)]
    public bool? IsCorrectOption { get; set; }
}
