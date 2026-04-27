using System;
using System.Collections.Generic;

namespace GunSlugsClone.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var key = typeof(T);
            _handlers[key] = _handlers.TryGetValue(key, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var key = typeof(T);
            if (!_handlers.TryGetValue(key, out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) _handlers.Remove(key);
            else _handlers[key] = remaining;
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(evt);
        }

        public static void ClearAll() => _handlers.Clear();
    }

    public readonly struct PlayerSpawnedEvent { public readonly int PlayerIndex; public PlayerSpawnedEvent(int i) { PlayerIndex = i; } }
    public readonly struct PlayerDamagedEvent { public readonly int PlayerIndex; public readonly int Damage; public readonly int RemainingHealth; public PlayerDamagedEvent(int i, int d, int r) { PlayerIndex = i; Damage = d; RemainingHealth = r; } }
    public readonly struct PlayerDiedEvent { public readonly int PlayerIndex; public PlayerDiedEvent(int i) { PlayerIndex = i; } }
    public readonly struct EnemyKilledEvent { public readonly string EnemyId; public readonly int ScoreReward; public readonly UnityEngine.Vector3 Position; public EnemyKilledEvent(string id, int r, UnityEngine.Vector3 p) { EnemyId = id; ScoreReward = r; Position = p; } }
    public readonly struct WeaponSwappedEvent { public readonly int PlayerIndex; public readonly string WeaponId; public WeaponSwappedEvent(int i, string id) { PlayerIndex = i; WeaponId = id; } }
    public readonly struct RoomEnteredEvent { public readonly int RoomIndex; public readonly bool IsBossRoom; public RoomEnteredEvent(int idx, bool boss) { RoomIndex = idx; IsBossRoom = boss; } }
    public readonly struct RoomClearedEvent { public readonly int RoomIndex; public RoomClearedEvent(int i) { RoomIndex = i; } }
    public readonly struct BiomeCompletedEvent { public readonly string BiomeId; public BiomeCompletedEvent(string id) { BiomeId = id; } }
    public readonly struct RunCompletedEvent { public readonly int FinalScore; public readonly float DurationSeconds; public RunCompletedEvent(int s, float d) { FinalScore = s; DurationSeconds = d; } }
    public readonly struct AllWavesClearedEvent { public readonly int WaveCount; public AllWavesClearedEvent(int c) { WaveCount = c; } }
    public readonly struct PauseToggledEvent { public readonly bool IsPaused; public PauseToggledEvent(bool p) { IsPaused = p; } }
    public readonly struct CurrencyChangedEvent { public readonly int NewTotal; public readonly int Delta; public CurrencyChangedEvent(int t, int d) { NewTotal = t; Delta = d; } }
}
