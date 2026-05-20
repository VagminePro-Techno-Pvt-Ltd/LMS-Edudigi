using TMS;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.Models.Masters;
using TMS.Models.Account;

namespace TMS.ViewModels.Masters
{
    public class CourseEnrollmentViewModel : BaseViewModel
    {
        [MapToDTO, Required, Display(Name = "Course")]
        public int CourseId { get; set; }

        [MapToDTO, Required, Display(Name = "Student")]
        public int StudentId { get; set; }

        [MapToDTO, Display(Name = "Enrollment Date")]
        public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;

        // ─── NEW: Status-based Enrollment Lifecycle ───
        [MapToDTO, Display(Name = "Status")]
        public int Status { get; set; } = (int)EnrollmentStatus.Active;

        [MapToDTO, Display(Name = "Approved On")]
        public DateTime? ApprovedOn { get; set; }

        [MapToDTO, Display(Name = "Dropped On")]
        public DateTime? DroppedOn { get; set; }

        [MapToDTO, Display(Name = "Completed On")]
        public DateTime? CompletedOn { get; set; }

        [MapToDTO, Display(Name = "Source")]
        public string? Source { get; set; } = "Admin";

        // ─── Display Helpers ───
        public string StatusText => Status switch
        {
            1 => "Active",
            2 => "Pending",
            3 => "Dropped",
            4 => "Completed",
            5 => "Waitlisted",
            _ => "Unknown"
        };

        public string StatusBadgeClass => Status switch
        {
            1 => "bg-success",
            2 => "bg-warning text-dark",
            3 => "bg-danger",
            4 => "bg-info",
            5 => "bg-secondary",
            _ => "bg-dark"
        };

        // ─── Existing navigation / helper properties ───
        public UserMaster? Student { get; set; }
        public CourseMasterViewModel Course { get; set; } = new();
        public List<UserViewModel> AvailableStudents { get; set; } = new();
        public List<int> SelectedStudentIds { get; set; } = new();
    }
}
