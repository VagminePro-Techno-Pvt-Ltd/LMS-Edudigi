using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Masters
{
    public class QuestionMasterViewModel : BaseViewModel
    {
        [Required, MapToDTO, Display(Name = "Question")]
        public string? Question { get; set; }
        [MapToDTO, Display(Name = "Question Type")]
        public int QuestionType { get; set; }
        [Required]
        public virtual List<QuestionOptionMasterViewModel> QuestionOptions { get; set; } = new();
        public string? QuestionTypeStr
        {
            get
            {
                if (QuestionType == 2)
                    return "Question with two options";
                if (QuestionType == 3)
                    return "Question with three options";
                if (QuestionType == 4)
                    return "Question with four options";
                if (QuestionType == 5)
                    return "Question with five options";
                return null;
            }
        }
        [NotMapped]
        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Question)) return null;
                return $"{Question} ({IsActiveStr})";
            }
        }

        [NotMapped]
        //public override string? NameStatusHtml
        //{
        //    get
        //    {
        //        if (string.IsNullOrWhiteSpace(Question)) return null;
        //        return $"{Question} <span class=\"opacity-50\">{IsActiveStrHtml}</span>";
        //    }
        //}
        public override string? NameStatusHtml
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Question)) return null;
                return $"{Question} ({IsActiveStrHtml})";
            }
        }
    }
}
