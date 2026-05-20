using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    [Table("Topics")]
    public class TopicLookup
    {
        [Key]
        [Column("TopicId")]
        public int Id { get; set; }

        public int SubjectId { get; set; }
        public int UnitId { get; set; }

        [Column("TopicName")]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
