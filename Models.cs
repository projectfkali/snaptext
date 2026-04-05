using System;
using System.Collections.Generic;

namespace SnapText
{
    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class AppSettings
    {
        public bool AutoCopy { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = false;
        public string Language { get; set; } = "tr-TR"; // Default to Turkish
    }
}
