using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Account
{
    [Serializable]
    public class MenuViewModel
    {
        public virtual int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public virtual string? Description { get; set; }
        public string? IconClass { get; set; }
        public int Sequence { get; set; }
        public bool IsAdmin { get; set; }
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
    }
}
