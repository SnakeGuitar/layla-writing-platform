using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Layla.Desktop.Services
{
    /// <summary>
    /// File-based offline cache for chapter RTF content.
    /// Used by the manuscript editor as a fallback when the API is unreachable
    /// so in-flight writing is not lost on network errors or app crashes.
    /// </summary>
    public class LocalCacheManager
    {
        private readonly string _cacheDirectory;
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

        public LocalCacheManager()
        {
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Layla",
                "chapter-cache");

            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }

        private string GetChapterPath(string manuscriptId, string chapterId)
            => Path.Combine(_cacheDirectory, $"{manuscriptId}_{chapterId}.rtf");

        /// <summary>
        /// Writes the chapter's RTF content to the local cache.
        /// Thread-safe via SemaphoreSlim to prevent file locking across rapid typing.
        /// </summary>
        public async Task SaveChapterAsync(string manuscriptId, string chapterId, string content)
        {
            var filePath = GetChapterPath(manuscriptId, chapterId);

            await _fileSemaphore.WaitAsync();
            try
            {
                await File.WriteAllTextAsync(filePath, content);
            }
            catch { /* Best-effort offline persistence — never crash the editor */ }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        /// <summary>
        /// Loads cached RTF content for the given chapter, or <c>null</c> if no cache exists.
        /// </summary>
        public async Task<string?> LoadChapterAsync(string manuscriptId, string chapterId)
        {
            var filePath = GetChapterPath(manuscriptId, chapterId);

            await _fileSemaphore.WaitAsync();
            try
            {
                return File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : null;
            }
            catch
            {
                return null;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        /// <summary>
        /// Deletes the cached copy of a chapter after a successful server save,
        /// so the cache only contains unsaved work.
        /// </summary>
        public void ClearChapter(string manuscriptId, string chapterId)
        {
            try
            {
                var filePath = GetChapterPath(manuscriptId, chapterId);
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch { /* Best-effort */ }
        }
    }
}
