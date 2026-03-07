using System.ComponentModel.DataAnnotations;

namespace ClassDutyHelper.Models
{
    public class SyncLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime SyncTime { get; set; } = DateTime.Now;

        public bool IsSuccess { get; set; }

        public string? Message { get; set; }

        public string? Detail { get; set; }
    }
}
