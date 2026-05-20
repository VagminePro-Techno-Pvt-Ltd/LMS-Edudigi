using System;
using System.Collections.Generic;

namespace TMS.ViewModels.Masters
{
    public class DiscussionReplyViewModel : BaseViewModel
    {
        public int ThreadId { get; set; }
        public int UserId { get; set; }
        public string CommentText { get; set; }
        public DateTime CommentedOn { get; set; }
        public int? ParentReplyId { get; set; }
        public bool IsAccepted { get; set; }

        public string UserName { get; set; }
        public List<DiscussionReplyViewModel> ChildReplies { get; set; }
    }
}
