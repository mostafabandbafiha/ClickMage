// IStatHolder.cs
namespace ClickMage.Stats
{
    public interface IStatHolder
    {
        bool HasStat(string statKey);
        BaseStat GetStat(string statKey);
        void AddStat(BaseStat stat);

        float GetStatValue(string statKey);
        void SetStatBaseValue(string statKey, float value);

        void AddModifier(string statKey, StatModifier modifier);
        void RemoveModifier(string statKey, StatModifier modifier);
        void RemoveModifiersFromSource(string source);
    }
}
