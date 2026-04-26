#if UNITY_IOS
using System.Threading.Tasks;
using UnityEngine;

namespace GunSlugsClone.Services
{
    // Stub. Real implementation in M6: depends on Apple Unity Plugins (GameKit).
    // Install via Unity Package Manager from https://github.com/apple/unityplugins
    // Then replace the bodies below with calls to Apple.GameKit.GKLocalPlayer / GKLeaderboard / GKAchievement.
    public sealed class GameCenterLeaderboards : ILeaderboardService
    {
        public bool IsAuthenticated { get; private set; }

        public async Task<bool> AuthenticateAsync()
        {
            // var local = await GKLocalPlayer.Local.Authenticate();
            // IsAuthenticated = local.IsAuthenticated;
            // return IsAuthenticated;
            await Task.Yield();
            Debug.LogWarning("[GameCenter] Apple Unity Plugins not yet installed; auth stub returning false.");
            return false;
        }

        public async Task<bool> SubmitScoreAsync(string leaderboardId, long score)
        {
            await Task.Yield();
            Debug.Log($"[GameCenter] STUB submit {score} to {leaderboardId}");
            return false;
        }

        public async Task<bool> UnlockAchievementAsync(string achievementId, double percentComplete = 100.0)
        {
            await Task.Yield();
            Debug.Log($"[GameCenter] STUB unlock {achievementId} ({percentComplete}%)");
            return false;
        }

        public void ShowLeaderboardUI(string leaderboardId)
            => Debug.Log($"[GameCenter] STUB show leaderboard {leaderboardId}");

        public void ShowAchievementsUI()
            => Debug.Log("[GameCenter] STUB show achievements");
    }
}
#endif
