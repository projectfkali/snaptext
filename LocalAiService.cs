using System;
using System.IO;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace SnapText
{
    /// <summary>
    /// Gömülü SmolLM2-360M modelini kullanarak yerel AI işlemleri yapar.
    /// İlk çağrıda model belleğe yüklenir; sonrasında anında yanıt verir.
    /// </summary>
    public class LocalAiService : IDisposable
    {
        // Uygulama ile birlikte gelen yeni Llama-3.2 model dosyası
        public static readonly string ModelPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Models", "llama-3.2-1b.gguf");

        private LLamaWeights?      _weights;
        private StatelessExecutor? _executor;
        private ModelParams?       _parameters;
        private bool               _initialized;

        public bool IsModelPresent => File.Exists(ModelPath);

        // ─── Model yükleme (sadece bir kez çalışır) ─────────────────────────
        public async Task<bool> EnsureInitializedAsync(Action<string>? onProgress = null)
        {
            if (_initialized) return true;

            if (!IsModelPresent)
            {
                onProgress?.Invoke("Hata: Model dosyası bulunamadı!");
                return false;
            }

            onProgress?.Invoke("AI Modeli Yükleniyor... (İlk kullanım, ~5 sn)");
            try
            {
                await Task.Run(() =>
                {
                    _parameters = new ModelParams(ModelPath) { ContextSize = 2048, GpuLayerCount = 0 };
                    _weights    = LLamaWeights.LoadFromFile(_parameters);
                    _executor   = new StatelessExecutor(_weights, _parameters);
                });

                _initialized = true;
                return true;
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"Model Yükleme Hatası: {ex.Message}");
                return false;
            }
        }

        // ─── Metin üretimi ───────────────────────────────────────────────────
        public async Task<string> ProcessTextAsync(string prompt, string text, Action<string>? onProgress = null)
        {
            bool ready = await EnsureInitializedAsync(onProgress);
            if (!ready) return "Model yüklenemedi.";

            onProgress?.Invoke("...");

            // Llama-3.2-Instruct format
            string fullPrompt =
                $"<|begin_of_text|><|start_header_id|>system<|end_header_id|>\n\n" +
                $"You are a highly intelligent and accurate assistant. Respond perfectly in the requested language.<|eot_id|>" +
                $"<|start_header_id|>user<|end_header_id|>\n\n" +
                $"{prompt}\n\nTEXT:\n{text}<|eot_id|>" +
                $"<|start_header_id|>assistant<|end_header_id|>\n\n";

            var inferParams = new InferenceParams
            {
                MaxTokens   = 1024,
                AntiPrompts = new[] { "<|eot_id|>", "<|start_header_id|>" }
            };

            var result = new System.Text.StringBuilder();

            await Task.Run(async () =>
            {
                await foreach (var token in _executor!.InferAsync(fullPrompt, inferParams))
                {
                    result.Append(token);
                    onProgress?.Invoke(result.ToString());
                }
            });

            return result.ToString().Trim();
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage, Action<string>? onProgress = null)
        {
            bool ready = await EnsureInitializedAsync(onProgress);
            if (!ready) return "Model yüklenemedi.";

            onProgress?.Invoke("...");

            string prompt = $"Translate the following text to {targetLanguage}. Output ONLY the direct translation, do NOT add any introductions, comments, or quotes. The user needs the strict translation.";
            string fullPrompt =
                $"<|begin_of_text|><|start_header_id|>system<|end_header_id|>\n\n" +
                $"You are a professional machine translation system. Provide ONLY the precise translation.<|eot_id|>" +
                $"<|start_header_id|>user<|end_header_id|>\n\n" +
                $"{prompt}\n\nTEXT:\n{text}<|eot_id|>" +
                $"<|start_header_id|>assistant<|end_header_id|>\n\n";

            var inferParams = new InferenceParams
            {
                MaxTokens   = 1024,
                AntiPrompts = new[] { "<|eot_id|>", "<|start_header_id|>" }
            };

            var result = new System.Text.StringBuilder();
            
            await Task.Run(async () =>
            {
                await foreach (var token in _executor!.InferAsync(fullPrompt, inferParams))
                {
                    result.Append(token);
                    onProgress?.Invoke(result.ToString());
                }
            });

            return result.ToString().Trim();
        }

        public void Dispose()
        {
            _weights?.Dispose();
            _weights     = null;
            _executor    = null;
            _initialized = false;
        }
    }
}
