using System.ComponentModel.DataAnnotations;

namespace ClassDutyHelper.Models
{
    public class AppSettings
    {
        [Key]
        public int Id { get; set; } = 1;

        public string? ClassName { get; set; }

        public double TopBarOpacity { get; set; } = 1.0;

        public int TopBarHeight { get; set; } = 50;

        public bool TopBarVisible { get; set; } = true;

        public int TopBarPosition { get; set; } = 0;

        public bool StartWithWindows { get; set; } = false;

        public bool WindowTopMost { get; set; } = false;

        public bool HideMainWindowOnStart { get; set; } = false;

        public double WindowOpacity { get; set; } = 0.9;

        public string? CloudFormUrl { get; set; }

        public string? AdminKey { get; set; }

        public int SyncIntervalMinutes { get; set; } = 5;

        public DateTime LastSyncTime { get; set; } = DateTime.MinValue;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
