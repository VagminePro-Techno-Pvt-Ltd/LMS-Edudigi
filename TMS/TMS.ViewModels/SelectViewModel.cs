using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels
{
    public class SelectViewModel
    {
        public string Id { get; set; }=string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentId { get; set; }
    }
    public class SelectIntViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ParentId { get; set; }
    }
}
