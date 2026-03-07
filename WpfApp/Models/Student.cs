using System.ComponentModel.DataAnnotations;

namespace ClassDutyHelper.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? StudentId { get; set; }

        [MaxLength(50)]
        public string? Group { get; set; }

        public bool IsEnabled { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<DutyRecord> DutyRecords { get; set; } = new List<DutyRecord>();
    }
}
