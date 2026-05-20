using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Masters
{
    public class BadgeMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Display(Name = "Category")]
        public int CourseCategoryId { get; set; }
        [MapToDTO, Display(Name = "Department")]
        public int DepartmentId { get; set; }
        [MapToDTO, Display(Name = "No Of Training Applicable")]
        public int? NoOfTrainingApplicable { get; set; }
        [MapToDTO, MaxLength(100)]
        public string? FileName { get; set; }
        [MapToDTO, MaxLength(50)]
        public string? FilePath { get; set; }

        [NotMapped, RequiredIf($"{nameof(FilePath)} == null", ErrorMessage = "{0} is required"), Display(Name = "File")]
        public IFormFile? File { get; set; }

        
        public virtual CourseCategoryMasterViewModel? CourseCategory { get; set; }
       
        public virtual DepartmentMasterViewModel? Department { get; set; }

    }
}
