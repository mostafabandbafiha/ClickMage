// StatusEffectInstance.cs
public class StatusEffectInstance
{
    public string EffectId;
    public string SourceId;

    // Damage-over-time mode
    public float DamagePerTick;
    public float TickInterval;

    // Generic stat-modifier mode (e.g. armor shred)
    public string ModifiedStatKey;     // null if this is a pure DoT
    public float ModifierValuePerStack;
    public int Stacks = 1;
    public int MaxStacks = 1;

    public float TimeRemaining;
    public float TickTimer;

    public bool IsStatModifierEffect => ModifiedStatKey != null;
}