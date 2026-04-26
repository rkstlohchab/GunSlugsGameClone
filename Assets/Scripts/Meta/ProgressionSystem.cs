using System;
using GunSlugsClone.Core;
using UnityEngine;

namespace GunSlugsClone.Meta
{
    public static class ProgressionSystem
    {
        public static event Action<int> CurrencyChanged;

        public static int Currency => SaveSystem.Data.Currency;

        public static void AddCurrency(int amount)
        {
            if (amount == 0) return;
            SaveSystem.Data.Currency = Mathf.Max(0, SaveSystem.Data.Currency + amount);
            SaveSystem.Save();
            EventBus.Publish(new CurrencyChangedEvent(SaveSystem.Data.Currency, amount));
            CurrencyChanged?.Invoke(SaveSystem.Data.Currency);
        }

        public static bool TryUnlockCharacter(CharacterData character)
        {
            if (character == null) return false;
            var d = SaveSystem.Data;
            if (d.UnlockedCharacters.Contains(character.Id)) return true;
            if (d.Currency < character.UnlockCost) return false;
            d.Currency -= character.UnlockCost;
            d.UnlockedCharacters.Add(character.Id);
            SaveSystem.Save();
            EventBus.Publish(new CurrencyChangedEvent(d.Currency, -character.UnlockCost));
            return true;
        }

        public static bool IsCharacterUnlocked(string characterId)
            => SaveSystem.Data.UnlockedCharacters.Contains(characterId);

        public static bool IsWeaponUnlocked(string weaponId)
            => SaveSystem.Data.UnlockedWeapons.Contains(weaponId);

        public static void UnlockWeapon(string weaponId)
        {
            var list = SaveSystem.Data.UnlockedWeapons;
            if (!list.Contains(weaponId))
            {
                list.Add(weaponId);
                SaveSystem.Save();
            }
        }
    }
}
