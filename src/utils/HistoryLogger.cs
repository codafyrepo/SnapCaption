using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;

using SnapCaption.models;

namespace SnapCaption.utils
{
    /// <summary>
    /// In-memory history logger to avoid creating translation_history.db on disk.
    /// </summary>
    public static class SQLiteHistoryLogger
    {
        private static readonly List<TranslationHistoryEntry> History = new();
        private static readonly object HistoryLock = new();

        public static Task LogTranslation(string sourceText, string translatedText,
            string targetLanguage, string apiUsed, CancellationToken token = default)
        {
            var entry = new TranslationHistoryEntry
            {
                Timestamp = DateTime.Now.ToString("MM/dd HH:mm"),
                TimestampFull = DateTime.Now.ToString("MM/dd/yy, HH:mm:ss"),
                SourceText = sourceText,
                TranslatedText = translatedText,
                TargetLanguage = targetLanguage,
                ApiUsed = apiUsed
            };

            lock (HistoryLock)
            {
                History.Add(entry);
            }

            return Task.CompletedTask;
        }

        public static Task<(List<TranslationHistoryEntry>, int)> LoadHistoryAsync(
            int page, int maxRow, string searchText, CancellationToken token = default)
        {
            List<TranslationHistoryEntry> snapshot;
            lock (HistoryLock)
            {
                snapshot = History
                    .Where(h => h.SourceText.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                h.TranslatedText.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(h => h.TimestampFull)
                    .ToList();
            }

            int totalCount = snapshot.Count;
            int maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)maxRow));
            int offset = Math.Max(0, (page - 1) * maxRow);
            var pageData = snapshot.Skip(offset).Take(maxRow).ToList();

            return Task.FromResult((pageData, maxPage));
        }

        public static Task ClearHistory(CancellationToken token = default)
        {
            lock (HistoryLock)
            {
                History.Clear();
            }
            return Task.CompletedTask;
        }

        public static Task<string> LoadLastSourceText(CancellationToken token = default)
        {
            lock (HistoryLock)
            {
                if (History.Count == 0)
                    return Task.FromResult(string.Empty);

                return Task.FromResult(History[^1].SourceText);
            }
        }

        public static Task<TranslationHistoryEntry?> LoadLastTranslation(CancellationToken token = default)
        {
            lock (HistoryLock)
            {
                if (History.Count == 0)
                    return Task.FromResult<TranslationHistoryEntry?>(null);

                return Task.FromResult<TranslationHistoryEntry?>(History[^1]);
            }
        }

        public static Task DeleteLastTranslation(CancellationToken token = default)
        {
            lock (HistoryLock)
            {
                if (History.Count > 0)
                    History.RemoveAt(History.Count - 1);
            }
            return Task.CompletedTask;
        }

        public static async Task ExportToCSV(string filePath, CancellationToken token = default)
        {
            List<TranslationHistoryEntry> snapshot;
            lock (HistoryLock)
            {
                snapshot = History.ToList();
            }

            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csvWriter.WriteRecordsAsync(snapshot, token);
        }
    }
}
