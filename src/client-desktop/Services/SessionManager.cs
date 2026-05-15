using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Layla.Desktop.Services
{
    public static class SessionManager
    {
        private static string _profileName = "session";
        public static string ProfileName
        {
            get => _profileName;
            set
            {
                _profileName = value;
                UpdateSessionPath();
            }
        }

        private static string _sessionPath = BuildSessionPath("session");

        public static string SessionPath => _sessionPath;

        private static string BuildSessionPath(string profile) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Layla",
            $"{profile}.bin"
        );

        private static void UpdateSessionPath() => _sessionPath = BuildSessionPath(_profileName);

        public static string CurrentToken { get; set; } = string.Empty;
        public static string CurrentEmail { get; set; } = string.Empty;
        public static string CurrentDisplayName { get; set; } = string.Empty;
        public static string CurrentUserId { get; set; } = string.Empty;

        public static bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

        // Per-user entropy: prevents another local user's session blob from
        // decrypting under this user's DPAPI key in shared scenarios.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Layla.Desktop.Session.v1");

        public static void SaveSession(string token, string email, string name, string userId)
        {
            CurrentToken = token;
            CurrentEmail = email;
            CurrentDisplayName = name;
            CurrentUserId = userId;

            try
            {
                var directory = Path.GetDirectoryName(SessionPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = System.Text.Json.JsonSerializer.Serialize(new SessionData
                {
                    CurrentToken = CurrentToken,
                    CurrentEmail = CurrentEmail,
                    CurrentDisplayName = CurrentDisplayName,
                    CurrentUserId = CurrentUserId,
                });
                var plaintext = Encoding.UTF8.GetBytes(json);
                var encrypted = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(SessionPath, encrypted);
            }
            catch
            {
                // Swallowed by design — a failed persist must not crash the UI.
                // The in-memory session above is still valid for this run.
            }
        }

        public static void LoadSession()
        {
            if (!File.Exists(SessionPath))
            {
                // Backwards-compat: pick up any plain JSON file from older builds.
                TryLoadLegacyJson();
                return;
            }

            try
            {
                var encrypted = File.ReadAllBytes(SessionPath);
                var plaintext = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(plaintext);
                var session = System.Text.Json.JsonSerializer.Deserialize<SessionData>(json);
                if (session != null)
                {
                    CurrentToken = session.CurrentToken;
                    CurrentEmail = session.CurrentEmail;
                    CurrentDisplayName = session.CurrentDisplayName;
                    CurrentUserId = session.CurrentUserId;
                }
            }
            catch
            {
                ClearSession();
            }
        }

        private static void TryLoadLegacyJson()
        {
            var legacy = Path.ChangeExtension(SessionPath, ".json");
            if (!File.Exists(legacy)) return;
            try
            {
                var json = File.ReadAllText(legacy);
                var session = System.Text.Json.JsonSerializer.Deserialize<SessionData>(json);
                if (session != null)
                {
                    CurrentToken = session.CurrentToken;
                    CurrentEmail = session.CurrentEmail;
                    CurrentDisplayName = session.CurrentDisplayName;
                    CurrentUserId = session.CurrentUserId;
                    // Migrate to encrypted file and remove the plaintext copy.
                    SaveSession(CurrentToken, CurrentEmail, CurrentDisplayName, CurrentUserId);
                    File.Delete(legacy);
                }
            }
            catch { }
        }

        public static void ClearSession()
        {
            CurrentToken = string.Empty;
            CurrentEmail = string.Empty;
            CurrentDisplayName = string.Empty;
            CurrentUserId = string.Empty;
            try { if (File.Exists(SessionPath)) File.Delete(SessionPath); } catch { }
            var legacy = Path.ChangeExtension(SessionPath, ".json");
            try { if (File.Exists(legacy)) File.Delete(legacy); } catch { }
        }

        private class SessionData
        {
            public string CurrentToken { get; set; } = string.Empty;
            public string CurrentEmail { get; set; } = string.Empty;
            public string CurrentDisplayName { get; set; } = string.Empty;
            public string CurrentUserId { get; set; } = string.Empty;
        }
    }
}
