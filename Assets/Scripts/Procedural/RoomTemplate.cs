using System.Collections.Generic;
using UnityEngine;

namespace GunSlugsClone.Procedural
{
    public sealed class RoomTemplate : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string templateId = "room_unset";
        [SerializeField] private string biomeTag = "biome_unset";

        [Header("Geometry")]
        [SerializeField] private Vector2 size = new(20f, 12f);
        [SerializeField] private List<DoorSocket> doors = new();
        [SerializeField] private List<Transform> enemySpawns = new();
        [SerializeField] private List<Transform> pickupSpawns = new();
        [SerializeField] private Transform playerSpawn;

        public string TemplateId => templateId;
        public string BiomeTag => biomeTag;
        public Vector2 Size => size;
        public IReadOnlyList<DoorSocket> Doors => doors;
        public IReadOnlyList<Transform> EnemySpawns => enemySpawns;
        public IReadOnlyList<Transform> PickupSpawns => pickupSpawns;
        public Transform PlayerSpawn => playerSpawn;

        public DoorSocket FindDoor(DoorDirection dir)
        {
            for (var i = 0; i < doors.Count; i++)
                if (doors[i].Direction == dir) return doors[i];
            return null;
        }

        public bool HasDoor(DoorDirection dir) => FindDoor(dir) != null;

        public ValidationResult Validate()
        {
            var issues = new List<string>();
            if (string.IsNullOrEmpty(templateId) || templateId == "room_unset")
                issues.Add("templateId not set");
            if (doors == null || doors.Count == 0)
                issues.Add("no DoorSockets configured");
            else
            {
                var seen = new HashSet<DoorDirection>();
                foreach (var d in doors)
                {
                    if (d == null) { issues.Add("null DoorSocket in list"); continue; }
                    if (!seen.Add(d.Direction)) issues.Add($"duplicate door direction: {d.Direction}");
                }
            }
            if (playerSpawn == null) issues.Add("playerSpawn not assigned");
            return new ValidationResult(issues.Count == 0, issues);
        }

        public readonly struct ValidationResult
        {
            public readonly bool Ok;
            public readonly IReadOnlyList<string> Issues;
            public ValidationResult(bool ok, IReadOnlyList<string> issues) { Ok = ok; Issues = issues; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
