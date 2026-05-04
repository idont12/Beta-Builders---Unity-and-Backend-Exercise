using System;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;
using TMPro;
using UnityEngine;

namespace MiniGames.Task
{
    /// <summary>
    /// Keeps a running total score aligned with Django <c>Profile.score</c>:
    /// loads current server score after login, adds XP earned on each mini-game win,
    /// then PATCHes <c>/api/users/profiles/me/</c> (backend replaces score with sent value).
    /// Hook <see cref="GameManager"/> via optional serialized reference.
    /// </summary>
    public class TaskGameBackendBridge : MonoBehaviour
    {
        const string ProfileMePath = "/api/users/profiles/me/";

        [Header("API")]
        [SerializeField] TaskJwtAuthClient authClient;

        [Header("UI (optional)")]
        [SerializeField] TMP_Text localScoreText;
        [SerializeField] TMP_Text syncErrorText;

        int _totalScore;
        bool _profileLoaded;

        void Awake()
        {
            if (authClient == null) authClient = FindFirstObjectByType<TaskJwtAuthClient>();
        }

        void OnEnable()
        {
            TaskBackendEvents.LoggedIn += OnLoggedIn;
            TaskBackendEvents.LoggedOut += OnLoggedOut;
        }

        void OnDisable()
        {
            TaskBackendEvents.LoggedIn -= OnLoggedIn;
            TaskBackendEvents.LoggedOut -= OnLoggedOut;
        }

        void Start()
        {
            if (!TaskAuthSession.HasRefreshToken) OnLoggedOut();
        }

        void OnLoggedIn()
        {
            ClearError();
            StartCoroutine(LoadProfileRoutine());
        }

        void OnLoggedOut()
        {
            StopAllCoroutines();
            _profileLoaded = false;
            _totalScore = 0;
            UpdateScoreLabel();
        }

        /// <summary>
        /// Called from <see cref="GameManager"/> when the player wins the timed mini-game.
        /// </summary>
        /// <param name="earnedXpRounded">Rounded XP for this win (matches win panel display).</param>
        public void ReportMiniGameWin(int earnedXpRounded)
        {
            if (authClient == null)
            {
                ShowError("Missing TaskJwtAuthClient.");
                return;
            }

            if (!TaskAuthSession.HasRefreshToken)
            {
                ShowError("Not logged in — score not saved to server.");
                return;
            }

            if (earnedXpRounded <= 0)
            {
                Debug.Log("[TaskGameBackendBridge] Earned XP is 0; skipping PATCH.");
                return;
            }

            StartCoroutine(SyncWinRoutine(earnedXpRounded));
        }

        IEnumerator LoadProfileRoutine()
        {
            if (authClient == null) yield break;

            var done = false;
            var ok = false;
            string err = null;
            string body = null;

            authClient.AuthorizedGet(ProfileMePath, (success, error, response) =>
            {
                ok = success;
                err = error;
                body = response;
                done = true;
            });

            while (!done) yield return null;

            if (!ok)
            {
                ShowError(string.IsNullOrEmpty(err) ? "Could not load profile." : err);
                _profileLoaded = false;
                yield break;
            }

            if (!TryParseProfileScore(body, out var score))
            {
                ShowError("Invalid profile response.");
                _profileLoaded = false;
                yield break;
            }

            _totalScore = score;
            _profileLoaded = true;
            UpdateScoreLabel();
            ClearError();
            TaskBackendEvents.RaiseLeaderboardShouldRefresh();
        }

        IEnumerator SyncWinRoutine(int earned)
        {
            if (!_profileLoaded)
                yield return LoadProfileRoutine();

            if (!_profileLoaded) yield break;

            _totalScore += earned;
            UpdateScoreLabel();

            var payload = new Dictionary<string, object> { ["score"] = _totalScore };
            var json = Json.Serialize(payload);

            var done = false;
            var ok = false;
            string err = null;
            string body = null;

            authClient.AuthorizedPatch(ProfileMePath, json, (success, error, response) =>
            {
                ok = success;
                err = error;
                body = response;
                done = true;
            });

            while (!done) yield return null;

            if (!ok)
            {
                ShowError(string.IsNullOrEmpty(err) ? "Could not save score." : err);
                yield break;
            }

            if (!TryParseProfileScore(body, out var serverScore))
            {
                ShowError("Score saved but response was invalid.");
                yield break;
            }

            _totalScore = serverScore;
            UpdateScoreLabel();
            ClearError();
            TaskBackendEvents.RaiseLeaderboardShouldRefresh();
        }

        void UpdateScoreLabel()
        {
            if (localScoreText != null) localScoreText.text = $"Score: {_totalScore}";
        }

        void ShowError(string msg)
        {
            Debug.LogWarning("[TaskGameBackendBridge] " + msg);
            if (syncErrorText != null) syncErrorText.text = msg;
        }

        void ClearError()
        {
            if (syncErrorText != null) syncErrorText.text = string.Empty;
        }

        static bool TryParseProfileScore(string json, out int score)
        {
            score = 0;
            if (string.IsNullOrEmpty(json)) return false;
            if (!(Json.Deserialize(json) is Dictionary<string, object> dict)) return false;
            if (!dict.TryGetValue("score", out var sObj) || sObj == null) return false;

            switch (sObj)
            {
                case int i:
                    score = i;
                    return true;
                case long l:
                    score = (int)l;
                    return true;
                case double d:
                    score = (int)d;
                    return true;
                default:
                    return int.TryParse(sObj.ToString(), out score);
            }
        }
    }
}
