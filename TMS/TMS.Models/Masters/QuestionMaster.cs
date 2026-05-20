using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(QuestionMaster))]
    public class QuestionMaster : BaseModel
    {
        public string? Question { get; set; }
        public int QuestionType { get; set; }
        public virtual List<QuestionOptionMaster> QuestionOptions { get; set; } = new();
    }
}
