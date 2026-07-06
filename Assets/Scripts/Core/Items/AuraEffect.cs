// AuraEffect.cs — serializable data defining one buff/debuff the aura applies
using ClickMage.Stats;
using System;
using UnityEngine;

[Serializable]
public class AuraEffect
{
    [Tooltip("Which stat to modify")]
    public string StatKey;

    [Tooltip("Amount to add/multiply")]
    public float Value;

    [Tooltip("Add, Multiply, or Override")]
    public StatModifierType ModifierType;
}