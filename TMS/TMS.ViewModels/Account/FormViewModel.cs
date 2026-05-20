using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Account
{
    [Serializable]
    public class FormViewModel
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Area { get; set; }
        [Required]
        public string? Controller { get; set; }
        [Required]
        public string? Action { get; set; }
        public string? IconClass { get; set; }
        public int Sequence { get; set; }
        public int? MenuId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool View { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Edit { get; set; } = false;
        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Description))
                    return string.Empty;
                if (string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(Name))
                    return Name;
                if (!string.IsNullOrWhiteSpace(Name) && Name.Equals(Description, StringComparison.OrdinalIgnoreCase))
                    return Name;
                return $"{Name}: {Description}";
            }
        }

        [NotMapped]
        public int RecordCountActive { get; set; }
        [NotMapped]
        public int RecordCountInactive { get; set; }
    }
}
