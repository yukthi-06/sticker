namespace StickerApp.Models
{
    public class SettingsModel
    {
        public string Theme { get; set; } = "system"; // light, dark, system
        public bool StartWithWindows { get; set; } = false;
        public bool TrayEnabled { get; set; } = true;
        public bool NotificationEnabled { get; set; } = true;
        public bool AnimationsEnabled { get; set; } = true;
        public int BackupIntervalMinutes { get; set; } = 60;
        public string Version { get; set; } = "1.0.0";
    }
}
