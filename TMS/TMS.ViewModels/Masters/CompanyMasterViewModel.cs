using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CompanyMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Required, MaxLength(50), Display(Name = "Initial")]
        public string? Initial { get; set; }

        [MapToDTO, Required, MaxLength(10), Display(Name = "Phone")]
        [Phone]
        public string? Phone { get; set; }
        [MapToDTO, Required, MaxLength(10), Display(Name = "Fax")]
        [Phone]
        public string? Fax { get; set; }
        [MapToDTO, Required, MaxLength(100), Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }
        [MapToDTO, Required, MaxLength(100), Display(Name = "Address 1")]
        public string? Address1 { get; set; }
        [MapToDTO, MaxLength(100), Display(Name = "Address 2")]
        public string? Address2 { get; set; }
        [MapToDTO, MaxLength(100), Display(Name = "Address 3")]
        public string? Address3 { get; set; }
        [MapToDTO, MaxLength(100), Display(Name = "Address 4")]
        public string? Address4 { get; set; }
    }
}
