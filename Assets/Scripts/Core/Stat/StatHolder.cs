// StatHolder.cs
using System.Collections.Generic;
using UnityEngine;

namespace ClickMage.Stats
{
    public class StatHolder : IStatHolder
    {
        private readonly Dictionary<string, BaseStat> stats =
            new Dictionary<string, BaseStat>();

        public bool HasStat(string statKey) =>
            stats.ContainsKey(statKey);

        public IEnumerable<BaseStat> GetAllStats() => stats.Values;

        public BaseStat GetStat(string statKey)
        {
            stats.TryGetValue(statKey, out var stat);
            return stat;
        }

        public void AddStat(BaseStat stat)
        {
            if (stat == null) return;

            if (stats.ContainsKey(stat.StatKey))
            {
                Debug.LogWarning($"[StatHolder] '{stat.StatKey}' already exists, skipping.");
                return;
            }

            stats[stat.StatKey] = stat;
        }

        public float GetStatValue(string statKey)
        {
            if (stats.TryGetValue(statKey, out var stat))
                return stat.GetValue();

            Debug.LogWarning($"[StatHolder] Stat '{statKey}' not found.");
            return 0f;
        }

        public void SetStatBaseValue(string statKey, float value)
        {
            if (stats.TryGetValue(statKey, out var stat))
                stat.BaseValue = value;
            else
                Debug.LogWarning($"[StatHolder] Stat '{statKey}' not found.");
        }

        public void AddModifier(string statKey, StatModifier modifier)
        {
            if (!stats.TryGetValue(statKey, out var stat))
            {
                stat = BaseStat.CreateRuntime(statKey, 0f);
                stats[statKey] = stat;
            }
            stat.AddModifier(modifier);
        }

        public void RemoveModifier(string statKey, StatModifier modifier)
        {
            if (stats.TryGetValue(statKey, out var stat))
                stat.RemoveModifier(modifier);
        }

        public void RemoveModifiersFromSource(string source)
        {
            foreach (var stat in stats.Values)
                stat.RemoveModifiersFromSource(source);
        }
    }
}
