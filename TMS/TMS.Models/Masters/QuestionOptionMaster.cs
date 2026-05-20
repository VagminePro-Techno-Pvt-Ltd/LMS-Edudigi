using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(QuestionOptionMaster))]
    public class QuestionOptionMaster : BaseModel
    {
        public string? Option { get; set; }
        public bool IsCorrectAnswer { get; set; }
        public int QuestionId { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionMaster? Question { get; set; }
    }
}
