using System.Collections;
using System.Collections.Generic;
using System.Text;
using MiniJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MiniGames.Task
{
    /// <summary>
    /// Fetches public <c>GET /api/users/leaderboard/</c> and renders top 10 (username + score).
    /// Place one instance on the login screen panel and optionally another in-game.
    /// </summary>
    public class TaskLeaderboardPanel : MonoBehaviour
    {
        [Header("API")]
        [SerializeField] TaskJwtAuthClient authClient;
        [SerializeField] string baseUrlFallback = "http://127.0.0.1:8000";

        [Header("UI")]
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button refreshButton;

        [Header("Behaviour")]
        [SerializeField] bool refreshOnEnable = true;
        [SerializeField, Min(1)] int topCount = 10;

        void Awake()
        {
            if (authClient == null) authClient = FindFirstObjectByType<TaskJwtAuthClient>();
            if (refreshButton != null) refreshButton.onClick.AddListener(Refresh);
        }

        void OnEnable()
        {
            TaskBackendEvents.LeaderboardShouldRefresh += Refresh;
            if (refreshOnEnable) Refresh();
        }

        void OnDisable()
        {
            TaskBackendEvents.LeaderboardShouldRefresh -= Refresh;
        }

        public void Refresh()
        {
            StopAllCoroutines();
            StartCoroutine(FetchRoutine());
        }

        string ResolveBaseUrl()
        {
            if (authClient != null) return authClient.BaseUrl;
            return baseUrlFallback.TrimEnd('/');
        }

        IEnumerator FetchRoutine()
        {
            var count = Mathf.Max(1, topCount);
            var url = $"{ResolveBaseUrl()}/api/users/leaderboard/?limit={count}&ordering=-score,user__username";
            using var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                if (bodyText != null) bodyText.text = $"Leaderboard error: {req.error}";
                yield break;
            }

            var json = req.downloadHandler.text;
            if (!TryExtractRows(json, out var list))
            {
                if (bodyText != null) bodyText.text = "Leaderboard: invalid JSON.";
                yield break;
            }

            var sb = new StringBuilder();
            var rank = 1;
            foreach (var item in list)
            {
                if (item is not Dictionary<string, object> row) continue;
                row.TryGetValue("username", out var uObj);
                row.TryGetValue("score", out var sObj);
                var name = uObj?.ToString() ?? "?";
                var scoreStr = FormatScore(sObj);
                sb.AppendLine($"{rank}. {name} — {scoreStr}");
                rank++;
                if (rank > count) break;
            }

            if (bodyText != null)
                bodyText.text = sb.Length == 0 ? "No entries yet." : sb.ToString().TrimEnd();
        }

        static string FormatScore(object sObj)
        {
            if (sObj == null) return "0";
            return sObj switch
            {
                int i => i.ToString(),
                long l => l.ToString(),
                double d => ((int)d).ToString(),
                _ => sObj.ToString()
            };
        }

        static bool TryExtractRows(string json, out List<object> rows)
        {
            rows = null;
            if (string.IsNullOrEmpty(json)) return false;
            var parsed = Json.Deserialize(json);
            switch (parsed)
            {
                case List<object> list:
                    rows = list;
                    return true;
                case Dictionary<string, object> dict when dict.TryGetValue("results", out var resultsObj) && resultsObj is List<object> results:
                    rows = results;
                    return true;
                default:
                    return false;
            }
        }
    }
}
