using System;

namespace StickerApp.Models
{
    public class StickerModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "New Sticker";
        public string Text { get; set; } = "";
        public double X { get; set; } = 100;
        public double Y { get; set; } = 100;
        public double Width { get; set; } = 250;
        public double Height { get; set; } = 200;
        public double Opacity { get; set; } = 1.0;
        public string Color { get; set; } = "#FFF8B0"; // Classic yellow sticker color
        public bool Locked { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Custom styling attributes
        public string Font { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14;
        public string FontColor { get; set; } = "#000000";
        public string? Image { get; set; } // Local path or relative path
        public string Animation { get; set; } = "FadeIn";

        // Layout modes
        public bool IsAlwaysOnTop { get; set; } = false;
        public bool IsClickThrough { get; set; } = false;
    }
}
