using System;

namespace TMS.ViewModels.Academics
{
    public class CalendarEventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // assign / resource / seminar
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Color { get; set; } = "#0d6efd"; // default blue
        public bool IsActive { get; set; } = true;
    }
}
