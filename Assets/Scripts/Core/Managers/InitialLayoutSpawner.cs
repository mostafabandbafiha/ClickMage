using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct StructurePlacementEntry
{
    public StructureDefinition Structure;
    public Vector2Int AnchorCell;
    public int Rotation; // 0/90/180/270
}

public class InitialLayoutSpawner : MonoBehaviour
{
    [SerializeField] private BuildModeController buildModeController;
    [SerializeField] private List<StructurePlacementEntry> layout;

    private void Start()
    {
        SpawnLayout();
    }

    private void SpawnLayout()
    {
        foreach (var entry in layout)
        {
            bool placed = buildModeController.TryPlaceStructureAt(
                entry.Structure, entry.AnchorCell, entry.Rotation);

            if (!placed)
                Debug.LogWarning($"[InitialLayoutSpawner] Failed to place {entry.Structure.name} at {entry.AnchorCell}");
        }
        buildModeController.BuildNavMesh();
    }
}
