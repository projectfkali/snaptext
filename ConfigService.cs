using System;
using System.Collections.Generic;
using System.IO;
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

        public static void SaveHistory(List<OcrResult> history)
        {
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }

        public static List<OcrResult> LoadHistory()
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                return JsonSerializer.Deserialize<List<OcrResult>>(json) ?? new List<OcrResult>();
            }
            return new List<OcrResult>();
        }
    }
}
