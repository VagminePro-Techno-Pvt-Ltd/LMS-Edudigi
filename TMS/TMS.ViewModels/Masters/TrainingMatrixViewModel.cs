using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;


namespace TMS.ViewModels.Masters
{
    public class TrainingMatrixViewModel: BaseViewModel
    {
        
        [Required, MapToDTO, Display(Name = "Location")]
        public int LocationId { get; set; }
   
        [Required, MapToDTO, Display(Name = "Department")]
        public int DepartmentId { get; set; }
  
        [Required, MapToDTO, Display(Name = "Division")]
        public int DivisionId { get; set; }
     
        [Required, MapToDTO, Display(Name = "Designation")] 
        public int DesignationId { get; set; }

        [Required, MapToDTO, Display(Name = "Course")]
        public int CourseId { get; set; }
        [Required, MapToDTO, Display(Name = "Priority")]
        public int CoursePriority { get; set; } = 1;


        public virtual LocationMasterViewModel? Location { get; set; }
        public virtual DepartmentMasterViewModel? Department { get; set; }
        public virtual DivisionMasterViewModel? Division { get; set; }
        public virtual DesignationMasterViewModel? Designation { get; set; }
        public virtual CourseMasterViewModel? Course { get; set; }

        //Extra fields
        [NotMapped, Display(Name = "Course Category")]
        public int CourseCategoryId { get; set; }
    }
}
