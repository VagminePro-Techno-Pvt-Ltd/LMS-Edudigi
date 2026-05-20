using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class LanguageMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, MaxLength(20), Display(Name = "Country Code")]
        public string? CountryCode { get; set; }
        [MapToDTO, MaxLength(20), Display(Name = "Language Code")]
        public string? LanguageCode { get; set; }
    }
}
