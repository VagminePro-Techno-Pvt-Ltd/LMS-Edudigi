using TMS.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.ViewModels
{
    public class BaseMasterViewModel
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        [MapToDTO]
        public virtual string? Name { get; set; }
        [MaxLength(200)]
        [MapToDTO]
        public virtual string? Description { get; set; }
        

        [Display(Name = "Is Active")]
        [MapToDTO]
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public UserViewModel? CreatedByUser { get; set; }
        public UserViewModel? UpdatedByUser { get; set; }
        [NotMapped]
        public string? LastActionBy
        {
            get
            {
                if (UpdatedByUser != null)
                    return UpdatedByUser.Name;
                if (CreatedByUser != null)
                    return CreatedByUser.Name;
                return null;
            }
        }
        [NotMapped]
        public DateTime LastActionOn { get { return UpdatedOn.HasValue ? UpdatedOn.Value : CreatedOn; } }
        [NotMapped]
        public string LastActionOnOrder { get { return LastActionOn.ToString("yyyyMMddHHmmssfff"); } }
        [NotMapped]
        public string LastActionOnStr
        {
            get
            {
                return LastActionOn.UTCToIST().ToString("dd/MM/yyyy hh:mm tt");
            }
        }
        [NotMapped]
        public string IdEnc { get { return Id.Encrypt(); } }
        [NotMapped]
        public string IsActiveStr { get { return IsActive ? "Active" : "Inactive"; } }
        [NotMapped]
        //public string IsActiveStrHtml { get { return IsActive ? "<span class=\"bg-success rounded text-white p-1\">Active</span>" : "<span class=\"bg-danger rounded text-white p-1\">Inactive</span>"; } }
        public string IsActiveStrHtml { get { return $"<span class=\"status-{IsActiveStr}\">{IsActiveStr}</span>"; } }
        [NotMapped]
        public virtual string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{Name} ({IsActiveStr})";
            }
        }
        [NotMapped]
        public virtual string? NameEmail
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{Name} ({IsActiveStr})";
            }
        }
        [NotMapped]
		public virtual string? CourseName
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Name)) return null;
				return $"{Name}";
			}
		}

		[NotMapped]
        //public virtual string? NameStatusHtml
        //{
        //    get
        //    {
        //        if (string.IsNullOrWhiteSpace(Name)) return null;
        //        return $"{Name} <span class=\"opacity-50\">{IsActiveStrHtml}</span>";
        //    }
        //}
        public virtual string? NameStatusHtml
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{Name} ({IsActiveStrHtml})";
            }
        }

    }
}
