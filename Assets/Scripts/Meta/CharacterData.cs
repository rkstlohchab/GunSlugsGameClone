using UnityEngine;

namespace GunSlugsClone.Meta
{
    [CreateAssetMenu(menuName = "GunSlugs/Character Data", fileName = "char_")]
    public sealed class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "char_unset";
        public string DisplayName = "Unset";
        public string FlavourText = "";
        public Sprite Portrait;
        public Sprite InGameSprite;
        public RuntimeAnimatorController AnimatorController;

        [Header("Stats Modifiers")]
        [Range(0.5f, 2f)] public float MoveSpeedMultiplier = 1f;
        [Range(0.5f, 2f)] public float MaxHealthMultiplier = 1f;
        [Range(0.5f, 2f)] public float FireRateMultiplier = 1f;
        [Range(0.5f, 2f)] public float DamageMultiplier = 1f;
        [Min(0)] public int ExtraJumpCount = 0;
        public bool StartsWithDash = true;

        [Header("Unlock")]
        public bool UnlockedByDefault = false;
        [Min(0)] public int UnlockCost = 1000;
        public string UnlockHint = "";

        [Header("Loadout")]
        public string StartingWeaponId = "weapon_pistol";
    }
}
