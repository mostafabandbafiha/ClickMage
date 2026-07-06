using System.Collections.Generic;
using UnityEngine;

public class GridVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridData grid;
    [SerializeField] private GameObject cellPrefab;

    [Header("Highlight Materials")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    private GameObject _tilesRoot;
    private GameObject[,] _tiles;
    private readonly Dictionary<Vector2Int, GameObject> _highlights = new();

    public void Init(GridData grid)
    {
        this.grid = grid;
        GenerateGrid();
    }

    [ContextMenu("Regenerate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();

        _tilesRoot = new GameObject("Grid Tiles");
        _tilesRoot.transform.SetParent(transform);
        _tiles = new GameObject[grid.Cols, grid.Rows];

        for (int c = 0; c < grid.Cols; c++)
        {
            for (int r = 0; r < grid.Rows; r++)
            {
                Vector3 pos = grid.GridToWorld(c, r);
                var tile = Instantiate(cellPrefab, pos, Quaternion.identity, _tilesRoot.transform);
                tile.name = $"Cell_{c}_{r}";
                _tiles[c, r] = tile;
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (_tilesRoot != null)
            _tilesRoot.SetActive(visible);
    }

    public void UpdateHighlights(List<Vector2Int> footprint, bool canPlace)
    {
        ClearHighlights();

        Material mat = canPlace ? validMaterial : invalidMaterial;

        foreach (var cell in footprint)
        {
            if (!grid.IsInBounds(cell.x, cell.y)) continue;

            Vector3 pos = grid.GridToWorld(cell.x, cell.y);
            // slightly above tile to avoid z-fighting
            pos.y += 0.01f;

            var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.transform.position = pos;
            overlay.transform.rotation = Quaternion.Euler(90, 0, 0);
            overlay.transform.localScale = Vector3.one * grid.CellSize;
            overlay.transform.SetParent(transform);
            overlay.GetComponent<Renderer>().material = mat;

            // no collider needed on overlay
            Destroy(overlay.GetComponent<Collider>());

            _highlights[cell] = overlay;
        }
    }

    public void ClearHighlights()
    {
        foreach (var kvp in _highlights)
            if (kvp.Value != null) Destroy(kvp.Value);
        _highlights.Clear();
    }

    private void ClearGrid()
    {
        if (_tilesRoot != null)
            Destroy(_tilesRoot);
        _tiles = null;
    }
}
