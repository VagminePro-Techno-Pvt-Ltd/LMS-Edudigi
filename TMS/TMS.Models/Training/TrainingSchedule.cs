using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Training
{
    public class TrainingSchedule : BaseModel
    {
        public int CourseId { get; set; }
        public bool IsAsPerMatrix { get; set; }
        [Required, StringLength(200)]
        public string? TrainingTitle { get; set; }
        public int ConductedByDivisionId { get; set; }
        [DataType(DataType.Date), Column(TypeName = "Date")]
        public DateTime FromDate { get; set; }
        [DataType(DataType.Date), Column(TypeName = "Date")]
        public DateTime ToDate { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan FromTime { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan ToTime { get; set; }
        public bool IsOnline { get; set; }
        public int VenueId { get; set; }
		
		public int MinNumberOfSeats { get; set; }
        public int MaxNumberOfSeats { get; set; }
        public int InstructorId { get; set; }
        public bool IsQuizMandatory { get; set; }
        public bool IsFeedBackMandatory { get; set; }
        public int StatusId { get; set; }
        public bool IsReScheduled { get; set; }
        [StringLength(500)]
        public string? MeetingLink  { get; set; }
        public List<TrainingScheduleLanguage> TrainingScheduleLanguages { get; set; } = new();
        public List<TrainingScheduleAttachment> TrainingScheduleAttachment { get; set; } = new();

        [ForeignKey(nameof(CourseId))]
        public CourseMaster? Course { get; set; }
        [ForeignKey(nameof(ConductedByDivisionId))]
        public DivisionMaster? ConductedByDivision { get; set; }
        [ForeignKey(nameof(VenueId))]
        public VenueMaster? Venue { get; set; }


        [ForeignKey(nameof(InstructorId))]
        public UserMaster? Instructor { get; set; }


        [ForeignKey(nameof(StatusId))]
        public TrainingScheduleStatusMaster? Status { get; set; }

    }

}
