using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;


namespace TMS.Models.Masters
{
    public class CourseFacultyMapMaster : BaseModel
    {
        public int CourseId { get; set; }
        public int FacultyId { get; set; }  

       
        public virtual CourseMaster Course { get; set; }
        public virtual UserMaster Faculty { get; set; }
    }
}

