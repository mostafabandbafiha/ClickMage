using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/Structure Definition")]
public class StructureDefinition : ScriptableObject
{
    public string Name;
    public GameObject Prefab;

    [Header("Size in grid cells")]
    public Vector2Int Size = Vector2Int.one; // e.g. (2,2) for a 2x2 structure

    public List<Vector2Int> GetFootprint()
    {
        var footprint = new List<Vector2Int>();
        for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y; y++)
                footprint.Add(new Vector2Int(x, y));
        return footprint;
    }
}