using System;
using System.Collections.Generic;

namespace SnapText
{
    public class OcrHistoryItem
    {
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsPinned { get; set; } = false;
    }

    public class AppSettings
    {
        public bool AutoCopy { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = false;
        public string Language { get; set; } = "tr-TR";
        public bool StartWithWindows { get; set; } = false;
        public string TranslationTarget { get; set; } = "tr";
        public bool AppendMode { get; set; } = false;
        public bool EnhanceImage { get; set; } = true;
        public string Hotkey { get; set; } = "Ctrl+Shift+S";
        public bool UseTableMode { get; set; } = false;
        public string Theme { get; set; } = "Dark";
    }
}
