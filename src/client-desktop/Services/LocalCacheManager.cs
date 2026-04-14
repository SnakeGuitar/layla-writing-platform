using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Layla.Desktop.Services
{
    public class LocalCacheManager
    {
        private readonly string _cacheDirectory;
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

        public LocalCacheManager()
        {
            string tempPath = Path.GetTempPath();
            _cacheDirectory = Path.Combine(tempPath, "LaylaLocalCache");

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        /// <summary>
        /// Saves the manuscript content locally simulating a "Last Write Wins" offline policy.
        /// Thread-safe via SemaphoreSlim to prevent file locking across quick typing strokes.
        /// </summary>
        public async Task SaveManuscriptAsync(string manuscriptId, string content)
        {
            string filePath = Path.Combine(_cacheDirectory, $"{manuscriptId}.rtf");

            await _fileSemaphore.WaitAsync();
            try
            {
                await File.WriteAllTextAsync(filePath, content);
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        /// <summary>
        /// Loads the most recent cached manuscript string.
        /// </summary>
        public async Task<string> LoadManuscriptAsync(string manuscriptId)
        {
            string filePath = Path.Combine(_cacheDirectory, $"{manuscriptId}.rtf");

            await _fileSemaphore.WaitAsync();
            try
            {
                if (File.Exists(filePath))
                {
                    return await File.ReadAllTextAsync(filePath);
                }
                return string.Empty;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }
    }
}
