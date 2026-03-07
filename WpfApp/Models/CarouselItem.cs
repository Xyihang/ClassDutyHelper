using System.ComponentModel.DataAnnotations;

namespace ClassDutyHelper.Models
{
    public class CarouselItem
    {
        [Key]
        public int Id { get; set; }

        public string Content { get; set; } = "";

        public int SortOrder { get; set; } = 0;

        public bool IsEnabled { get; set; } = true;

        public bool IsSystemDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
