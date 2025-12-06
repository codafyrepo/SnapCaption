using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Automation;

using SnapCaption.models;
using SnapCaption.utils;

namespace SnapCaption
{
    public static class Translator
    {
        private static AutomationElement? window = null;
        private static Caption? caption = null;
        private static Setting? setting = null;

        private static readonly Queue<string> pendingTextQueue = new();
        private static readonly TranslationTaskQueue translationTaskQueue = new();

        public static AutomationElement? Window
        {
            get => window;
            set => window = value;
        }
        public static Caption? Caption => caption;
        public static Setting? Setting => setting;

        public static bool LogOnlyFlag { get; set; } = false;
        public static bool FirstUseFlag { get; set; } = false;
        public static bool IsExiting { get; set; } = false;
        private static bool pauseUpdates = false;

        private static readonly object debugLockObject = new();
        private const string DEBUG_FILENAME = "debug.txt";

        public static event Action? TranslationLogged;

        static Translator()
        {
            window = LiveCaptionsHandler.LaunchLiveCaptions();
            LiveCaptionsHandler.FixLiveCaptions(Window);

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), models.Setting.FILENAME)))
                FirstUseFlag = true;

            caption = Caption.GetInstance();
            setting = Setting.Load();

            if (!Setting.MainWindow.NativeCaptionsVisible)
                LiveCaptionsHandler.HideLiveCaptions(Window);
        }

        public static void ClearAllCaptions()
        {
            pauseUpdates = true;
            
            if (caption != null)
            {
                caption.AccumulatedHistory = string.Empty;
                caption.DisplayTranslatedCaption = string.Empty;
                caption.DisplayOriginalCaption = string.Empty;
                caption.OriginalCaption = string.Empty;
                caption.TranslatedCaption = string.Empty;
                caption.Contexts.Clear();
                caption.OnPropertyChanged("DisplayContexts");
            }
            
            lock (pendingTextQueue)
            {
                pendingTextQueue.Clear();
            }
            
            Task.Run(async () =>
            {
                await Task.Delay(500);
                pauseUpdates = false;
            });
        }

        public static void SyncLoop()
        {
            int idleCount = 0;
            int syncCount = 0;

            while (!IsExiting)
            {
                if (pauseUpdates)
                {
                    Thread.Sleep(100);
                    continue;
                }
                
                if (Window == null)
                {
                    if (IsExiting) break;
                    Thread.Sleep(2000);
                    continue;
                }

                string fullText = string.Empty;
                try
                {
                    var info = Window.Current;
                    var name = info.Name;
                    fullText = LiveCaptionsHandler.GetCaptions(Window);
                }
                catch (ElementNotAvailableException)
                {
                    Window = null;
                    continue;
                }
                if (string.IsNullOrEmpty(fullText))
                    continue;

                fullText = RegexPatterns.Acronym().Replace(fullText, "$1$2");
                fullText = RegexPatterns.AcronymWithWords().Replace(fullText, "$1 $2");
                fullText = RegexPatterns.PunctuationSpace().Replace(fullText, "$1 ");
                fullText = RegexPatterns.CJPunctuationSpace().Replace(fullText, "$1");
                fullText = TextUtil.ReplaceNewlines(fullText, TextUtil.MEDIUM_THRESHOLD);

                if (fullText.IndexOfAny(TextUtil.PUNC_EOS) == -1 && Caption.Contexts.Count > 0)
                {
                    Caption.Contexts.Clear();
                    Caption.OnPropertyChanged("DisplayContexts");
                }

                int lastEOSIndex;
                if (Array.IndexOf(TextUtil.PUNC_EOS, fullText[^1]) != -1)
                    lastEOSIndex = fullText[0..^1].LastIndexOfAny(TextUtil.PUNC_EOS);
                else
                    lastEOSIndex = fullText.LastIndexOfAny(TextUtil.PUNC_EOS);
                string latestCaption = fullText.Substring(lastEOSIndex + 1);

                if (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(latestCaption) < TextUtil.SHORT_THRESHOLD)
                {
                    lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(TextUtil.PUNC_EOS);
                    latestCaption = fullText.Substring(lastEOSIndex + 1);
                }


                if (string.CompareOrdinal(Caption.DisplayOriginalCaption, latestCaption) != 0)
                {
                    Caption.DisplayOriginalCaption = latestCaption;
                    Caption.DisplayOriginalCaption =
                        TextUtil.ShortenDisplaySentence(Caption.DisplayOriginalCaption, TextUtil.VERYLONG_THRESHOLD);
                }

                int lastEOS = latestCaption.LastIndexOfAny(TextUtil.PUNC_EOS);
                if (lastEOS != -1)
                    latestCaption = latestCaption.Substring(0, lastEOS + 1);
                if (string.CompareOrdinal(Caption.OriginalCaption, latestCaption) != 0)
                {
                    Caption.OriginalCaption = latestCaption;

                    idleCount = 0;
                    if (Array.IndexOf(TextUtil.PUNC_EOS, Caption.OriginalCaption[^1]) != -1)
                    {
                        syncCount = 0;
                        pendingTextQueue.Enqueue(Caption.OriginalCaption);
                    }
                    else if (Encoding.UTF8.GetByteCount(Caption.OriginalCaption) >= TextUtil.SHORT_THRESHOLD)
                        syncCount++;
                }
                else
                    idleCount++;

                if (syncCount > Setting.MaxSyncInterval ||
                    idleCount == Setting.MaxIdleInterval)
                {
                    syncCount = 0;
                    pendingTextQueue.Enqueue(Caption.OriginalCaption);
                }

                Thread.Sleep(25);
            }
        }

        public static async Task TranslateLoop()
        {
            while (!IsExiting)
            {
                if (Window == null)
                {
                    if (IsExiting) break;
                    Caption.DisplayTranslatedCaption = "[WARNING] LiveCaptions was unexpectedly closed, restarting...";
                    Window = LiveCaptionsHandler.LaunchLiveCaptions();
                    Caption.DisplayTranslatedCaption = "";
                }

                if (pendingTextQueue.Count > 0)
                {
                    var originalSnapshot = pendingTextQueue.Dequeue();

                    if (!Setting.TranslateEnabled)
                    {
                        // When translation is disabled, pass through the original text without calling API
                        translationTaskQueue.Enqueue(token => Task.FromResult(
                            (originalSnapshot, Array.IndexOf(TextUtil.PUNC_EOS, originalSnapshot[^1]) != -1)
                        ), originalSnapshot);
                    }
                    else if (LogOnlyFlag)
                    {
                        bool isOverwrite = await IsOverwrite(originalSnapshot);
                        await LogOnly(originalSnapshot, isOverwrite);
                    }
                    else
                    {
                        translationTaskQueue.Enqueue(token => Task.Run(
                            () => Translate(originalSnapshot, token), token), originalSnapshot);
                    }
                }

                Thread.Sleep(40);
            }
        }

        public static async Task DisplayLoop()
        {
            while (true)
            {
                var (translatedText, isChoke) = translationTaskQueue.Output;

                if (LogOnlyFlag)
                {
                    Caption.TranslatedCaption = string.Empty;
                    Caption.DisplayTranslatedCaption = "[Paused]";
                }
                else if (!string.IsNullOrEmpty(RegexPatterns.NoticePrefix().Replace(
                             translatedText, string.Empty).Trim()) &&
                         string.CompareOrdinal(Caption.TranslatedCaption, translatedText) != 0)
                {
                    Caption.TranslatedCaption = translatedText;
                    Caption.DisplayTranslatedCaption =
                        TextUtil.ShortenDisplaySentence(Caption.TranslatedCaption, TextUtil.VERYLONG_THRESHOLD);
                }

                if (isChoke)
                    Thread.Sleep(720);
                Thread.Sleep(40);
            }
        }

        public static async Task<(string, bool)> Translate(string text, CancellationToken token = default)
        {
            string translatedText;
            bool isChoke = Array.IndexOf(TextUtil.PUNC_EOS, text[^1]) != -1;
            
            try
            {
                var sw = Setting.MainWindow.DebugMode ? Stopwatch.StartNew() : null;
                translatedText = await TranslateAPI.TranslateFunction(text, token);
                translatedText = translatedText.Replace("🔤", "");
                if (sw != null)
                {
                    sw.Stop();
                    DebugLog($"[Translation] Latency: {sw.ElapsedMilliseconds}ms | Text: {text}");
                }
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Translation Failed: {ex.Message}");
                DebugLog($"[ERROR] Translation Failed: {ex.Message}");
                return ($"[ERROR] Translation Failed: {ex.Message}", isChoke);
            }

            return (translatedText, isChoke);
        }

        public static async Task Log(string originalText, string translatedText,
            bool isOverwrite = false, CancellationToken token = default)
        {
            string targetLanguage, apiName;

            // Check if in passthrough mode (translation disabled, original = translated)
            bool isPassthrough = !Setting.TranslateEnabled && originalText == translatedText;

            if (isPassthrough)
            {
                targetLanguage = "N/A";
                apiName = "Passthrough";
            }
            else if (Setting != null)
            {
                targetLanguage = Setting.TargetLanguage;
                apiName = Setting.ApiName;
            }
            else
            {
                targetLanguage = "N/A";
                apiName = "N/A";
            }

            try
            {
                if (isOverwrite)
                    await SQLiteHistoryLogger.DeleteLastTranslation(token);
                await SQLiteHistoryLogger.LogTranslation(originalText, translatedText, targetLanguage, apiName);
                TranslationLogged?.Invoke();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Logging History Failed: {ex.Message}");
            }
        }

        public static async Task LogOnly(string originalText,
            bool isOverwrite = false, CancellationToken token = default)
        {
            try
            {
                if (isOverwrite)
                    await SQLiteHistoryLogger.DeleteLastTranslation(token);
                await SQLiteHistoryLogger.LogTranslation(originalText, "N/A", "N/A", "LogOnly");
                TranslationLogged?.Invoke();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Logging History Failed: {ex.Message}");
            }
        }

        public static async Task LogPassthrough(string originalText,
            bool isOverwrite = false, CancellationToken token = default)
        {
            try
            {
                if (isOverwrite)
                    await SQLiteHistoryLogger.DeleteLastTranslation(token);
                await SQLiteHistoryLogger.LogTranslation(originalText, originalText, "N/A", "Passthrough");
                TranslationLogged?.Invoke();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Logging History Failed: {ex.Message}");
            }
        }

        public static async Task AddLogCard(CancellationToken token = default)
        {
            var lastLog = await SQLiteHistoryLogger.LoadLastTranslation(token);
            if (lastLog == null)
                return;

            if (Caption?.Contexts.Count >= Setting?.MainWindow.CaptionLogMax)
                Caption.Contexts.Dequeue();
            Caption?.Contexts.Enqueue(lastLog);
            Caption?.OnPropertyChanged("DisplayContexts");

            if (Caption != null)
            {
                string newEntry = $"{lastLog.TranslatedText.Trim()}\n";
                Caption.AccumulatedHistory += newEntry;
            }
        }

        public static async Task<bool> IsOverwrite(string originalText, CancellationToken token = default)
        {
            string lastOriginalText = await SQLiteHistoryLogger.LoadLastSourceText(token);
            if (lastOriginalText == null)
                return false;
            double similarity = TextUtil.Similarity(originalText, lastOriginalText);
            return similarity > 0.66;
        }

        public static void DebugLog(string message)
        {
            if (!Setting?.MainWindow.DebugMode ?? true)
                return;

            try
            {
                lock (debugLockObject)
                {
                    string logPath = Path.Combine(Directory.GetCurrentDirectory(), DEBUG_FILENAME);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                    File.AppendAllText(logPath, logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to write debug log: {ex.Message}");
            }
        }
    }
}
