// ResourceNodeData.cs
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceNode", menuName = "ClickMage/World/ResourceNodeData")]
public class ResourceNodeData : ScriptableObject
{

    [Header("Harvesting")]
    public float dropRadius = 1.2f;

    [Header("Outputs")]
    public NodeOutputEntry[] outputs;

    [Header("Visual Stage Thresholds")]
    [Tooltip("HP thresholds only. Actual GameObjects are on the prefab.")]
    public float[] stageThresholds = { 1.0f, 0.66f, 0.33f, 0.0f };
}

// ── Nested data types ─────────────────────────────────────────────────────────

[Serializable]
public class NodeOutputEntry
{
    public ItemData item;
    [Range(0f, 1f)] public float dropChance = 1f;
    public int minAmount = 1;
    public int maxAmount = 1;
}


