using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Media.Ocr;

namespace SnapText
{
    public static class TableService
    {
        /// <summary>
        /// OCR sonucunu Markdown tablo formatına çevirir.
        /// Satırlar arası boşluk algoritmasına göre sütunları tespit eder.
        /// </summary>
        public static string ProcessToMarkdown(OcrResult result)
        {
            if (result == null || result.Lines.Count == 0) return string.Empty;

            var rows = BuildRows(result);
            if (rows.Count == 0) return string.Empty;

            int maxCols = rows.Max(r => r.Count);
            var sb      = new StringBuilder();

            // Başlık satırı
            AppendRow(sb, rows[0], maxCols);

            // Ayraç
            sb.Append("| ");
            for (int i = 0; i < maxCols; i++) sb.Append(" --- | ");
            sb.AppendLine();

            // Veri satırları
            for (int r = 1; r < rows.Count; r++)
                AppendRow(sb, rows[r], maxCols);

            return sb.ToString();
        }

        /// <summary>
        /// OCR sonucunu CSV formatına çevirir.
        /// </summary>
        public static string ProcessToCsv(OcrResult result)
        {
            if (result == null || result.Lines.Count == 0) return string.Empty;

            var rows = BuildRows(result);
            if (rows.Count == 0) return string.Empty;

            int maxCols = rows.Max(r => r.Count);
            var sb      = new StringBuilder();

            foreach (var row in rows)
            {
                for (int c = 0; c < maxCols; c++)
                {
                    if (c > 0) sb.Append(',');
                    string val = c < row.Count ? row[c] : "";
                    // CSV'de virgül veya çift tırnak içeren değerleri tırnak içine al
                    if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                        val = $"\"{val.Replace("\"", "\"\"")}\"";
                    sb.Append(val);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ─── Ortak satır oluşturma mantığı ──────────────────────────────────
        private static List<List<string>> BuildRows(OcrResult result)
        {
            var rows = new List<List<string>>();

            foreach (var line in result.Lines.OrderBy(l => l.Words[0].BoundingRect.Top))
            {
                var    row         = new List<string>();
                string currentCell = "";
                double lastRight   = -1;

                foreach (var word in line.Words.OrderBy(w => w.BoundingRect.Left))
                {
                    double gap       = lastRight > 0 ? word.BoundingRect.Left - lastRight : 0;
                    double threshold = word.BoundingRect.Width * 1.5;

                    if (gap > threshold && !string.IsNullOrEmpty(currentCell))
                    {
                        row.Add(currentCell.Trim());
                        currentCell = "";
                    }

                    currentCell += word.Text + " ";
                    lastRight    = word.BoundingRect.Left + word.BoundingRect.Width;
                }

                if (!string.IsNullOrEmpty(currentCell))
                    row.Add(currentCell.Trim());

                if (row.Count > 0)
                    rows.Add(row);
            }

            return rows;
        }

        private static void AppendRow(StringBuilder sb, List<string> row, int maxCols)
        {
            sb.Append("| ");
            for (int i = 0; i < maxCols; i++)
            {
                sb.Append(i < row.Count ? row[i] : "");
                sb.Append(" | ");
            }
            sb.AppendLine();
        }
    }
}
