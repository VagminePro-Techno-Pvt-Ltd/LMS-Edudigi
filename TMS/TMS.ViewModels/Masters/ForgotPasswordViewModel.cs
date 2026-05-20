using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Masters
{
    public class ForgotPasswordViewModel : BaseViewModel
    {
       
        [MapToDTO, Display(Name = "OTP")]
        public int OTP { get; set; }
       
        [MapToDTO, Display(Name = "Email"), MaxLength(100)]
        public string? Email { get; set; }

        [NotMapped, MaxLength(100)]
        public string Password { get;set; }
       
        [NotMapped, Display(Name = "New Password"), MaxLength(100)] 
        public string NewPassword { get;set; }
        
        [NotMapped, Display(Name = "Confirm Password"), MaxLength(100)]
        public string ConfirmPassword { get;set; }

       

    }
}
