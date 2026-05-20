using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(ForgotPassword))]
    public class ForgotPassword : BaseModel
    {
        [Required]
        [StringLength(100)]
        public string? Email { get; set; }
        
        public int OTP { get; set; }
       
       
    }
}
