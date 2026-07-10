using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ClickMage/Projectiles/Projectile Registry")]
public class ProjectileRegistry : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public DamageType Element;
        public GameObject Prefab;
    }

    [SerializeField] private GameObject defaultPrefab;
    [SerializeField] private List<Entry> entries = new();

    public GameObject GetPrefab(DamageType requested)
    {
        foreach (var e in entries)
            if (e.Element == requested) return e.Prefab;

        GameObject best = null;
        int bestBits = 0;
        foreach (var e in entries)
        {
            if (e.Element == DamageType.Normal) continue;
            if ((requested & e.Element) != e.Element) continue;

            int bits = CountBits((int)e.Element);
            if (bits > bestBits) { bestBits = bits; best = e.Prefab; }
        }
        return best != null ? best : defaultPrefab;
    }

    private static int CountBits(int n) { int c = 0; while (n != 0) { c += n & 1; n >>= 1; } return c; }
}