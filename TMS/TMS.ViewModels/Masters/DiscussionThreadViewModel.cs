using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class DiscussionThreadViewModel : BaseViewModel
    {
        [MapToDTO]
        public int? CourseQuadrantId { get; set; }

        [MapToDTO]
        public int? ProgramId { get; set; }

        [MapToDTO, Required, MaxLength(255)]
        public string Title { get; set; } = default!;

        [MapToDTO]
        public int CreatedByUserId { get; set; }

        [MapToDTO]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [MapToDTO]
        public bool IsLocked { get; set; }

        [MapToDTO]
        public bool IsPinned { get; set; }

        [MapToDTO]
        public int ViewCount { get; set; }

        public string CreatedByUserName { get; set; }
        public List<DiscussionReplyViewModel> Replies { get; set; } = new();
    }
}
