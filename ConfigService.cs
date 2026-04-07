using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SnapText
{
    public static class ConfigService
    {
        private static readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnapText");
        private static readonly string _settingsPath = Path.Combine(_appDataPath, "settings.json");
        private static readonly string _historyPath = Path.Combine(_appDataPath, "history.json");

        static ConfigService()
        {
            if (!Directory.Exists(_appDataPath))
            {
                Directory.CreateDirectory(_appDataPath);
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }

        public static AppSettings LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public static void SaveHistory(List<OcrHistoryItem> history)
        {
            // Sabitlenmiş ögeler asla silinmez. Maksimum 50 öge sınırı.
            if (history.Count > 50)
            {
                var pinnedItems = history.Where(h => h.IsPinned).ToList();
                var unpinnedItems = history.Where(h => !h.IsPinned).ToList();
                
                int slotsLeft = 50 - pinnedItems.Count;
                unpinnedItems = slotsLeft > 0 ? unpinnedItems.Take(slotsLeft).ToList() : new List<OcrHistoryItem>();
                
                history = pinnedItems.Concat(unpinnedItems).OrderByDescending(h => h.Timestamp).ToList();
            }
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }

        public static List<OcrHistoryItem> LoadHistory()
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                return JsonSerializer.Deserialize<List<OcrHistoryItem>>(json) ?? new List<OcrHistoryItem>();
            }
            return new List<OcrHistoryItem>();
        }
    }
}
