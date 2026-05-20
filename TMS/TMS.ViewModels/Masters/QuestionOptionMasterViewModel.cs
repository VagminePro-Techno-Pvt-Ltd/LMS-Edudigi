using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;
namespace TMS.ViewModels.Masters
{
    public class QuestionOptionMasterViewModel : BaseViewModel
    {
        [Required, MapToDTO, Display(Name = "Option")]
        public string? Option { get; set; }
        [MapToDTO, Display(Name = "Correct Answer")]
        public bool IsCorrectAnswer { get; set; }
        public int QuestionId { get; set; }
    }
}
