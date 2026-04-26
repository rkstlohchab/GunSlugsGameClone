using System.Collections.Generic;
using GunSlugsClone.Core;
using UnityEngine;

namespace GunSlugsClone.Procedural
{
    public sealed class GeneratedRoom
    {
        public RoomTemplate Template;
        public Vector2 GridPosition;
        public bool IsBoss;
        public bool IsStart;
        public Dictionary<DoorDirection, GeneratedRoom> Connections = new();
    }

    public sealed class GeneratedLevel
    {
        public List<GeneratedRoom> Rooms = new();
        public GeneratedRoom Start;
        public GeneratedRoom Boss;
        public BiomeConfig Biome;
        public int Seed;
    }

    public static class LevelGenerator
    {
        public static GeneratedLevel Generate(BiomeConfig biome, int seed)
        {
            if (biome == null || !biome.IsValid)
                throw new System.ArgumentException("BiomeConfig is null or invalid", nameof(biome));

            var rng = new DeterministicRng(seed);
            var level = new GeneratedLevel { Biome = biome, Seed = seed };

            var occupied = new Dictionary<Vector2, GeneratedRoom>();
            var start = MakeRoom(biome, rng, isStart: true);
            start.GridPosition = Vector2.zero;
            occupied[start.GridPosition] = start;
            level.Start = start;
            level.Rooms.Add(start);

            // Random walk: pick a frontier room with a free door, attach a new room.
            var frontier = new List<GeneratedRoom> { start };
            var target = Mathf.Max(3, biome.RoomsPerRun);
            var safety = target * 12;
            while (level.Rooms.Count < target && safety-- > 0 && frontier.Count > 0)
            {
                var anchor = frontier[rng.NextInt(0, frontier.Count)];
                var freeDir = NextFreeDoor(anchor, occupied, rng);
                if (!freeDir.HasValue) { frontier.Remove(anchor); continue; }

                var dir = freeDir.Value;
                var pos = anchor.GridPosition + DirOffset(dir);
                if (occupied.ContainsKey(pos)) continue;

                // Need a template that has a door pointing back at the anchor.
                var requiredOpposite = OppositeOf(dir);
                var newRoom = MakeRoomMatching(biome, rng, requiredOpposite);
                if (newRoom == null) continue;

                newRoom.GridPosition = pos;
                anchor.Connections[dir] = newRoom;
                newRoom.Connections[requiredOpposite] = anchor;
                occupied[pos] = newRoom;
                level.Rooms.Add(newRoom);
                frontier.Add(newRoom);
            }

            // Pick the room furthest from start as boss room.
            level.Boss = FurthestFrom(level.Start, level.Rooms);
            level.Boss.IsBoss = true;
            return level;
        }

        private static GeneratedRoom MakeRoom(BiomeConfig biome, DeterministicRng rng, bool isStart = false)
        {
            var prefab = biome.RoomTemplates[rng.NextInt(0, biome.RoomTemplates.Count)];
            var rt = prefab.GetComponent<RoomTemplate>();
            return new GeneratedRoom { Template = rt, IsStart = isStart };
        }

        private static GeneratedRoom MakeRoomMatching(BiomeConfig biome, DeterministicRng rng, DoorDirection requiredDoor)
        {
            // Try a handful of templates; if none have the required door, fall back to any.
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var prefab = biome.RoomTemplates[rng.NextInt(0, biome.RoomTemplates.Count)];
                var rt = prefab.GetComponent<RoomTemplate>();
                if (rt != null && rt.HasDoor(requiredDoor))
                    return new GeneratedRoom { Template = rt };
            }
            return null;
        }

        private static DoorDirection? NextFreeDoor(GeneratedRoom room, Dictionary<Vector2, GeneratedRoom> occupied, DeterministicRng rng)
        {
            var candidates = new List<DoorDirection>();
            foreach (var d in room.Template.Doors)
            {
                if (d == null) continue;
                if (room.Connections.ContainsKey(d.Direction)) continue;
                var neighbour = room.GridPosition + DirOffset(d.Direction);
                if (occupied.ContainsKey(neighbour)) continue;
                candidates.Add(d.Direction);
            }
            if (candidates.Count == 0) return null;
            return candidates[rng.NextInt(0, candidates.Count)];
        }

        private static Vector2 DirOffset(DoorDirection d) => d switch
        {
            DoorDirection.North => new Vector2(0, 1),
            DoorDirection.South => new Vector2(0, -1),
            DoorDirection.East  => new Vector2(1, 0),
            _                   => new Vector2(-1, 0),
        };

        private static DoorDirection OppositeOf(DoorDirection d) => d switch
        {
            DoorDirection.North => DoorDirection.South,
            DoorDirection.South => DoorDirection.North,
            DoorDirection.East  => DoorDirection.West,
            _                   => DoorDirection.East,
        };

        private static GeneratedRoom FurthestFrom(GeneratedRoom start, List<GeneratedRoom> rooms)
        {
            // BFS over the connection graph; pick deepest node.
            var depth = new Dictionary<GeneratedRoom, int> { { start, 0 } };
            var queue = new Queue<GeneratedRoom>();
            queue.Enqueue(start);
            GeneratedRoom furthest = start;
            var furthestDepth = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var d = depth[node];
                if (d > furthestDepth) { furthestDepth = d; furthest = node; }
                foreach (var kv in node.Connections)
                {
                    if (depth.ContainsKey(kv.Value)) continue;
                    depth[kv.Value] = d + 1;
                    queue.Enqueue(kv.Value);
                }
            }
            return furthest;
        }
    }
}
