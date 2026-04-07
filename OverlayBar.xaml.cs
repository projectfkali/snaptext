using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SnapText
{
    public partial class OverlayBar : Window
    {
        private readonly string _text;
        private readonly AppSettings _settings;
        private readonly LocalAiService _ai;

        public OverlayBar(string text, AppSettings settings)
        {
            InitializeComponent();
            _text     = text;
            _settings = settings;
            _ai       = new LocalAiService();

            this.Left = System.Windows.Forms.Cursor.Position.X - (this.Width / 2);
            this.Top  = System.Windows.Forms.Cursor.Position.Y + 20;
            
            // Ekran dışına taşmayı engelle
            if (this.Left < 0) this.Left = 10;
            if (this.Top  < 0) this.Top  = 10;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_text)) Clipboard.SetText(_text);
            this.Close();
        }

        private void TranslateWeb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tLang = _settings.TranslationTarget == "English" ? "en" : "tr";
                string url = $"https://translate.google.com/?sl=auto&tl={tLang}&text={Uri.EscapeDataString(_text)}&op=translate";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
            this.Close();
        }

        private async void AiTranslate_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            this.Opacity = 0.6;
            this.IsEnabled = false;

            string targetLang = _settings.TranslationTarget ?? "Turkish";
            string result = await _ai.TranslateTextAsync(_text, targetLang);
            
            Clipboard.SetText(result);
            MessageBox.Show($"Çeviri (Panoya Kopyalandı):\n\n{result}", "SnapText AI Çevirmen", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private async void AiSummarize_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            this.Opacity = 0.6;
            this.IsEnabled = false;
            
            string result = await _ai.ProcessTextAsync("Bu metni özetle:", _text);
            
            Clipboard.SetText(result);
            MessageBox.Show($"Özet (Panoya Kopyalandı):\n\n{result}", "SnapText AI Özetleyici", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        protected override void OnClosed(EventArgs e)
        {
            _ai?.Dispose();
            base.OnClosed(e);
        }
    }
}
