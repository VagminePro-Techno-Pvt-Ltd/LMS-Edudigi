using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TMS.Common;
using TMS.ViewModels.Masters;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Training
{
    public class TrainingScheduleViewModel : BaseViewModel
    {
        [MapToDTO, Display(Name = "Course")]
        public int CourseId { get; set; }
        [MapToDTO, Display(Name = "As Per Matrix")]
        public bool IsAsPerMatrix { get; set; }
        [Required, MapToDTO, MaxLength(200), Display(Name = "Training Title")]
        public string? TrainingTitle { get; set; }
        [MapToDTO, Display(Name = "Conducted By Division")]
        public int ConductedByDivisionId { get; set; }

        [Required, DataType(DataType.Date), MapToDTO, Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }
        [Required, DataType(DataType.Date), MapToDTO, Display(Name = "To Date")]
        [AssertThat("ToDate >= FromDate", ErrorMessage = "To Date must be later than From Date")]
        public DateTime? ToDate { get; set; }
        [Required, DataType(DataType.Time), MapToDTO, Display(Name = "From Time")]
        public TimeSpan? FromTime { get; set; }
        [Required, DataType(DataType.Time), MapToDTO, Display(Name = "To Time")]
        [AssertThat("ToTime > FromTime", ErrorMessage = "To Time must be later than From Time")]
        public TimeSpan? ToTime { get; set; }

        [MapToDTO, Display(Name = "Online")]
        public bool IsOnline { get; set; }
        [MapToDTO, Display(Name = "Venue")]
        public int VenueId { get; set; }
        [MapToDTO,Display(Name ="Seats")]
        public int VenueSeat {  get; set; }
        [Required, MapToDTO, Display(Name = "Min. Number Of Seats")]
        public int? MinNumberOfSeats { get; set; }
        [MapToDTO, Display(Name = "Max Number Of Seats")]
        [AssertThat($"{nameof(MaxNumberOfSeats)} <= {nameof(VenueSeat)}&&{nameof(MaxNumberOfSeats)}>{nameof(MinNumberOfSeats)}", ErrorMessage = "Max Number Of Seats must be lesser than the number of VenueSeats and more than the Min Number of the seats")]
        public int? MaxNumberOfSeats { get; set; }
        [MapToDTO, Display(Name = "Instructor")]
        public int InstructorId { get; set; }
        [MapToDTO, Display(Name = "Is Quiz Mandatory")]
        public bool IsQuizMandatory { get; set; }
		[MapToDTO, Display(Name = "Is Feedback Mandatory")]
		public bool IsFeedBackMandatory { get; set; }
        [MaxLength(200),Display(Name ="Remarks")]
        public string? Remarks { get; set; }
        [MapToDTO, Display(Name = "Status")]
        public int StatusId { get; set; }
        [MapToDTO, Display(Name = "Re-Scheduled")]
        public bool IsReScheduled { get; set; }
        [Display(Name ="Meeting Link")]
        public string? MeetingLink { get; set; }
       
        public List<TrainingScheduleLanguageViewModel>? TrainingScheduleLanguages { get; set; } = new();
        [NotMapped]
        public List<TrainingScheduleAttachmentViewModel>? TrainingScheduleAttachment { get; set; } = new();

        public CourseMasterViewModel? Course { get; set; }
        public DivisionMasterViewModel? ConductedByDivision { get; set; }
        public VenueMasterViewModel? Venue { get; set; }
        public UserViewModel? Instructor { get; set; }

        
        public TrainingScheduleStatusMasterViewModel? Status { get; set; }
        public TrainingScheduleApprovalHistoryViewModel? Approval { get; set; }
        //public List<NominationRequestRemarksViewModel>? RequestRemarks {  get; set; }

        [NotMapped, Display(Name = "Training Materials")]
        public IFormFile[]? Files { get; set; }

        [NotMapped]
        public string DateRangeStr
        {
            get
            {
                if (!FromDate.HasValue) return string.Empty;
                return $"{FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}";
            }
        }

        [NotMapped]
        public string TimeRangeStr
        {
            get
            {
                if (!FromTime.HasValue || !ToTime.HasValue) return string.Empty;
                return $"{new DateTime(FromTime.Value.Ticks):hh:mm tt} - {new DateTime(ToTime.Value.Ticks):hh:mm tt}";
            }
        }
        [NotMapped]
        public string SittingRangeStr
        {
            get
            {
                
                if (!MinNumberOfSeats.HasValue || !MaxNumberOfSeats.HasValue) return string.Empty;
                var SittingRange =  $"{ MinNumberOfSeats} - {MaxNumberOfSeats }";
                return SittingRange;
            }
        }
        [NotMapped]
        public string ModeStr
        {
            get
            {
                return IsOnline ? "Online" : "Offline";
            }
        }
        //  public bool IsShowNomination { get; set; } = false;

        [NotMapped]
        public int? NRRequest { get; set; }

        [NotMapped]
        public String? VenueName {  get; set; }
        [NotMapped]
        public String? StatusName { get; set; }
        [NotMapped]
        public String? UpdatedByUserName  { get; set;}
        [NotMapped]
        public String? StatusDescription { get; set; }

        [NotMapped]
        public int? NominationLeft { get; set; }
    }
}
