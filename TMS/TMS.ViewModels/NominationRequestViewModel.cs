using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Http;



using System.Reflection.Metadata.Ecma335;

using TMS.Common;

using UoN.ExpressiveAnnotations.NetCore.Attributes;
using TMS.ViewModels.Training;

namespace TMS.ViewModels.Masters

{

	public class NominationRequestViewModel : BaseViewModel

	{

		
		[Required]

		public int CandidateId { get; set; }

		[Required, Display(Name = "TrainingId")]

		public int TrainingId { get; set; }

		[Required, Display(Name = "Priority")]

		public int CoursePriority { get; set; } = 1;

		[MapToDTO]
		public NominationRequestStatus Status { get; set; }

		public virtual LocationMasterViewModel? Location { get; set; }

		public virtual DepartmentMasterViewModel? Department { get; set; }

		public virtual DivisionMasterViewModel? Division { get; set; }

		public virtual DesignationMasterViewModel? Designation { get; set; }

		public virtual TrainingScheduleViewModel? Training { get; set; }
		public virtual UserViewModel? Candidate { get; set; }

		public virtual List<NominationRequestRemarksViewModel>? NominationRemarks { get; set; }
       

        //Extra fields

        [NotMapped, Display(Name = "Course Category")]

		public int CourseCategoryId { get; set; }

	}

}
