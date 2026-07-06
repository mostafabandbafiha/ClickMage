// CharacterNeed.cs
using UnityEngine;

public enum NeedType
{
    Energy,
    Hunger,
    Social,
    Entertainment
}

[System.Serializable]
public class CharacterNeed
{
    public NeedType Type;
    public float CurrentValue;
    public float MaxValue;
    public float DecayRate; // How fast this need decreases per second
    public float CriticalThreshold; // Below this, need becomes urgent (0-1)

    public float NormalizedValue => MaxValue > 0 ? CurrentValue / MaxValue : 0f;
    public bool IsCritical => NormalizedValue < CriticalThreshold;

    public void Decay(float deltaTime)
    {
        CurrentValue = Mathf.Max(0f, CurrentValue - DecayRate * deltaTime);
    }

    public void Restore(float amount)
    {
        CurrentValue = Mathf.Min(MaxValue, CurrentValue + amount);
    }
}
