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
        /// <summary>
        /// Raised after login and from leaderboard "Play Again" — <see cref="GameManager"/> starts the default JSON run.
        /// </summary>
        public static event Action PlayDefaultGameRequested;

        public static void RaiseLoggedIn() => LoggedIn?.Invoke();

        public static void RaiseLoggedOut() => LoggedOut?.Invoke();

        public static void RaiseLeaderboardShouldRefresh() => LeaderboardShouldRefresh?.Invoke();

        public static void RaisePlayDefaultGameRequested() => PlayDefaultGameRequested?.Invoke();
    }
}
