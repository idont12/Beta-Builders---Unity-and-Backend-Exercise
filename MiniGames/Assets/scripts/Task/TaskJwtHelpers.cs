using System;
using System.Collections.Generic;
using System.Text;
using MiniJSON;

namespace MiniGames.Task
{
    public static class TaskJwtHelpers
    {
        /// <summary>
        /// Reads the "exp" claim (seconds since Unix epoch) from a JWT access token, if present.
        /// </summary>
        public static bool TryGetAccessExpiryUtc(string jwt, out DateTime expiryUtc)
        {
            expiryUtc = default;
            if (string.IsNullOrEmpty(jwt)) return false;

            var parts = jwt.Split('.');
            if (parts.Length < 2) return false;

            var payload = parts[1];
            string padded = payload.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(padded);
            }
            catch
            {
                return false;
            }

            var json = Encoding.UTF8.GetString(bytes);
            if (!(Json.Deserialize(json) is Dictionary<string, object> dict)) return false;
            if (!dict.TryGetValue("exp", out var expObj) || expObj == null) return false;

            long expUnix;
            switch (expObj)
            {
                case long l:
                    expUnix = l;
                    break;
                case int i:
                    expUnix = i;
                    break;
                case double d:
                    expUnix = (long)d;
                    break;
                default:
                    return false;
            }

            expiryUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            return true;
        }
    }
}
