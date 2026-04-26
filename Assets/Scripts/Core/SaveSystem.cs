using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GunSlugsClone.Core
{
    [Serializable]
    public sealed class SaveData
    {
        public int SchemaVersion = CurrentSchemaVersion;
        public int Currency;
        public int HighScore;
        public List<string> UnlockedCharacters = new() { "char_default" };
        public List<string> UnlockedWeapons = new() { "weapon_pistol" };
        public List<string> UnlockedAchievements = new();
        public string SelectedCharacterId = "char_default";
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float SfxVolume = 1f;
        public bool ScreenShakeEnabled = true;

        public const int CurrentSchemaVersion = 1;
    }

    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private static SaveData _data;
        public static SaveData Data => _data ??= new SaveData();

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => FilePath + ".tmp";

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) { _data = new SaveData(); return; }
                var json = File.ReadAllText(FilePath);
                var loaded = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                _data = Migrate(loaded);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e}. Falling back to defaults.");
                _data = new SaveData();
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonUtility.ToJson(Data, prettyPrint: true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(TempPath, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e}");
            }
        }

        public static void DeleteAll()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            _data = new SaveData();
        }

        private static SaveData Migrate(SaveData loaded)
        {
            if (loaded.SchemaVersion == SaveData.CurrentSchemaVersion) return loaded;
            // Future migrations: switch on loaded.SchemaVersion and patch fields up to CurrentSchemaVersion.
            loaded.SchemaVersion = SaveData.CurrentSchemaVersion;
            return loaded;
        }
    }
}
