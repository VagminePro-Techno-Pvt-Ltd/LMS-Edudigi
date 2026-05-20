using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Common;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace TMS.ViewModels.Training
{
    public class TrainingScheduleAttachmentViewModel
    {
        public int Id { get; set; }
        [MapToDTO]
        public int TraningScheduleId { get; set; }
        [MapToDTO, MaxLength(100)]
        public string? FileName { get; set; }
        [MapToDTO, MaxLength(50)]
        public string? FilePath { get; set; }       
    }
}
