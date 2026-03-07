using System.ComponentModel.DataAnnotations;

namespace ClassDutyHelper.Models
{
    public class Reminder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public TimeSpan Time { get; set; }

        public int AdvanceMinutes { get; set; } = 5;

        public bool IsEnabled { get; set; } = true;

        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string TimeText => Time.ToString(@"hh\:mm");
    }
}
