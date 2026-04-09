using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        private List<OcrHistoryItem> _history;
        private System.Windows.Forms.NotifyIcon _notifyIcon = null!;

        // ─── Win32 Hotkey ───────────────────────────────────────────────────
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT   = 0x0004;
        private const uint MOD_ALT     = 0x0001;
        private const uint MOD_WIN     = 0x0008;
        private const uint VK_S        = 0x53;

        private int  _hotkeyId         = 9000;
        private uint _currentModifiers = MOD_CONTROL | MOD_SHIFT;
        private uint _currentKey       = VK_S;

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // ─── Constructor ────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();

            this.MouseDown += (s, e) => { 
                if (e.ChangedButton == MouseButton.Left && HotkeyOverlay.Visibility != Visibility.Visible) 
                    this.DragMove(); 
            };

            _settings = ConfigService.LoadSettings();
            _history  = ConfigService.LoadHistory();
            
            LoadLanguages();
            ApplySettings();
            RefreshHistoryUI();
            InitializeTrayIcon();
        }

        // ─── Languages ──────────────────────────────────────────────────────
        private void LoadLanguages()
        {
            try
            {
                var languages = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages;
                LanguageComboBox.Items.Clear();
                foreach (var lang in languages)
                    LanguageComboBox.Items.Add(new ComboBoxItem { Content = lang.DisplayName, Tag = lang.LanguageTag });

                if (LanguageComboBox.Items.Count == 0)
                {
                    LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Türkçe", Tag = "tr-TR" });
                    LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = "en-US" });
                }
            }
            catch
            {
                LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Türkçe", Tag = "tr-TR" });
                LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = "en-US" });
            }
        }

        // ─── Hotkey registration ─────────────────────────────────────────────
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            HwndSource.FromHwnd(helper.Handle).AddHook(HwndHook);
            ParseHotkey(_settings.Hotkey);
            RegisterHotKey(helper.Handle, _hotkeyId, _currentModifiers, _currentKey);
        }

        private void ParseHotkey(string hotkeyStr)
        {
            var parts = hotkeyStr.Split('+').Select(p => p.Trim().ToUpper()).ToList();
            uint mods = 0;
            if (parts.Contains("CTRL"))  mods |= MOD_CONTROL;
            if (parts.Contains("SHIFT")) mods |= MOD_SHIFT;
            if (parts.Contains("ALT"))   mods |= MOD_ALT;
            if (parts.Contains("WIN"))   mods |= MOD_WIN;
            _currentModifiers = mods;
            string key = parts.Last();
            _currentKey = key.Length == 1 ? (uint)key[0] : VK_S;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == _hotkeyId) { OnHotKeyFired(); handled = true; }
            return IntPtr.Zero;
        }

        private void OnHotKeyFired() => CaptureButton_Click(null, null);

        // ─── Tray ────────────────────────────────────────────────────────────
        private void InitializeTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            try
            {
                var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/logo.png"));
                if (streamInfo != null)
                {
                    using var ms = new System.IO.MemoryStream();
                    streamInfo.Stream.CopyTo(ms);
                    byte[] pngBytes = ms.ToArray();

                    using var icoStream = new System.IO.MemoryStream();
                    using var writer = new System.IO.BinaryWriter(icoStream);
                    
                    writer.Write((short)0); // Reserved
                    writer.Write((short)1); // Type 1=Icon
                    writer.Write((short)1); // Count
                    
                    writer.Write((byte)0); // Width 256
                    writer.Write((byte)0); // Height 256
                    writer.Write((byte)0); // ColorCount
                    writer.Write((byte)0); // Reserved
                    writer.Write((short)1); // Planes
                    writer.Write((short)32); // BitCount
                    writer.Write(pngBytes.Length); // Size
                    writer.Write(22); // Offset
                    
                    writer.Write(pngBytes);
                    icoStream.Seek(0, System.IO.SeekOrigin.Begin);

                    _notifyIcon.Icon = new System.Drawing.Icon(icoStream);
                }
                else _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            catch { _notifyIcon.Icon = System.Drawing.SystemIcons.Application; }

            _notifyIcon.Visible = true;
            _notifyIcon.Text    = "SnapText";
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Göster", null, (s, e) => ShowWindow());
            menu.Items.Add("Çıkış",  null, (s, e) => { _notifyIcon.Dispose(); Application.Current.Shutdown(); });
            _notifyIcon.ContextMenuStrip = menu;
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // ─── Settings ────────────────────────────────────────────────────────
        private void ApplySettings()
        {
            TableModeCheckBox.IsChecked        = _settings.UseTableMode;
            EnhanceImageCheckBox.IsChecked     = _settings.EnhanceImage;
            AppendModeCheckBox.IsChecked       = _settings.AppendMode;
            AutoCopyCheckBox.IsChecked         = _settings.AutoCopy;
            AlwaysOnTopCheckBox.IsChecked      = _settings.AlwaysOnTop;
            StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
            HotkeyTextBlock.Text               = _settings.Hotkey;
            this.Topmost                       = _settings.AlwaysOnTop;

            foreach (ComboBoxItem item in LanguageComboBox.Items)
                if (item.Tag?.ToString() == _settings.Language) { LanguageComboBox.SelectedItem = item; break; }
        }

        private void RefreshHistoryUI()
        {
            HistoryListBox.ItemsSource = null;
            HistoryListBox.ItemsSource = _history;
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null || LanguageComboBox == null) return;

            _settings.UseTableMode     = TableModeCheckBox.IsChecked  ?? false;
            _settings.EnhanceImage     = EnhanceImageCheckBox.IsChecked ?? true;
            _settings.AppendMode       = AppendModeCheckBox.IsChecked  ?? false;
            _settings.AutoCopy         = AutoCopyCheckBox.IsChecked    ?? true;
            _settings.AlwaysOnTop      = AlwaysOnTopCheckBox.IsChecked ?? false;
            _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;
            this.Topmost               = _settings.AlwaysOnTop;

            if (LanguageComboBox.SelectedItem is ComboBoxItem lang)
                _settings.Language = lang.Tag?.ToString() ?? "tr-TR";

            ConfigService.SaveSettings(_settings);
            SetAutostart(_settings.StartWithWindows);
        }

        private void SetAutostart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (enable) key!.SetValue("SnapText", Process.GetCurrentProcess().MainModule!.FileName);
                else        key!.DeleteValue("SnapText", false);
            }
            catch { }
        }

        // ─── Button handlers ─────────────────────────────────────────────────
        private void CloseButton_Click(object sender, RoutedEventArgs e)   => this.Hide();
        private void OpenDrawer_Click(object sender, RoutedEventArgs e)    => SettingsOverlay.Visibility = Visibility.Visible;
        private void CloseDrawer_Click(object sender, RoutedEventArgs e)   => SettingsOverlay.Visibility = Visibility.Collapsed;
        private void ClearCurrent_Click(object sender, RoutedEventArgs e)  => ResultTextBox.Clear();
        private void CopyCurrent_Click(object sender, RoutedEventArgs e)   { if (!string.IsNullOrEmpty(ResultTextBox.Text)) Clipboard.SetText(ResultTextBox.Text); }

        private void TogglePin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OcrHistoryItem item)
            {
                item.IsPinned = !item.IsPinned;
                ConfigService.SaveHistory(_history);
                RefreshHistoryUI();
            }
        }

        private void DeleteHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OcrHistoryItem item)
            {
                _history.Remove(item);
                ConfigService.SaveHistory(_history);
                RefreshHistoryUI();
            }
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListBox.SelectedItem is OcrHistoryItem item)
            {
                ResultTextBox.Text = item.Text;
                Clipboard.SetText(item.Text);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history = _history.Where(h => h.IsPinned).ToList();
            ConfigService.SaveHistory(_history);
            RefreshHistoryUI();
        }

        // ─── Capture ──────────────────────────────────────────────────────────
        private void CaptureButton_Click(object? sender, RoutedEventArgs? e)
        {
            bool wasVisible = this.IsVisible;
            if (wasVisible) this.Hide();

            var win = new SelectionWindow
            {
                SelectedLanguage = _settings.Language,
                ShouldEnhance    = _settings.EnhanceImage,
                IsTableMode      = _settings.UseTableMode,
                AutoCopy         = _settings.AutoCopy
            };

            if (win.ShowDialog() == true)
            {
                string finaltext = "";
                
                // Process QR Code if found
                if (!string.IsNullOrEmpty(win.QrResult))
                {
                    finaltext = win.QrResult.Trim();
                    if (finaltext.StartsWith("http"))
                    {
                        try { Process.Start(new ProcessStartInfo { FileName = finaltext, UseShellExecute = true }); } catch { }
                    }
                }

                // Append OCR Text
                if (!string.IsNullOrEmpty(win.ExtractedText))
                {
                    string text = win.ExtractedText.Trim();
                    if (!string.IsNullOrEmpty(finaltext)) finaltext += "\n\n" + text;
                    else finaltext = text;
                }

                if (!string.IsNullOrEmpty(finaltext))
                {
                    // Append Mode Support
                    if (_settings.AppendMode)
                    {
                        var clip = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(clip)) finaltext = clip + "\n" + finaltext;
                    }

                    if (_settings.AutoCopy) Clipboard.SetText(finaltext);
                    _history.Insert(0, new OcrHistoryItem { Text = finaltext, Timestamp = DateTime.Now });
                    ConfigService.SaveHistory(_history);
                    RefreshHistoryUI();
                    
                    ResultTextBox.Text = finaltext;
                }

                this.Show();
                this.Activate();
            }
            else if (wasVisible) this.Show();
        }

        // ─── Hotkey overlay ───────────────────────────────────────────────────
        private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            HotkeyOverlay.Visibility = Visibility.Visible;
            HotkeyOverlay.Focus();
        }

        private void CancelHotkey_Click(object sender, RoutedEventArgs e) => HotkeyOverlay.Visibility = Visibility.Collapsed;

        private void HotkeyOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key is Key.System or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;

            var mods = new List<string>();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods.Add("Ctrl");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))   mods.Add("Shift");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))     mods.Add("Alt");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) mods.Add("Win");

            string newHotkey = string.Join(" + ", mods) + (mods.Count > 0 ? " + " : "") + e.Key;
            _settings.Hotkey = newHotkey;
            HotkeyTextBlock.Text = newHotkey;
            HotkeyOverlay.Visibility = Visibility.Collapsed;

            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, _hotkeyId);
            ParseHotkey(newHotkey);
            RegisterHotKey(helper.Handle, _hotkeyId, _currentModifiers, _currentKey);
            ConfigService.SaveSettings(_settings);
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────
        protected override void OnClosed(EventArgs e)
        {
            _notifyIcon?.Dispose();
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, _hotkeyId);
            base.OnClosed(e);
        }
    }
}