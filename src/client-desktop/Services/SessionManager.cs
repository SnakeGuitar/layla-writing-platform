using System;
using System.IO;

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

        private static string _sessionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Layla",
            "session.json"
        );

        public static string SessionPath => _sessionPath;

        private static void UpdateSessionPath()
        {
            _sessionPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Layla",
                $"{_profileName}.json"
            );
        }

        public static string CurrentToken { get; set; } = string.Empty;
        public static string CurrentEmail { get; set; } = string.Empty;
        public static string CurrentDisplayName { get; set; } = string.Empty;
        public static string CurrentUserId { get; set; } = string.Empty;

        public static bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

        public static void SaveSession(string token, string email, string name, string userId)
        {
            CurrentToken = token;
            CurrentEmail = email;
            CurrentDisplayName = name;
            CurrentUserId = userId;

            try
            {
                var directory = Path.GetDirectoryName(SessionPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

                var json = System.Text.Json.JsonSerializer.Serialize(new { CurrentToken, CurrentEmail, CurrentDisplayName, CurrentUserId });
                File.WriteAllText(SessionPath, json);
            }
            catch { /* Redundancy/Logging */ }
        }

        public static void LoadSession()
        {
            if (!File.Exists(SessionPath)) return;

            try
            {
                var json = File.ReadAllText(SessionPath);
                var session = System.Text.Json.JsonSerializer.Deserialize<SessionData>(json);
                if (session != null)
                {
                    CurrentToken = session.CurrentToken;
                    CurrentEmail = session.CurrentEmail;
                    CurrentDisplayName = session.CurrentDisplayName;
                    CurrentUserId = session.CurrentUserId;
                }
            }
            catch { /* Clear corrupted session */ ClearSession(); }
        }

        public static void ClearSession()
        {
            CurrentToken = string.Empty;
            CurrentEmail = string.Empty;
            CurrentDisplayName = string.Empty;
            CurrentUserId = string.Empty;
            if (File.Exists(SessionPath)) File.Delete(SessionPath);
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