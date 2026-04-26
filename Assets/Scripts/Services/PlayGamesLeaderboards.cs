#if UNITY_ANDROID
using System.Threading.Tasks;
using UnityEngine;

namespace GunSlugsClone.Services
{
    // Stub. Real implementation in M6: depends on Google Play Games Plugin for Unity.
    // Install via .unitypackage from https://github.com/playgameservices/play-games-plugin-for-unity
    // Then replace the bodies below with PlayGamesPlatform.Instance calls.
    public sealed class PlayGamesLeaderboards : ILeaderboardService
    {
        public bool IsAuthenticated { get; private set; }

        public Task<bool> AuthenticateAsync()
        {
            // PlayGamesPlatform.Activate();
            // var tcs = new TaskCompletionSource<bool>();
            // PlayGamesPlatform.Instance.Authenticate(status => { IsAuthenticated = status == SignInStatus.Success; tcs.SetResult(IsAuthenticated); });
            // return tcs.Task;
            Debug.LogWarning("[PlayGames] Plugin not yet installed; auth stub returning false.");
            return Task.FromResult(false);
        }

        public Task<bool> SubmitScoreAsync(string leaderboardId, long score)
        {
            Debug.Log($"[PlayGames] STUB submit {score} to {leaderboardId}");
            return Task.FromResult(false);
        }

        public Task<bool> UnlockAchievementAsync(string achievementId, double percentComplete = 100.0)
        {
            Debug.Log($"[PlayGames] STUB unlock {achievementId} ({percentComplete}%)");
            return Task.FromResult(false);
        }

        public void ShowLeaderboardUI(string leaderboardId)
            => Debug.Log($"[PlayGames] STUB show leaderboard {leaderboardId}");

        public void ShowAchievementsUI()
            => Debug.Log("[PlayGames] STUB show achievements");
    }
}
#endif
