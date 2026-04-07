using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace SnapText
{
    public partial class SelectionWindow : Window
    {
        private System.Windows.Point _startPoint;
        private bool _isDragging = false;
        public string ExtractedText { get; private set; } = string.Empty;
        public string QrResult { get; private set; } = string.Empty;
        public bool ShouldEnhance { get; set; } = true;
        public bool IsTableMode { get; set; } = false;
        public string SelectedLanguage { get; set; } = "tr-TR";

        public SelectionWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(OverlayCanvas);
                _isDragging = true;
                SelectionRectangle.Visibility = Visibility.Visible;
                SelectionRectangle.Width = 0;
                SelectionRectangle.Height = 0;
                System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, _startPoint.X);
                System.Windows.Controls.Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                var pos = e.GetPosition(OverlayCanvas);
                var x = Math.Min(pos.X, _startPoint.X);
                var y = Math.Min(pos.Y, _startPoint.Y);
                var w = Math.Max(pos.X, _startPoint.X) - x;
                var h = Math.Max(pos.Y, _startPoint.Y) - y;

                SelectionRectangle.Width = w;
                SelectionRectangle.Height = h;
                System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, x);
                System.Windows.Controls.Canvas.SetTop(SelectionRectangle, y);
            }
        }

        private async void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            SelectionRectangle.Visibility = Visibility.Hidden;

            double left = System.Windows.Controls.Canvas.GetLeft(SelectionRectangle);
            double top = System.Windows.Controls.Canvas.GetTop(SelectionRectangle);
            double width = SelectionRectangle.Width;
            double height = SelectionRectangle.Height;

            if (width < 10 || height < 10) 
            {
                this.DialogResult = false;
                this.Close();
                return;
            }

            // --- DPI Ölçeklendirme Onarımı ---
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            // WPF koordinatlarını fiziksel piksel koordinatlarına çeviriyoruz
            int ix = (int)(left * dpiX);
            int iy = (int)(top * dpiY);
            int iw = (int)(width * dpiX);
            int ih = (int)(height * dpiY);

            Bitmap bmp = new Bitmap(iw, ih, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(ix, iy, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            }

            if (ShouldEnhance)
            {
                bmp = EnhanceImage(bmp);
            }

            QrResult = ScanQRCode(bmp);
            ExtractedText = await PerformOcr(bmp);
            
            this.DialogResult = true;
            this.Close();
        }

        private Bitmap EnhanceImage(Bitmap bmp)
        {
            try
            {
                Bitmap newBmp = new Bitmap(bmp.Width, bmp.Height);
                using (Graphics g = Graphics.FromImage(newBmp))
                {
                    // Create grayscale matrix
                    System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                        new float[][]
                        {
                            new float[] {.3f, .3f, .3f, 0, 0},
                            new float[] {.59f, .59f, .59f, 0, 0},
                            new float[] {.11f, .11f, .11f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                        });

                    using (System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        attributes.SetColorMatrix(colorMatrix);
                        g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
                            0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                    }
                }
                return newBmp;
            }
            catch { return bmp; }
        }

        private string ScanQRCode(Bitmap bmp)
        {
            try
            {
                var reader = new ZXing.Windows.Compatibility.BarcodeReader();
                var result = reader.Decode(bmp);
                return result?.Text ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private async Task<string> PerformOcr(Bitmap bmp)
        {
            try
            {
                using var stream = new MemoryStream();
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = stream.ToArray();

                using var randomAccessStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(randomAccessStream))
                {
                    dataWriter.WriteBytes(bytes);
                    await dataWriter.StoreAsync();
                    dataWriter.DetachStream();
                }
                
                randomAccessStream.Seek(0);
                
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(SelectedLanguage));
                if (ocrEngine == null) {
                    ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                }
                
                
                var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
                
                if (IsTableMode)
                {
                    return TableService.ProcessToMarkdown(ocrResult);
                }
                
                return ocrResult.Text;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("OCR Hatası: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
