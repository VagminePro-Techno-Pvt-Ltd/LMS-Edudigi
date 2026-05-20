using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Masters
{
    public class SignatureMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, MaxLength(100), Display(Name = "Title")]
        public string? Title { get; set; }
        [MapToDTO, MaxLength(100)]
        public string? FileName { get; set; }
        [MapToDTO, MaxLength(50)]
        public string? FilePath { get; set; }
        [NotMapped, RequiredIf($"{nameof(FilePath)} == null", ErrorMessage = "{0} is required"), Display(Name = "File")]
        public IFormFile? File { get; set; }
    }
}
