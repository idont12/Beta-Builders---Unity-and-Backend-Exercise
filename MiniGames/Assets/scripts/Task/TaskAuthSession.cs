using System;
using UnityEngine;

namespace MiniGames.Task
{
    /// <summary>
    /// Persists JWT access/refresh tokens for the Django SimpleJWT backend.
    /// Logout is local-only (clears stored tokens); no server call required.
    /// </summary>
    public static class TaskAuthSession
    {
        const string PrefsAccess = "task_jwt_access";
        const string PrefsRefresh = "task_jwt_refresh";
        const string PrefsAccessExpiryUnix = "task_jwt_access_exp_unix";

        public static bool HasRefreshToken => !string.IsNullOrEmpty(GetRefreshToken());

        public static string GetAccessToken() => PlayerPrefs.GetString(PrefsAccess, string.Empty);

        public static string GetRefreshToken() => PlayerPrefs.GetString(PrefsRefresh, string.Empty);

        public static DateTime? GetAccessExpiryUtc()
        {
            if (!PlayerPrefs.HasKey(PrefsAccessExpiryUnix)) return null;
            var unix = PlayerPrefs.GetInt(PrefsAccessExpiryUnix, 0);
            if (unix <= 0) return null;
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        }

        public static void SaveTokens(string access, string refresh, DateTime? accessExpiryUtc)
        {
            PlayerPrefs.SetString(PrefsAccess, access ?? string.Empty);
            PlayerPrefs.SetString(PrefsRefresh, refresh ?? string.Empty);
            if (accessExpiryUtc.HasValue)
            {
                var seconds = new DateTimeOffset(accessExpiryUtc.Value).ToUnixTimeSeconds();
                if (seconds > int.MaxValue) seconds = int.MaxValue;
                PlayerPrefs.SetInt(PrefsAccessExpiryUnix, (int)seconds);
            }
            else
            {
                PlayerPrefs.DeleteKey(PrefsAccessExpiryUnix);
            }

            PlayerPrefs.Save();
        }

        public static void UpdateAccessToken(string access, DateTime? accessExpiryUtc)
        {
            PlayerPrefs.SetString(PrefsAccess, access ?? string.Empty);
            if (accessExpiryUtc.HasValue)
            {
                var seconds = new DateTimeOffset(accessExpiryUtc.Value).ToUnixTimeSeconds();
                if (seconds > int.MaxValue) seconds = int.MaxValue;
                PlayerPrefs.SetInt(PrefsAccessExpiryUnix, (int)seconds);
            }
            else
            {
                PlayerPrefs.DeleteKey(PrefsAccessExpiryUnix);
            }

            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsAccess);
            PlayerPrefs.DeleteKey(PrefsRefresh);
            PlayerPrefs.DeleteKey(PrefsAccessExpiryUnix);
            PlayerPrefs.Save();
        }
    }
}
