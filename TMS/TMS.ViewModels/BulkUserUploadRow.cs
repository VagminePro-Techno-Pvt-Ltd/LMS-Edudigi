namespace TMS.ViewModels
{
    /// <summary>
    /// Represents a single row from a bulk user upload file (CSV/Excel).
    /// Used for creating users independently of course enrollment.
    /// </summary>
    public class BulkUserUploadRow
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        /// <summary>Validation status set during preview</summary>
        public string Status { get; set; } = "Pending";
        /// <summary>Validation message set during preview</summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Upload processing modes.
    /// </summary>
    public enum BulkUploadMode
    {
        CreateOnly = 0,
        SkipExisting = 1,
        UpdateExisting = 2
    }

    /// <summary>
    /// Result model returned after processing a bulk user upload.
    /// </summary>
    public class BulkUserUploadResult
    {
        public int TotalProcessed { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
