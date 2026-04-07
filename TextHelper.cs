using System;
using System.Text.RegularExpressions;

namespace SnapText
{
    public static class TextHelper
    {
        /// <summary>
        /// OCR işleminden gelen metinlerdeki kırık satır sonlarını (kopuk cümleleri) birleştirir
        /// ve paragraf bütünlüğünü sağlar.
        /// </summary>
        public static string SmartCleanOcrText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 1. Satır sonundaki tireleme (hyphenation) hatalarını düzelt: "kelime-\n" -> "kelime"
            var text = Regex.Replace(input, @"-\s*\n\s*", "");

            // 2. Tek satır atlamalarını boşlukla değiştir. (Kopuk satırları birleştir)
            // Sadece tek \n olan yerleri \s yapar, çift \n (paragraf sonları) korunur.
            // \r leri temizleyerek başlayıp standartlaştırıyoruz.
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // Lookbehind ve Lookahead ile sadece tek \n olan yerleri yakala
            text = Regex.Replace(text, @"(?<!\n)\n(?!\n)", " ");

            // 3. Peş peşe gelen aşırı boşlukları tek boşluğa düşür
            text = Regex.Replace(text, @"[ \t]{2,}", " ");

            // 4. Paragraf aralarındaki aşırı enter'ları (3 ve fazlası) 2'ye düşür
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }
    }
}
