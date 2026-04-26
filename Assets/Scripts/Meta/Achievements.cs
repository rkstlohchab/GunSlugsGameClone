using GunSlugsClone.Core;
using GunSlugsClone.Services;
using UnityEngine;

namespace GunSlugsClone.Meta
{
    public static class Achievements
    {
        public const string FirstKill = "ach_first_kill";
        public const string FirstBoss = "ach_first_boss";
        public const string Run100Score = "ach_score_100";
        public const string FullClear = "ach_full_clear";

        private static bool _wired;

        public static void Initialise()
        {
            if (_wired) return;
            _wired = true;
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<BiomeCompletedEvent>(OnBiomeCompleted);
            EventBus.Subscribe<RunCompletedEvent>(OnRunCompleted);
        }

        private static void OnEnemyKilled(EnemyKilledEvent _) => Unlock(FirstKill);
        private static void OnBiomeCompleted(BiomeCompletedEvent _) => Unlock(FirstBoss);
        private static void OnRunCompleted(RunCompletedEvent e)
        {
            if (e.FinalScore >= 100) Unlock(Run100Score);
        }

        private static async void Unlock(string id)
        {
            var d = SaveSystem.Data;
            if (d.UnlockedAchievements.Contains(id)) return;
            d.UnlockedAchievements.Add(id);
            SaveSystem.Save();
            await LeaderboardServiceFactory.Get().UnlockAchievementAsync(id);
            Debug.Log($"[Achievements] unlocked {id}");
        }
    }
}
