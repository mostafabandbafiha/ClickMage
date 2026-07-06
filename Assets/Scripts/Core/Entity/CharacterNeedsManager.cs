// CharacterNeedsManager.cs
using System.Collections.Generic;
using UnityEngine;

public class CharacterNeedsManager : MonoBehaviour
{
    [SerializeField] private List<CharacterNeed> _needs = new();

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        foreach (var need in _needs)
        {
            need.Decay(deltaTime);
        }
    }

    public CharacterNeed GetNeed(NeedType type)
    {
        return _needs.Find(n => n.Type == type);
    }

    public CharacterNeed GetMostUrgentNeed()
    {
        CharacterNeed mostUrgent = null;
        float lowestValue = 1f;

        foreach (var need in _needs)
        {
            if (need.NormalizedValue < lowestValue)
            {
                lowestValue = need.NormalizedValue;
                mostUrgent = need;
            }
        }

        return mostUrgent;
    }

    public bool HasCriticalNeed()
    {
        return _needs.Exists(n => n.IsCritical);
    }
}
