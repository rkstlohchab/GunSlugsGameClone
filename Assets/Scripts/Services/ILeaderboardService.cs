using System;
using System.Threading.Tasks;

namespace GunSlugsClone.Services
{
    public interface ILeaderboardService
    {
        bool IsAuthenticated { get; }
        Task<bool> AuthenticateAsync();
        Task<bool> SubmitScoreAsync(string leaderboardId, long score);
        Task<bool> UnlockAchievementAsync(string achievementId, double percentComplete = 100.0);
        void ShowLeaderboardUI(string leaderboardId);
        void ShowAchievementsUI();
    }

    public static class LeaderboardServiceFactory
    {
        private static ILeaderboardService _instance;
        public static ILeaderboardService Get()
        {
            if (_instance != null) return _instance;
#if UNITY_IOS && !UNITY_EDITOR
            _instance = new GameCenterLeaderboards();
#elif UNITY_ANDROID && !UNITY_EDITOR
            _instance = new PlayGamesLeaderboards();
#else
            _instance = new NullLeaderboardService();
#endif
            return _instance;
        }
    }

    public sealed class NullLeaderboardService : ILeaderboardService
    {
        public bool IsAuthenticated => false;
        public Task<bool> AuthenticateAsync() => Task.FromResult(false);
        public Task<bool> SubmitScoreAsync(string leaderboardId, long score)
        {
            UnityEngine.Debug.Log($"[Leaderboards/Null] would submit {score} to '{leaderboardId}'");
            return Task.FromResult(true);
        }
        public Task<bool> UnlockAchievementAsync(string achievementId, double percentComplete = 100.0)
        {
            UnityEngine.Debug.Log($"[Leaderboards/Null] would unlock '{achievementId}' ({percentComplete}%)");
            return Task.FromResult(true);
        }
        public void ShowLeaderboardUI(string leaderboardId) => UnityEngine.Debug.Log($"[Leaderboards/Null] show '{leaderboardId}'");
        public void ShowAchievementsUI() => UnityEngine.Debug.Log("[Leaderboards/Null] show achievements");
    }
}
