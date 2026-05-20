
using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

using static System.Net.Mime.MediaTypeNames;
 
namespace TMS.ViewModels.Account

{

    public class FormPermissions

    {

        public bool View { get; set; } = false;

        public bool Add { get; set; } = false;

        public bool Edit { get; set; } = false;


        public string EditViewText { get { return Edit ? "Edit" : "View"; } }

        public string EditViewIconClass { get { return Edit ? "fa-solid fa-pen-to-square text-warning" : "fa-solid fas fa-eye"; } }

        public string ScheduleViewText { get { return Edit ? "Approved" : "Pending for Approvel"; } }

        public string ScheduleViewIconClass { get { return Edit ? "far fa-check-square text-success" : ""; } }

        public string RejectViewText { get { return Edit ? "Reject" : "Reject"; } }

        public string RejectViewIconClass { get { return Edit ? "fa fa-ban text-danger" : ""; } }
        public string ApprovalViewIconClass { get
            {
                return Edit ? "fa-solid fas fa-eye" : "";

			} }
		public string ApprovalViewText { get { return Edit ? "History" : ""; } }

        public string NominationViewText { get { return Edit ? "" : "Nominate"; } }
        public string NominationViewIcon { get { return Edit ? "" : "fa fa-reply-all"; } }
        public string NominationApprovalViewText { get { return Edit ? "Approve" : ""; } }
        public string NominationApprovalViewIcon { get { return Edit ? "far fa-check-square text-success" : ""; } }
        public string NominationRejectViewText { get { return Edit ? "Reject" : ""; } }
        public string NominationRejectViewIcon { get { return Edit ? "fa fa-ban text-danger" : ""; } }
		public string AdminNominationViewText { get { return Edit ? "Nominate Users" : ""; } }
		public string AdminNominationViewIcon { get { return Edit ? "fa fa-reply-all" : ""; } }
		public string TrainingCancelText { get { return Edit ? "Cancel Training" : ""; } }
		public string TrainingCancelIcon { get { return Edit ? "fas fa-time" : ""; } }

		public bool CanShowAddEditButton(int id)

        {

            return Edit || (id == 0 && Add);

        }

    }

}



























































//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace TMS.ViewModels.Account
//{
//    public class FormPermissions
//    {
//        public bool View { get; set; } = false;
//        public bool Add { get; set; } = false;
//        public bool Edit { get; set; } = false;
//        public string EditViewText { get { return Edit ? "Edit" : "View"; } }
//        public string EditViewIconClass { get { return Edit ? "fa-solid fa-pen-to-square text-warning" : "fa-solid fas fa-eye"; } }
//        public bool Schedule { get; set; } = false;
//        public string ScheduleViewIconClass { get { return Edit ? "" : "Schedule"; } }
//        public bool Reject { get; set; } = false;
//        public string RejectViewIconClass { get { return Reject ? "fa-solid fas fa-eye" : "Reject"; } }
//        public bool CanShowAddEditButton(int id)
//        {
//            return Edit || (id == 0 && Add);
//        }
//    }
//}


//using System;

//using System.Collections.Generic;

//using System.Linq;

//using System.Text;

//using System.Threading.Tasks;

//namespace TMS.ViewModels.Account

//{

//	public class FormPermissions

//	{

//		public bool View { get; set; } = false;

//		public bool Add { get; set; } = false;

//		public bool Edit { get; set; } = false;

//		public bool Approve { get; set; } = false;

//		public bool Reject { get; set; } = false;

//		public string EditViewText { get { return Edit ? "Edit" : "View"; } }

//		public string EditViewIconClass { get { return Edit ? "fa-solid fa-pen-to-square text-warning" : "fa-solid fas fa-eye"; } }

//		public bool CanShowAddEditButton(int id)

//		{

//			return Edit || (id == 0 && Add);

//		}



//		public string ScheduleViewText { get { return Edit ? "Pending for Approvel" : "Approved"; } }

//		public string ScheduleViewIconClass { get { return Edit ? "Schedule" : ""; } }


//		public string RejectViewText { get { return Edit ? "Reject" : "Reject"; } }

//		public string RejectViewIconClass { get { return Edit ? "Reject" : ""; } }

//	}

//}
