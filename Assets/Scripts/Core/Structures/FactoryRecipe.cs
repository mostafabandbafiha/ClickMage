// FactoryRecipe.cs
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

namespace ClickMage.Factories
{
    [CreateAssetMenu(
        fileName = "FactoryRecipe",
        menuName = "ClickMage/Factories/Recipe")]
    public class FactoryRecipe : ScriptableObject
    {
        // -------------------------------------------------------
        // Nested types
        // -------------------------------------------------------

        [System.Serializable]
        public class InputRequirement
        {
            public ItemData resource;
            public int amount;
        }

        [System.Serializable]
        public class OutputProduction
        {
            public ItemData resource;
            public int baseAmount;
            public float productionTime = 1f;
        }

        [System.Serializable]
        public class StatRequirement
        {
            public string statKey;
            public float requiredValue;
            public ComparisonType comparison = ComparisonType.GreaterOrEqual;

            public enum ComparisonType
            {
                GreaterOrEqual,
                Equal,
                LessOrEqual
            }

            // Moved onto the requirement itself - cleaner than doing it in CheckStatRequirements
            public bool IsMet(float currentValue) => comparison switch
            {
                ComparisonType.GreaterOrEqual => currentValue >= requiredValue,
                ComparisonType.Equal => Mathf.Approximately(currentValue, requiredValue),
                ComparisonType.LessOrEqual => currentValue <= requiredValue,
                _ => false
            };
        }

        // -------------------------------------------------------
        // Inspector fields
        // -------------------------------------------------------

        [Header("Recipe Information")]
        public string recipeName;
        [TextArea]
        public string description;

        [Header("Input Requirements")]
        public List<InputRequirement> inputs = new List<InputRequirement>();

        [Header("Output Production")]
        public List<OutputProduction> outputs = new List<OutputProduction>();

        [Header("Base Production Stats")]
        [Min(0.1f)]
        public float baseCraftTime = 5f;
        public int baseMaxConcurrentCrafts = 1;

        [Header("Stat Requirements")]
        public List<StatRequirement> statRequirements = new List<StatRequirement>();

        // -------------------------------------------------------
        // Computed values
        // -------------------------------------------------------

        /// <summary>Actual craft time after speed modifier. Speed > 1 = faster.</summary>
        public float GetCraftTime(float speedMultiplier = 1f) =>
            Mathf.Max(0.1f, baseCraftTime / Mathf.Max(0.001f, speedMultiplier));

        /// <summary>Actual output amount for a specific resource after efficiency.</summary>
        public int GetOutputAmount(ItemData resource, float efficiencyMultiplier = 1f)
        {
            if (resource == null) return 0;

            foreach (var output in outputs)
                if (output.resource == resource)
                    return Mathf.Max(0, Mathf.RoundToInt(output.baseAmount * efficiencyMultiplier));

            return 0;
        }

        /// <summary>Actual max concurrent crafts after capacity modifier.</summary>
        public int GetMaxConcurrentCrafts(float capacityMultiplier = 1f) =>
            Mathf.Max(1, Mathf.RoundToInt(baseMaxConcurrentCrafts * capacityMultiplier));

        // -------------------------------------------------------
        // Input queries
        // -------------------------------------------------------

        public bool HasInputOfType(ItemData type)
        {
            foreach (var input in inputs)
                if (input.resource != null && input.resource == type)
                    return true;
            return false;
        }

        public int GetInputAmount(ItemData type)
        {
            foreach (var input in inputs)
                if (input.resource != null && input.resource == type)
                    return input.amount;
            return 0;
        }

        // -------------------------------------------------------
        // Stat requirements
        // -------------------------------------------------------

        /// <summary>Returns true if all stat requirements pass, or if statHolder is null.</summary>
        public bool CheckStatRequirements(IStatHolder statHolder)
        {
            // Null holder = no stat system yet, treat as passed
            if (statHolder == null) return true;

            foreach (var req in statRequirements)
            {
                // Missing stat = treat as 0 (requirement fails unless requiredValue <= 0)
                float current = statHolder.HasStat(req.statKey)
                    ? statHolder.GetStatValue(req.statKey)
                    : 0f;

                if (!req.IsMet(current))
                    return false;
            }

            return true;
        }

        // -------------------------------------------------------
        // Aggregated lookups (useful for UI)
        // -------------------------------------------------------

        /// <summary>Total input cost by ResourceType, for UI display.</summary>
        public Dictionary<ItemData, int> GetTotalInputCosts()
        {
            var costs = new Dictionary<ItemData, int>();
            foreach (var input in inputs)
            {
                if (input.resource == null) continue;
                var type = input.resource;
                costs.TryGetValue(type, out int existing);
                costs[type] = existing + input.amount;
            }
            return costs;
        }

        /// <summary>Total output by ResourceType, scaled by efficiency.</summary>
        public Dictionary<ItemData, int> GetTotalOutputs(float efficiencyMultiplier = 1f)
        {
            var result = new Dictionary<ItemData, int>();
            foreach (var output in outputs)
            {
                if (output.resource == null) continue;
                var type = output.resource;
                int amount = GetOutputAmount(output.resource, efficiencyMultiplier);
                result.TryGetValue(type, out int existing);
                result[type] = existing + amount;
            }
            return result;
        }

        // -------------------------------------------------------
        // Validation (editor safety)
        // -------------------------------------------------------

        private void OnValidate()
        {
            foreach (var input in inputs)
                if (input.amount < 1) input.amount = 1;

            foreach (var output in outputs)
                if (output.baseAmount < 1) output.baseAmount = 1;
        }
    }
}
