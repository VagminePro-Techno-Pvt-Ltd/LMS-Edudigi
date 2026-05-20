using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Masters
{
    public class QueryMasterViewModel : BaseViewModel
    {
        [MapToDTO,Display(Name ="Course")]
        public int CourseId { get; set; }
        [MapToDTO,Required,MaxLength(200),Display(Name ="Description")]
        public virtual string? Description { get; set; }
        [MapToDTO,MaxLength(100)]

        public  string? FileName { get; set;}
        [MapToDTO,MaxLength(100)]

        public  string? FilePath { get; set;}
        [NotMapped,RequiredIf($"{nameof(FilePath)}==null", ErrorMessage = "{0} is required"), Display(Name="File")]

        public IFormFile? File { get; set; }

        public virtual CourseMasterViewModel? Course {  get; set; }
        [NotMapped]

        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Description)) return null;
                return $"{Description} ({IsActiveStr})";
            }
        }

    }
}
