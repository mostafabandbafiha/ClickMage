using ClickMage.Entities;
using System.Collections.Generic;
using UnityEngine;

public class DecorationStructure : BaseEntity
{
    [Header("Decoration Settings")]
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;

    public string DisplayName => _displayName;
    public string Description => _description;

    public float HappinessValue => HasStat("happiness") ? GetStatValue("happiness") : 0f;
    public float AestheticsValue => HasStat("aesthetics") ? GetStatValue("aesthetics") : 0f;
    public float InfluenceRadius => HasStat("influenceRadius") ? GetStatValue("influenceRadius") : 0f;

    public bool IsPlaced { get; private set; }
    public event System.Action<DecorationStructure> OnPlaced;
    public event System.Action<DecorationStructure> OnRemoved;

    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        IsPlaced = true;
        OnPlaced?.Invoke(this);
    }

    public void Remove()
    {
        IsPlaced = false;
        OnRemoved?.Invoke(this);
        Destroy(gameObject);
    }

    public List<BaseCharacter> GetCharactersInRange()
    {
        var result = new List<BaseCharacter>();
        if (InfluenceRadius <= 0f) return result;
        foreach (var hit in Physics.OverlapSphere(transform.position, InfluenceRadius))
        {
            var c = hit.GetComponent<BaseCharacter>();
            if (c != null) result.Add(c);
        }
        return result;
    }
}