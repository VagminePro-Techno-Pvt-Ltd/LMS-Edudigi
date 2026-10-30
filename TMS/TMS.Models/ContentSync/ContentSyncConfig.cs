using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.ContentSync
{
    [Table("ContentSyncConfig")]
    public class ContentSyncConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool LocalDriveEnabled { get; set; } = true;

        [StringLength(500)]
        public string LocalDriveRootPath { get; set; } = "EduDigiClass_LMS_Content";

        public bool GoogleDriveEnabled { get; set; } = false;

        [StringLength(200)]
        public string GoogleDriveRootFolderName { get; set; } = "EduDigiClass_LMS_Content";

        [StringLength(200)]
        public string? GoogleDriveRootFolderId { get; set; }

        [StringLength(200)]
        public string? GoogleDriveServiceAccountEmail { get; set; }

        public bool GoogleDriveNetworkDisabled { get; set; } = false;

        public int AutoSyncIntervalSeconds { get; set; } = 10;

        public bool DriveToLmsSyncEnabled { get; set; } = true;

        public DateTime? LastDriveToLmsScanUtc { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
