using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;

namespace SnapText
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private List<OcrResult> _history;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private SpeechSynthesizer _speechSynthesizer;

        // --- Win32 Hotkey Constants ---
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_S = 0x53; // 'S' key

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();
            
            // Allow dragging from anywhere on the window (since we use custom chrome)
            this.MouseDown += (s, e) => { 
                if (e.ChangedButton == MouseButton.Left) this.DragMove(); 
            };
            
            _settings = ConfigService.LoadSettings();
            _history = ConfigService.LoadHistory();
            _speechSynthesizer = new SpeechSynthesizer();
            
            LoadLanguages();
            ApplySettings();
            RefreshHistoryUI();
            InitializeTrayIcon();
        }

        private void LoadLanguages()
        {
            try
            {
                var languages = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages;
                LanguageComboBox.Items.Clear();
                
                foreach (var lang in languages)
                {
                    LanguageComboBox.Items.Add(new ComboBoxItem 
                    { 
                        Content = lang.DisplayName, 
                        Tag = lang.LanguageTag 
                    });
                }

                // If no languages available (shouldn't happen on modern Win), add fallbacks
                if (LanguageComboBox.Items.Count == 0)
                {
                    LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Türkçe", Tag = "tr-TR" });
                    LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = "en-US" });
                }
            }
            catch
            {
                // Fallback for older systems or errors
                LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Türkçe", Tag = "tr-TR" });
                LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = "en-US" });
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);

            // Register Hotkey: Ctrl + Shift + S
            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_S);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                OnHotKeyFired();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyFired()
        {
            if (!this.IsVisible && Application.Current.Windows.OfType<SelectionWindow>().Any()) return;
            CaptureButton_Click(null, null);
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            try {
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            } catch {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "SnapText OCR";
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Göster", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Çıkış", null, (s, e) => {
                _notifyIcon.Dispose();
                Application.Current.Shutdown();
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ApplySettings()
        {
            AutoCopyCheckBox.IsChecked = _settings.AutoCopy;
            AlwaysOnTopCheckBox.IsChecked = _settings.AlwaysOnTop;
            StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
            this.Topmost = _settings.AlwaysOnTop;

            // Select OCR Language
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
            if (LanguageComboBox.SelectedItem == null && LanguageComboBox.Items.Count > 0)
                LanguageComboBox.SelectedIndex = 0;

            // Select Target Language
            foreach (ComboBoxItem item in TargetLangComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.TranslationTarget)
                {
                    TargetLangComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void RefreshHistoryUI()
        {
            HistoryListBox.ItemsSource = null;
            HistoryListBox.ItemsSource = _history;
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null || LanguageComboBox == null || TargetLangComboBox == null) return;

            _settings.AutoCopy = AutoCopyCheckBox.IsChecked ?? true;
            _settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? false;
            _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;
            this.Topmost = _settings.AlwaysOnTop;

            if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
                _settings.Language = langItem.Tag?.ToString() ?? "tr-TR";

            if (TargetLangComboBox.SelectedItem is ComboBoxItem targetItem)
                _settings.TranslationTarget = targetItem.Tag?.ToString() ?? "tr";

            ConfigService.SaveSettings(_settings);
            SetAutostart(_settings.StartWithWindows);
        }

        private void SetAutostart(bool enable)
        {
            try
            {
                string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true))
                {
                    if (enable)
                        key.SetValue("SnapText", Process.GetCurrentProcess().MainModule.FileName);
                    else
                        key.DeleteValue("SnapText", false);
                }
            }
            catch { /* Ignore registry errors */ }
        }

        // --- Event Handlers ---
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void OpenDrawer_Click(object sender, RoutedEventArgs e) => MainDrawerHost.IsRightDrawerOpen = true;
        private void CloseDrawer_Click(object sender, RoutedEventArgs e) => MainDrawerHost.IsRightDrawerOpen = false;
        private void ClearCurrent_Click(object sender, RoutedEventArgs e) => ResultTextBox.Clear();

        private void CopyCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ResultTextBox.Text))
                Clipboard.SetText(ResultTextBox.Text);
        }

        private void CopyHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string text)
            {
                Clipboard.SetText(text);
                MainDrawerHost.IsRightDrawerOpen = false;
                ResultTextBox.Text = text;
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            ConfigService.SaveHistory(_history);
            RefreshHistoryUI();
        }

        private void SearchGoogle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text)) return;
            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(ResultTextBox.Text)}";
            OpenUrl(url);
        }

        private void Translate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text)) return;
            string target = _settings.TranslationTarget;
            string url = $"https://translate.google.com/?sl=auto&tl={target}&text={Uri.EscapeDataString(ResultTextBox.Text)}&op=translate";
            OpenUrl(url);
        }

        private void Speak_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text)) return;
            _speechSynthesizer.SpeakAsyncCancelAll();
            _speechSynthesizer.SpeakAsync(ResultTextBox.Text);
        }

        private void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            bool wasVisible = this.IsVisible;
            if (wasVisible) this.Hide();

            var selectionWin = new SelectionWindow();
            selectionWin.SelectedLanguage = _settings.Language;

            if (selectionWin.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(selectionWin.ExtractedText))
                {
                    string text = selectionWin.ExtractedText.Trim();
                    ResultTextBox.Text = text;

                    if (_settings.AutoCopy)
                        Clipboard.SetText(text);

                    _history.Insert(0, new OcrResult { Text = text, Timestamp = DateTime.Now });
                    if (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
                    
                    ConfigService.SaveHistory(_history);
                    RefreshHistoryUI();
                }
                else
                {
                    ResultTextBox.Text = "Metin bulunamadı.";
                }
                this.Show();
                this.Activate();
            }
            else if (wasVisible)
            {
                this.Show();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _notifyIcon?.Dispose();
            _speechSynthesizer?.Dispose();
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            base.OnClosed(e);
        }
    }
}