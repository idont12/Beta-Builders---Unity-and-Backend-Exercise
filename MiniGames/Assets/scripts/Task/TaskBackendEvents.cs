using System;

namespace MiniGames.Task
{
    /// <summary>
    /// Lightweight events so login UI, score sync, and leaderboard can stay decoupled.
    /// </summary>
    public static class TaskBackendEvents
    {
        public static event Action LoggedIn;
        public static event Action LoggedOut;
        public static event Action LeaderboardShouldRefresh;

        public static void RaiseLoggedIn() => LoggedIn?.Invoke();

        public static void RaiseLoggedOut() => LoggedOut?.Invoke();

        public static void RaiseLeaderboardShouldRefresh() => LeaderboardShouldRefresh?.Invoke();
    }
}
