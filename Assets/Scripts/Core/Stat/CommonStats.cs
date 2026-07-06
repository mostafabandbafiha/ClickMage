using UnityEngine;

namespace ClickMage.Stats
{
    public static class CommonStats
    {
        // Factory Stats
        public const string ProductionSpeed = "Factory.ProductionSpeed";
        public const string ProductionEfficiency = "Factory.ProductionEfficiency";
        public const string StorageCapacity = "Factory.StorageCapacity";
        public const string ConcurrentCrafts = "Factory.ConcurrentCrafts";
        public const string FactoryLevel = "Factory.Level";

        // Entity Stats
        public const string Health = "Entity.Health";
        public const string MaxHealth = "Entity.MaxHealth";
        public const string Defense = "Entity.Defense";
        public const string Attack = "Entity.Attack";
        public const string HarvestPower = "Entity.HarvestPower";
        public const string MaxEngagers = "Entity.MaxEngagers";
        public const string AttackRange = "Entity.AttackRange";
        public const string AttackCooldown = "Entity.AttackCooldown";
        public const string AreaRadius = "Entity.AreaRadius";
        public const string MoveSpeed = "Entity.MoveSpeed";
        public const string Damage = "Entity.Damage";
        public const string StaysOutAtNight = "Entity.StaysOutAtNight";
        public const string DetectionRadius = "Entity.DetectionRadius";

        public const string ArmorPiercing = "Entity.ArmorPiercing";
        public const string SlowAmount = "Entity.SlowAmount";
        public const string FireDamage = "Entity.FireDamage";
        public const string FrostDamage = "Entity.FrostDamage";
        public const string LightningDamage = "Entity.LightningDamage";
        public const string BleedDamage = "Entity.BleedDamage";
        public const string AttackSpeed = "Entity.AttackSpeed";
        public const string HasPoison = "Entity.HasPoison";


        // ── NEW: on-hit status effects ──
        public const string FireOnHitDamage = "Entity.FireOnHitDamage";
        public const string FireOnHitDuration = "Entity.FireOnHitDuration";
        public const string FireOnHitTick = "Entity.FireOnHitTick";

        public const string BleedOnHitDamage = "Entity.BleedOnHitDamage";
        public const string BleedOnHitDuration = "Entity.BleedOnHitDuration";
        public const string BleedOnHitTick = "Entity.BleedOnHitTick";

        // ── NEW: instant on-damage modifiers ──
        public const string ReflectPercent = "Entity.ReflectPercent";
        public const string LifestealPercent = "Entity.LifestealPercent";

        // ── NEW: armor shred (Piercing Tip — applies to TARGET, stacking) ──
        public const string Armor = "Entity.Armor";
        public const string ArmorShredPerHit = "Entity.ArmorShredPerHit";
        public const string ArmorShredMaxStacks = "Entity.ArmorShredMaxStacks";
        public const string ArmorShredDuration = "Entity.ArmorShredDuration";

        // passive stats
        public const string Invisibility = "Item.Invisibility";

        // New character need stats
        public static readonly string Energy = "Character.Energy";
        public static readonly string MaxEnergy = "Character.MaxEnergy";
        public static readonly string Hunger = "Character.Hunger";
        public static readonly string MaxHunger = "Character.MaxHunger";
        public static readonly string GatheringDesire = "Character.GatheringDesire";
        public static readonly string RestDesire = "Character.RestDesire";

        // Tower Stats
        public const string Range = "Tower.Range";
        
        public const string FireRate = "Tower.FireRate";
        public const string ChainCount = "Tower.chainCount";
        public const string ChainRange = "Tower.chainRange";
        public const string ChainDamageMultiplier = "Tower.chainDamageMultiplier";


        // ── New: Woodworking (and any future subclass bonus stats) ─
        public const string BonusOutputChance = "bonus_output_chance";
        public const string BonusOutputMultiplier = "bonus_output_multiplier";

        // ── Resource Node ───────────────────────────────────────────────────
        public const string RegenRate = "ResourceNode.RegenRate";

        public static readonly Color Normal = Color.white;
        public static readonly Color Fire = new Color(1f, 0.85f, 0.1f);
        public static readonly Color Bleed = new Color(0.85f, 0.1f, 0.1f);
        public static readonly Color Reflect = new Color(0.6f, 0.6f, 1f);

        public static Color GetStatColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return Fire;
                case DamageType.Bleed: return Bleed;
                case DamageType.Reflect: return Reflect;
                default: return Normal;
            }
        }

        // Helper methods
        public static float GetDefaultValue(string statKey)
        {
            return statKey switch
            {
                ProductionSpeed => 1.0f,
                ProductionEfficiency => 1.0f,
                StorageCapacity => 100f,
                ConcurrentCrafts => 1f,
                FactoryLevel => 1f,
                Health => 100f,
                MaxHealth => 100f,
                Defense => 10f,
                Attack => 20f,
                RegenRate => 1f,
                Range => 10,
                Damage => 10,
                FireRate => 1,
                HarvestPower => 10,
                _ => 0f
            };
        }

        public static string GetDisplayName(string statKey)
        {
            return statKey switch
            {
                ProductionSpeed => "Production Speed",
                ProductionEfficiency => "Production Efficiency",
                StorageCapacity => "Storage Capacity",
                ConcurrentCrafts => "Concurrent Crafts",
                FactoryLevel => "Factory Level",
                Health => "HP",
                MaxHealth => "MHP",
                Defense => "Defense",
                Attack => "Att",
                RegenRate => "RR",
                Range => "ATR",
                Damage => "DMG",
                FireRate => "Fire Rate",
                HarvestPower => "Harvest Power",
                _ => statKey
            };
        }
    }
}
