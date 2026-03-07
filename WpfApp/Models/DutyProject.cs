using System.ComponentModel.DataAnnotations;

namespace ClassDutyHelper.Models
{
    public class DutyProject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int DefaultPersonCount { get; set; } = 1;

        public bool IsEnabled { get; set; } = true;

        public int SortOrder { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<DutyRecord> DutyRecords { get; set; } = new List<DutyRecord>();
    }
}
