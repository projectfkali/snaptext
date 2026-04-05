using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace SnapText
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private List<OcrResult> _history;
        private System.Windows.Forms.NotifyIcon _notifyIcon;

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
            this.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) this.DragMove(); };
            
            _settings = ConfigService.LoadSettings();
            _history = ConfigService.LoadHistory();
            
            ApplySettings();
            RefreshHistoryUI();
            InitializeTrayIcon();
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
            // If the window is already in selection mode, ignore.
            if (!this.IsVisible && Application.Current.Windows.OfType<SelectionWindow>().Any()) return;
            CaptureButton_Click(null, null);
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            try {
                // Using an icon from resources if available, or a fallback.
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
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
            this.Topmost = _settings.AlwaysOnTop;

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.Language)
                {
                    LanguageComboBox.SelectedItem = item;
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
            if (_settings == null || AutoCopyCheckBox == null || AlwaysOnTopCheckBox == null || LanguageComboBox == null) return;

            _settings.AutoCopy = AutoCopyCheckBox.IsChecked ?? true;
            _settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? false;
            this.Topmost = _settings.AlwaysOnTop;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selected)
            {
                _settings.Language = selected.Tag?.ToString() ?? "tr-TR";
            }

            ConfigService.SaveSettings(_settings);
        }

        // --- Window Hide Instead of Close ---
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();

        private void OpenDrawer_Click(object sender, RoutedEventArgs e) => MainDrawerHost.IsRightDrawerOpen = true;

        private void CloseDrawer_Click(object sender, RoutedEventArgs e) => MainDrawerHost.IsRightDrawerOpen = false;

        private void ClearCurrent_Click(object sender, RoutedEventArgs e) => ResultTextBox.Clear();

        private void CopyCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ResultTextBox.Text))
            {
                 System.Windows.Clipboard.SetText(ResultTextBox.Text);
            }
        }

        private void CopyHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string text)
            {
                System.Windows.Clipboard.SetText(text);
                MainDrawerHost.IsRightDrawerOpen = false;
                ResultTextBox.Text = text;
            }
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
                    {
                        System.Windows.Clipboard.SetText(text);
                    }

                    _history.Insert(0, new OcrResult { Text = text, Timestamp = DateTime.Now });
                    if (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
                    
                    ConfigService.SaveHistory(_history);
                    RefreshHistoryUI();
                }
                else
                {
                    ResultTextBox.Text = "Metin bulunamadı.";
                }
                
                // If it was triggered via hotkey in background, show the window with result.
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
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            base.OnClosed(e);
        }
    }
}