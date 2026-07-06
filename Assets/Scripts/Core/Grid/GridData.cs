using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellType { Ground, Water, Cliff }

public class CellData
{
    public bool IsOccupied;
    public CellType Type;
    public GameObject Structure;
}
public class StructurePlacementData : MonoBehaviour
{
    public Vector2Int AnchorCell;
    public int Rotation;
}

public class GridData
{
    public readonly int Cols;
    public readonly int Rows;
    public readonly float CellSize;
    public readonly Vector3 Origin;

    private readonly CellData[,] _cells;

    public GridData(int cols, int rows, float cellSize, Vector3 origin)
    {
        Cols = cols;
        Rows = rows;
        CellSize = cellSize;
        Origin = origin;

        _cells = new CellData[cols, rows];
        for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
                _cells[c, r] = new CellData();
    }

    public Vector3 GridToWorld(int col, int row)
    {
        return Origin + new Vector3(col * CellSize + CellSize * 0.5f, 0, row * CellSize + CellSize * 0.5f);
    }

    public bool WorldToGrid(Vector3 worldPos, out int col, out int row)
    {
        col = Mathf.FloorToInt((worldPos.x - Origin.x) / CellSize);
        row = Mathf.FloorToInt((worldPos.z - Origin.z) / CellSize);
        return IsInBounds(col, row);
    }

    public bool IsInBounds(int col, int row) =>
        col >= 0 && col < Cols && row >= 0 && row < Rows;

    public CellData GetCell(int col, int row) => _cells[col, row];

    public void SetOccupied(int col, int row, bool occupied, GameObject structure = null)
    {
        if (!IsInBounds(col, row)) return;
        _cells[col, row].IsOccupied = occupied;
        _cells[col, row].Structure = occupied ? structure : null;
    }

    public bool CanPlace(List<Vector2Int> footprint)
    {
        foreach (var cell in footprint)
        {
            if (!IsInBounds(cell.x, cell.y)) return false;
            if (_cells[cell.x, cell.y].IsOccupied) return false;
        }
        return true;
    }

    public void OccupyFootprint(List<Vector2Int> footprint, GameObject structure)
    {
        foreach (var cell in footprint)
            SetOccupied(cell.x, cell.y, true, structure);
    }

    public void FreeFootprint(List<Vector2Int> footprint)
    {
        foreach (var cell in footprint)
            SetOccupied(cell.x, cell.y, false);
    }

    internal Vector2Int WorldToGrid(Vector3 worldPoint)
    {
        WorldToGrid(worldPoint, out int col, out int row);
        return new Vector2Int(col, row);
    }

    internal void OccupyFootprint(List<Vector2Int> footprint)
    {
        OccupyFootprint(footprint, null);
    }

}
