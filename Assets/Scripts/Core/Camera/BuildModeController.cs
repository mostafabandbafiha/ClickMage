using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    [SerializeField] private GridData grid;
    [SerializeField] private GridVisual gridVisual;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private StructureDefinition _testStructure;
    [SerializeField] private Transform _origin;
    [SerializeField] private NavMeshSurface navMeshSurface;

    private StructureDefinition _selectedStructure;
    private GameObject _ghost;
    private int _rotation;
    private bool _isActive;
    private Vector2Int _currentCell;
    private bool _canPlace;
    private bool _isDragging;

    // move-existing flow
    private bool _isMovingExisting;
    private GameObject _existingStructure;
    private Vector2Int _existingOriginalCell;
    private int _existingOriginalRotation;

    public bool IsActive => _isActive;

    public GridData GetGridData => grid;

    private void Awake()
    {
        grid = new GridData(20, 20, 7, _origin.position);
        Instance = this;
    }

    private void Start()
    {
        //grid = new GridData(20, 20, 7, _origin.position);
        gridVisual.Init(grid);
        LeaveBuildMode();
    }

    private void Update()
    {
        if (!_isActive) return;

        // — move-existing: waiting for player to click a structure —
        if (_isMovingExisting && _ghost == null)
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                TryPickUpStructure();

            if (Input.GetKeyDown(KeyCode.Escape))
                ExitBuildMode(confirm: false);

            return;
        }

        // — shared controls once a ghost exists —
        if (Input.GetKeyDown(KeyCode.R))
        {
            Rotate();
            RefreshGhost();
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            _isDragging = true;

        if (Input.GetMouseButtonUp(0) && _ghost != null)
        {
            _isDragging = false;
            ContextMenuManager.Instance.OpenBuildMenu(_ghost.transform);
        }

        if (_isDragging && TryGetCellUnderCursor(out Vector2Int hitCell))
        {
            Vector2Int newCell = new Vector2Int(
                hitCell.x - (_selectedStructure.Size.x - 1) / 2,
                hitCell.y - (_selectedStructure.Size.y - 1) / 2
            );

            if (newCell != _currentCell)
            {
                _currentCell = newCell;
                RefreshGhost();
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            ExitBuildMode(confirm: false);
    }

    // ── Enter / Exit ───────────────────────────────────────────────────────

    /// <summary>Place a brand-new structure (called from BuildPanel).</summary>
    public void EnterBuildMode(StructureDefinition structure)
    {
        _selectedStructure = structure;
        _rotation = 0;
        _isActive = true;
        _isDragging = false;
        _isMovingExisting = false;

        _currentCell = new Vector2Int(
            grid.Cols / 2 - (_selectedStructure.Size.x - 1) / 2,
            grid.Rows / 2 - (_selectedStructure.Size.y - 1) / 2
        );

        gridVisual.SetVisible(true);
        SpawnGhost();
        RefreshGhost();
    }

    /// <summary>Move-existing mode: no structure selected yet, wait for click.</summary>
    public void EnterBuildMode()
    {
        _isActive = true;
        _isDragging = false;
        _isMovingExisting = true;
        _ghost = null;
        _selectedStructure = null;

        gridVisual.SetVisible(true);
    }

    public void LeaveBuildMode()
    {
        if (_ghost != null)
        {
            if (_isMovingExisting)
                CancelCurrent();
            else
                DestroyGhost();
        }

        ContextMenuManager.Instance.CloseMenu();

        _isActive = false;
        _isMovingExisting = false;
        _selectedStructure = null;
        _rotation = 0;
        _existingStructure = null;

        gridVisual.SetVisible(false);
        gridVisual.ClearHighlights();
    }

    public void ConfirmCurrent()
    {
        if (_ghost == null || !_canPlace) return;

        List<Vector2Int> footprint = GetRotatedFootprint(_currentCell);

        if (_isMovingExisting)
        {
            grid.OccupyFootprint(footprint, _ghost);

            Vector3 worldPos = GetFootprintCenter(_currentCell);
            worldPos.y = 0f;
            _ghost.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0, _rotation, 0));

            foreach (var c in footprint)
                grid.GetCell(c.x, c.y).Structure = _ghost;

            // stamp updated placement data
            var data = _ghost.GetComponent<StructurePlacementData>();
            if (data == null) data = _ghost.AddComponent<StructurePlacementData>();
            data.AnchorCell = _currentCell;
            data.Rotation = _rotation;

            RestoreComponents(_ghost);
            RestoreOriginalMaterial(_ghost);
        }
        else
        {
            PlaceStructure(_currentCell, footprint);
            DestroyGhost();
        }


        // Bake NavMesh after moving
        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();

        gridVisual.ClearHighlights();

        _ghost = null;
        _isMovingExisting = true;
        _selectedStructure = null;
        _existingStructure = null;
        _rotation = 0;
    }

    public void CancelCurrent()
    {
        if (_ghost == null) return;

        if (_isMovingExisting)
        {
            _rotation = _existingOriginalRotation;
            List<Vector2Int> originalFootprint = GetRotatedFootprint(_existingOriginalCell);
            grid.OccupyFootprint(originalFootprint, _ghost);

            Vector3 revertPos = GetFootprintCenter(_existingOriginalCell);
            revertPos.y = 0f;
            _ghost.transform.SetPositionAndRotation(revertPos, Quaternion.Euler(0, _existingOriginalRotation, 0));

            foreach (var c in originalFootprint)
                grid.GetCell(c.x, c.y).Structure = _ghost;

            // restore placement data to original
            var data = _ghost.GetComponent<StructurePlacementData>();
            if (data == null) data = _ghost.AddComponent<StructurePlacementData>();
            data.AnchorCell = _existingOriginalCell;
            data.Rotation = _existingOriginalRotation;

            RestoreOriginalMaterial(_ghost);
            RestoreComponents(_ghost);
        }
        else
        {
            DestroyGhost();
        }

        gridVisual.ClearHighlights();

        _ghost = null;
        _isMovingExisting = true;
        _selectedStructure = null;
        _existingStructure = null;
        _rotation = 0;
    }

    public void ExitBuildMode(bool confirm)
    {
        if (_ghost != null)
        {
            if (_isMovingExisting)
                FinishMoveExisting(confirm);
            else
                FinishPlaceNew(confirm);
        }

        _isActive = false;
        _isMovingExisting = false;
        _selectedStructure = null;
        _rotation = 0;
        _existingStructure = null;

        gridVisual.SetVisible(false);
        gridVisual.ClearHighlights();
        DestroyGhost();
    }

    // ── New placement ──────────────────────────────────────────────────────

    private void FinishPlaceNew(bool confirm)
    {
        if (confirm && _canPlace)
        {
            List<Vector2Int> footprint = GetRotatedFootprint(_currentCell);
            PlaceStructure(_currentCell, footprint);
        }
    }

    private void PlaceStructure(Vector2Int anchorCell, List<Vector2Int> footprint)
    {
        grid.OccupyFootprint(footprint);

        Vector3 worldPos = GetFootprintCenter(anchorCell);
        worldPos.y = 0f;

        var placed = Instantiate(
            _selectedStructure.Prefab,
            worldPos,
            Quaternion.Euler(0, _rotation, 0)
        );

        var data = placed.AddComponent<StructurePlacementData>();
        data.AnchorCell = anchorCell;
        data.Rotation = _rotation;

        foreach (var c in footprint)
            grid.GetCell(c.x, c.y).Structure = placed;

        // Notify any structure that cares about being placed
        var house = placed.GetComponent<HouseStructure>();
        if (house != null) house.OnPlaced();
    }


    // ── Move existing ──────────────────────────────────────────────────────

    private void TryPickUpStructure()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        var selectable = hit.collider.GetComponentInParent<SelectableComponent>();
        if (selectable == null || !selectable.IsStructure) return;

        _existingStructure = selectable.gameObject;
        _selectedStructure = selectable.StructureDefinition;

        // read authoritative anchor + rotation from the stamped component
        // instead of scanning the grid (which returns wrong anchor for rotated structures)
        var placementData = _existingStructure.GetComponent<StructurePlacementData>();
        if (placementData == null) return;

        _existingOriginalCell = placementData.AnchorCell;
        _existingOriginalRotation = placementData.Rotation;
        _rotation = _existingOriginalRotation;

        // free the correct cells using the authoritative data
        List<Vector2Int> oldFootprint = GetRotatedFootprint(_existingOriginalCell);
        grid.FreeFootprint(oldFootprint);

        _ghost = _existingStructure;
        _currentCell = _existingOriginalCell;

        _originalMaterials.Clear();
        ApplyGhostMaterial(_ghost, true);
        DisableComponents(_ghost);
        RefreshGhost();
    }

    private void FinishMoveExisting(bool confirm)
    {
        if (confirm && _canPlace)
        {
            List<Vector2Int> footprint = GetRotatedFootprint(_currentCell);
            grid.OccupyFootprint(footprint, _ghost);

            Vector3 worldPos = GetFootprintCenter(_currentCell);
            worldPos.y = 0f;
            _ghost.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0, _rotation, 0));

            foreach (var c in footprint)
                grid.GetCell(c.x, c.y).Structure = _ghost;

            var data = _ghost.GetComponent<StructurePlacementData>();
            if (data == null) data = _ghost.AddComponent<StructurePlacementData>();
            data.AnchorCell = _currentCell;
            data.Rotation = _rotation;

        }
        else
        {
            _rotation = _existingOriginalRotation;
            List<Vector2Int> originalFootprint = GetRotatedFootprint(_existingOriginalCell);
            grid.OccupyFootprint(originalFootprint, _ghost);

            Vector3 revertPos = GetFootprintCenter(_existingOriginalCell);
            revertPos.y = 0f;
            _ghost.transform.SetPositionAndRotation(revertPos, Quaternion.Euler(0, _existingOriginalRotation, 0));

            foreach (var c in originalFootprint)
                grid.GetCell(c.x, c.y).Structure = _ghost;

            var data = _ghost.GetComponent<StructurePlacementData>();
            if (data == null) data = _ghost.AddComponent<StructurePlacementData>();
            data.AnchorCell = _existingOriginalCell;
            data.Rotation = _existingOriginalRotation;
        }

        RestoreComponents(_ghost);
        RestoreOriginalMaterial(_ghost);

        _ghost = null;
    }


    // ── Ghost helpers ──────────────────────────────────────────────────────

    private void RefreshGhost()
    {
        List<Vector2Int> footprint = GetRotatedFootprint(_currentCell);
        _canPlace = grid.CanPlace(footprint);

        if (_ghost != null)
        {
            _ghost.transform.position = GetFootprintCenter(_currentCell);
            _ghost.transform.rotation = Quaternion.Euler(0, _rotation, 0);
            ApplyGhostMaterial(_ghost, _canPlace);
        }

        gridVisual.UpdateHighlights(footprint, _canPlace);
    }

    private void SpawnGhost()
    {
        DestroyGhost();

        Vector3 spawnPos = GetFootprintCenter(_currentCell);
        spawnPos.y = 0f;

        _ghost = Instantiate(_selectedStructure.Prefab, spawnPos, Quaternion.Euler(0, _rotation, 0));
        DisableComponents(_ghost);
        ApplyGhostMaterial(_ghost, false);
    }

    private void DestroyGhost()
    {
        if (_ghost != null && !_isMovingExisting)
            Destroy(_ghost);

        _ghost = null;
    }

    // ── Material helpers ───────────────────────────────────────────────────

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new();

    private void ApplyGhostMaterial(GameObject go, bool canPlace)
    {
        Color color = canPlace ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!_originalMaterials.ContainsKey(r))
                _originalMaterials[r] = r.sharedMaterials;

            var mats = r.materials;
            foreach (var mat in mats)
                mat.color = color;
            r.materials = mats;
        }
    }

    private void RestoreOriginalMaterial(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (_originalMaterials.TryGetValue(r, out var mats))
                r.sharedMaterials = mats;
        }
        _originalMaterials.Clear();
    }

    // ── Component disable/restore ──────────────────────────────────────────

    private readonly List<MonoBehaviour> _disabledComponents = new();

    private void DisableComponents(GameObject go)
    {
        _disabledComponents.Clear();
        foreach (var mono in go.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono == this) continue;
            if (mono.enabled)
            {
                mono.enabled = false;
                _disabledComponents.Add(mono);
            }
        }
    }

    private void RestoreComponents(GameObject go)
    {
        foreach (var mono in _disabledComponents)
            if (mono != null) mono.enabled = true;
        _disabledComponents.Clear();
    }

    // ── Grid helpers ───────────────────────────────────────────────────────

    public void BuildNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }

    public bool TryPlaceStructureAt(StructureDefinition structure, Vector2Int anchorCell, int rotation)
    {
        var footprint = new List<Vector2Int>();
        foreach (var offset in structure.GetFootprint())
        {
            Vector2Int rotated = RotateOffset(offset, rotation);
            footprint.Add(new Vector2Int(anchorCell.x + rotated.x, anchorCell.y + rotated.y));
        }

        if (!grid.CanPlace(footprint))
            return false;

        grid.OccupyFootprint(footprint);

        Vector3 worldPos = GetFootprintCenterPublic(anchorCell, footprint);
        worldPos.y = 0f;

        var placed = Instantiate(structure.Prefab, worldPos, Quaternion.Euler(0, rotation, 0));

        var data = placed.AddComponent<StructurePlacementData>();
        data.AnchorCell = anchorCell;
        data.Rotation = rotation;

        foreach (var c in footprint)
            grid.GetCell(c.x, c.y).Structure = placed;

        var house = placed.GetComponent<HouseStructure>();
        if (house != null) house.OnPlaced();

        return true;
    }


    private bool TryGetCellUnderCursor(out Vector2Int cell)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            cell = grid.WorldToGrid(ray.GetPoint(distance));
            return true;
        }

        cell = _currentCell;
        return false;
    }

    /// <summary>Frees the grid cells a placed structure was occupying. Call this when
    /// a structure is destroyed — placement/move already calls grid.FreeFootprint()
    /// directly, but nothing previously did this on death, so destroyed Blocks kept
    /// showing as occupied on the grid forever (stale Structure reference too).</summary>
    public void FreeStructureFootprint(GameObject structure, Action OnFreeFootPrint)
    {
        if (structure == null || grid == null) return;
        var placementData = structure.GetComponent<StructurePlacementData>();
        var block = structure.GetComponent<Block>();
        if (placementData == null || block == null || block.Definition == null) return;

        var footprint = new List<Vector2Int>();
        foreach (var offset in block.Definition.GetFootprint())
        {
            Vector2Int rotated = RotateOffset(offset, placementData.Rotation);
            footprint.Add(new Vector2Int(placementData.AnchorCell.x + rotated.x, placementData.AnchorCell.y + rotated.y));
        }
        grid.FreeFootprint(footprint);
        OnFreeFootPrint?.Invoke();

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
    }

    private List<Vector2Int> GetRotatedFootprint(Vector2Int anchorCell)
    {
        var result = new List<Vector2Int>();
        foreach (var offset in _selectedStructure.GetFootprint())
        {
            Vector2Int rotated = RotateOffset(offset, _rotation);
            result.Add(new Vector2Int(anchorCell.x + rotated.x, anchorCell.y + rotated.y));
        }
        return result;
    }

    private Vector3 GetFootprintCenter(Vector2Int anchorCell)
    {
        List<Vector2Int> footprint = GetRotatedFootprint(anchorCell);

        float sumX = 0f, sumZ = 0f;
        foreach (var cell in footprint)
        {
            Vector3 cellWorld = grid.GridToWorld(cell.x, cell.y);
            sumX += cellWorld.x;
            sumZ += cellWorld.z;
        }

        float count = footprint.Count;
        return new Vector3(sumX / count, 0f, sumZ / count);
    }

    private Vector3 GetFootprintCenterPublic(Vector2Int anchorCell, List<Vector2Int> footprint)
    {
        float sumX = 0f, sumZ = 0f;
        foreach (var cell in footprint)
        {
            Vector3 cellWorld = grid.GridToWorld(cell.x, cell.y);
            sumX += cellWorld.x;
            sumZ += cellWorld.z;
        }
        return new Vector3(sumX / footprint.Count, 0f, sumZ / footprint.Count);
    }

    private void Rotate() => _rotation = (_rotation + 90) % 360;

    private static Vector2Int RotateOffset(Vector2Int offset, int degrees) => degrees switch
    {
        90 => new Vector2Int(offset.y, -offset.x),
        180 => new Vector2Int(-offset.x, -offset.y),
        270 => new Vector2Int(-offset.y, offset.x),
        _ => offset
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [ContextMenu("Test Place New")]
    private void TestBuildMode()
    {
        if (_testStructure == null) { Debug.LogWarning("Assign a StructureDefinition first."); return; }
        EnterBuildMode(_testStructure);
    }

    [ContextMenu("Test Move Existing")]
    private void TestMoveMode() => EnterBuildMode();

    [ContextMenu("Confirm")]
    private void TestConfirm() => ExitBuildMode(confirm: true);

    [ContextMenu("Cancel")]
    private void TestCancel() => ExitBuildMode(confirm: false);
}