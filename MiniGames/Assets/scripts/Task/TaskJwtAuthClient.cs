using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace MiniGames.Task
{
    /// <summary>
    /// Calls Django REST JWT endpoints: login (/api/token/) and refresh (/api/token/refresh/).
    /// Attach to a GameObject in the scene (e.g. same object as TaskLoginLogoutUI).
    /// </summary>
    public class TaskJwtAuthClient : MonoBehaviour
    {
        [SerializeField] string baseUrl = "http://127.0.0.1:8000";

        public string BaseUrl => baseUrl.TrimEnd('/');

        public void SetBaseUrl(string url) => baseUrl = url;

        public void Login(string username, string password, Action<bool, string> onComplete)
        {
            StartCoroutine(LoginRoutine(username, password, onComplete));
        }

        public void RefreshIfPossible(Action<bool, string> onComplete)
        {
            StartCoroutine(RefreshRoutine(onComplete));
        }

        /// <summary>
        /// Public registration: creates User + Profile on the server (no JWT returned).
        /// On success, call <see cref="Login"/> with the same credentials to obtain tokens.
        /// </summary>
        public void Register(string username, string password, Action<bool, string> onComplete)
        {
            StartCoroutine(RegisterRoutine(username, password, onComplete));
        }

        /// <summary>
        /// GET with Bearer access token. On 401, refreshes access token once and retries.
        /// </summary>
        /// <param name="path">Absolute path from host, e.g. /api/users/profiles/me/</param>
        /// <param name="onComplete">(success, errorMessage, responseBody)</param>
        public void AuthorizedGet(string path, Action<bool, string, string> onComplete)
        {
            StartCoroutine(AuthorizedRequest("GET", path, null, onComplete));
        }

        /// <summary>
        /// PATCH with JSON body and Bearer access token. On 401, refreshes once and retries.
        /// </summary>
        public void AuthorizedPatch(string path, string jsonBody, Action<bool, string, string> onComplete)
        {
            StartCoroutine(AuthorizedRequest("PATCH", path, jsonBody, onComplete));
        }

        IEnumerator LoginRoutine(string username, string password, Action<bool, string> onComplete)
        {
            var payload = new Dictionary<string, object>
            {
                ["username"] = username ?? string.Empty,
                ["password"] = password ?? string.Empty
            };
            var json = Json.Serialize(payload);
            using var req = new UnityWebRequest($"{BaseUrl}/api/token/", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                onComplete?.Invoke(false, req.error ?? "Network error");
                yield break;
            }

            var body = req.downloadHandler.text;
            if (!TryParseTokenPair(body, out var access, out var refresh, out var error))
            {
                onComplete?.Invoke(false, error ?? body);
                yield break;
            }

            DateTime? exp = null;
            if (TaskJwtHelpers.TryGetAccessExpiryUtc(access, out var expUtc)) exp = expUtc;

            TaskAuthSession.SaveTokens(access, refresh, exp);
            onComplete?.Invoke(true, string.Empty);
        }

        IEnumerator RegisterRoutine(string username, string password, Action<bool, string> onComplete)
        {
            var payload = new Dictionary<string, object>
            {
                ["username"] = username ?? string.Empty,
                ["password"] = password ?? string.Empty
            };
            var json = Json.Serialize(payload);
            using var req = new UnityWebRequest($"{BaseUrl}/api/users/register/", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            var body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
#if UNITY_2020_1_OR_NEWER
            var transportOk = req.result == UnityWebRequest.Result.Success;
#else
            var transportOk = !(req.isNetworkError || req.isHttpError);
#endif

            if (!transportOk)
            {
                var msg = FormatDrfError(body);
                if (string.IsNullOrEmpty(msg))
                    msg = string.IsNullOrEmpty(req.error) ? "Registration failed." : req.error;
                onComplete?.Invoke(false, msg);
                yield break;
            }

            onComplete?.Invoke(true, string.Empty);
        }

        IEnumerator RefreshRoutine(Action<bool, string> onComplete)
        {
            var refresh = TaskAuthSession.GetRefreshToken();
            if (string.IsNullOrEmpty(refresh))
            {
                onComplete?.Invoke(false, "No refresh token");
                yield break;
            }

            var payload = new Dictionary<string, object> { ["refresh"] = refresh };
            var json = Json.Serialize(payload);
            using var req = new UnityWebRequest($"{BaseUrl}/api/token/refresh/", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                onComplete?.Invoke(false, req.error ?? "Network error");
                yield break;
            }

            var body = req.downloadHandler.text;
            if (!TryParseRefreshResponse(body, out var access, out var error))
            {
                onComplete?.Invoke(false, error ?? body);
                yield break;
            }

            DateTime? exp = null;
            if (TaskJwtHelpers.TryGetAccessExpiryUtc(access, out var expUtc)) exp = expUtc;

            TaskAuthSession.UpdateAccessToken(access, exp);
            onComplete?.Invoke(true, string.Empty);
        }

        IEnumerator AuthorizedRequest(
            string method,
            string path,
            string jsonBody,
            Action<bool, string, string> onComplete)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("/"))
            {
                onComplete?.Invoke(false, "Path must start with /", null);
                yield break;
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var url = $"{BaseUrl}{path}";
                var token = TaskAuthSession.GetAccessToken();

                UnityWebRequest req;
                if (method == "GET")
                {
                    req = UnityWebRequest.Get(url);
                    req.SetRequestHeader("Authorization", $"Bearer {token}");
                }
                else if (method == "PATCH")
                {
                    req = new UnityWebRequest(url, "PATCH");
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody ?? "{}"));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.SetRequestHeader("Authorization", $"Bearer {token}");
                }
                else
                {
                    onComplete?.Invoke(false, "Unsupported HTTP method", null);
                    yield break;
                }

                yield return req.SendWebRequest();

                var body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                var code = req.responseCode;
#if UNITY_2020_1_OR_NEWER
                var transportOk = req.result == UnityWebRequest.Result.Success;
#else
                var transportOk = !(req.isNetworkError || req.isHttpError);
#endif
                var errMsg = transportOk ? null : (req.error ?? $"HTTP {code}");

                if (code == 401 && attempt == 0)
                {
                    req.Dispose();

                    var refreshOk = false;
                    string refreshErr = null;
                    yield return RefreshRoutine((ok, err) =>
                    {
                        refreshOk = ok;
                        refreshErr = err;
                    });

                    if (!refreshOk)
                    {
                        onComplete?.Invoke(false, refreshErr ?? "Session expired", body);
                        yield break;
                    }

                    continue;
                }

                req.Dispose();

                if (!transportOk)
                {
                    onComplete?.Invoke(false, errMsg ?? "Request failed", body);
                    yield break;
                }

                onComplete?.Invoke(true, null, body);
                yield break;
            }
        }

        static bool TryParseTokenPair(string json, out string access, out string refresh, out string error)
        {
            access = null;
            refresh = null;
            error = null;

            if (string.IsNullOrEmpty(json)) { error = "Empty response"; return false; }
            if (!(Json.Deserialize(json) is Dictionary<string, object> dict))
            {
                error = "Invalid JSON";
                return false;
            }

            if (dict.TryGetValue("detail", out var detailObj) && detailObj != null)
            {
                error = detailObj.ToString();
                return false;
            }

            if (!dict.TryGetValue("access", out var aObj) || aObj == null)
            {
                error = "Missing access token";
                return false;
            }

            access = aObj.ToString();
            if (dict.TryGetValue("refresh", out var rObj) && rObj != null) refresh = rObj.ToString();
            return !string.IsNullOrEmpty(access);
        }

        static bool TryParseRefreshResponse(string json, out string access, out string error)
        {
            access = null;
            error = null;

            if (string.IsNullOrEmpty(json)) { error = "Empty response"; return false; }
            if (!(Json.Deserialize(json) is Dictionary<string, object> dict))
            {
                error = "Invalid JSON";
                return false;
            }

            if (dict.TryGetValue("detail", out var detailObj) && detailObj != null)
            {
                error = detailObj.ToString();
                return false;
            }

            if (!dict.TryGetValue("access", out var aObj) || aObj == null)
            {
                error = "Missing access token";
                return false;
            }

            access = aObj.ToString();
            return !string.IsNullOrEmpty(access);
        }

        static string FormatDrfError(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            if (!(Json.Deserialize(json) is Dictionary<string, object> dict)) return null;

            if (dict.TryGetValue("detail", out var detail) && detail != null)
                return detail.ToString();

            var sb = new StringBuilder();
            foreach (var kv in dict)
            {
                switch (kv.Value)
                {
                    case List<object> list:
                        foreach (var item in list)
                            sb.AppendLine($"{kv.Key}: {item}");
                        break;
                    default:
                        sb.AppendLine($"{kv.Key}: {kv.Value}");
                        break;
                }
            }

            var s = sb.ToString().Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }
}
