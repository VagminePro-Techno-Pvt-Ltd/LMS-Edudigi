using System;

namespace TMS.ViewModels.Academics
{
    public class InteractivePPTStudentViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}

