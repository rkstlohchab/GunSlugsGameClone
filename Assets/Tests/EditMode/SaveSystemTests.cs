using GunSlugsClone.Core;
using NUnit.Framework;
using UnityEngine;

namespace GunSlugsClone.Tests
{
    public class SaveSystemTests
    {
        [Test]
        public void RoundTrip_ViaJsonUtility_PreservesFields()
        {
            var d = new SaveData
            {
                Currency = 250,
                HighScore = 999,
                MasterVolume = 0.42f,
                ScreenShakeEnabled = false,
                SelectedCharacterId = "char_test"
            };
            d.UnlockedCharacters.Add("char_test");
            d.UnlockedAchievements.Add("ach_first_kill");

            var json = JsonUtility.ToJson(d);
            var restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(250, restored.Currency);
            Assert.AreEqual(999, restored.HighScore);
            Assert.AreEqual(0.42f, restored.MasterVolume, 0.0001f);
            Assert.IsFalse(restored.ScreenShakeEnabled);
            Assert.AreEqual("char_test", restored.SelectedCharacterId);
            CollectionAssert.Contains(restored.UnlockedCharacters, "char_test");
            CollectionAssert.Contains(restored.UnlockedAchievements, "ach_first_kill");
        }

        [Test]
        public void DefaultData_HasStarterUnlocks()
        {
            var d = new SaveData();
            Assert.AreEqual(SaveData.CurrentSchemaVersion, d.SchemaVersion);
            CollectionAssert.Contains(d.UnlockedCharacters, "char_default");
            CollectionAssert.Contains(d.UnlockedWeapons, "weapon_pistol");
        }
    }
}
