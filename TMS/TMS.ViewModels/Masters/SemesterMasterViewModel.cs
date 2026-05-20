using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class SemesterMasterViewModel : BaseMasterViewModel
    {
       

        public override string? NameStatus => $"{Name} ({IsActiveStr})";
    }
}
