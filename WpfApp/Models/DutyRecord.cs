using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassDutyHelper.Models
{
    public class DutyRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime DutyDate { get; set; }

        [Required]
        public int DutyProjectId { get; set; }

        [Required]
        public int StudentId { get; set; }

        public string? Remark { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(DutyProjectId))]
        public virtual DutyProject DutyProject { get; set; } = null!;

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;
    }
}
