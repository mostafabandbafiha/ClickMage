using ClickMage.Interface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera uiCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomSmoothTime = 0.15f;

    [Header("Pan — Middle Mouse")]
    [SerializeField] private float panSpeed = 1f;

    [Header("Keyboard Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Camera Bounds (optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float boundsMinX = -50f;
    [SerializeField] private float boundsMaxX = 50f;
    [SerializeField] private float boundsMinZ = -50f;
    [SerializeField] private float boundsMaxZ = 50f;

    [Header("Selection Raycast")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private LayerMask collectibleLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastMaxDistance = 100f;

    [Header("Hold Collection")]
    [SerializeField] private float holdThreshold = 0.4f;
    [SerializeField] private float collectRadius = 1.5f;
    [SerializeField] private float maxDragDistance = 10f;

    [Header("Hover Outline")]
    [SerializeField] private bool enableHoverOutline = true;
    [SerializeField] private LayerMask hoverLayer; // usually same as selectableLayer



    // ── Private state ──────────────────────────────────────────────────────

    private float _targetZoom;
    private float _zoomVelocity;
    private bool _isPanning;
    private Vector3 _panOriginWorld;
    private float _mouseDownTime;
    private Vector2 _mouseDownScreenPos;
    private bool _holdTriggered;
    private Coroutine _holdCoroutine;

    // NEW: fixed height for the follow rig - never drifts regardless of what a raycast hits
    private float _followTargetFixedY;

    // NEW: selection cycling state
    private List<Collider> _lastCycleHits = new List<Collider>();
    private int _cycleIndex = -1;

    private GameObject _currentHoveredObject;
    private EPOOutline.Outlinable _currentHoverOutline;
    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (virtualCamera != null) _targetZoom = virtualCamera.m_Lens.OrthographicSize;
        if (followTarget != null) _followTargetFixedY = followTarget.position.y;
    }

    private void Update()
    {
        HandleZoom();
        HandleKeyboardMovement();
        HandlePan();
        HandleClickAndHold();
        HandleRightClick();
        ClampPosition();

        if (enableHoverOutline) HandleHover();
    }

    // ── Follow-target movement (single point of truth for Y) ───────────────

    /// <summary>
    /// All followTarget position writes should go through here.
    /// Guarantees Y never drifts due to a raycast hitting a tower/hero/etc.
    /// </summary>
    private void SetFollowTargetXZ(Vector3 desiredPos)
    {
        desiredPos.y = _followTargetFixedY;
        followTarget.position = desiredPos;
    }

    // ── Input handlers ───────────────────────────────────────────────────

    private void HandleRightClick()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsActive) return;
        if (IsPointerOverUI(Input.mousePosition)) return;

        var selected = SelectionManager.Instance.CurrentSelected;
        if (selected == null) return;

        BaseCharacter character = selected.GetComponent<BaseCharacter>();
        if (character == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit selectableHit, raycastMaxDistance, selectableLayer))
        {
            OpenContextMenu(character, selectableHit.collider.gameObject);
            return;
        }

        if (Physics.Raycast(ray, out RaycastHit groundHit, raycastMaxDistance, groundLayer))
        {
            bool isShift = Input.GetKey(KeyCode.LeftShift);
            ICommand<BaseCharacter> moveCmd = new MoveCommand(groundHit.point);

            if (isShift)
                character.QueueCommand(moveCmd);
            else
                character.GiveCommand(moveCmd);
        }
    }

    private void OpenContextMenu(BaseCharacter character, GameObject target)
    {
        ContextMenuManager.Instance.OpenMenu(character, target);
    }

    private void HandleZoom()
    {
        if (virtualCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            _targetZoom = Mathf.Clamp(_targetZoom - scroll * zoomSpeed, minZoom, maxZoom);

        float current = virtualCamera.m_Lens.OrthographicSize;
        float next = Mathf.SmoothDamp(current, _targetZoom, ref _zoomVelocity, zoomSmoothTime);
        virtualCamera.m_Lens.OrthographicSize = next;
    }

    private void HandleKeyboardMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f) return;

        Vector3 right = mainCamera.transform.right;
        Vector3 forward = mainCamera.transform.forward;
        right.y = 0f; forward.y = 0f;
        right.Normalize(); forward.Normalize();

        Vector3 dir = (right * h + forward * v).normalized;
        SetFollowTargetXZ(followTarget.position + dir * (moveSpeed * Time.deltaTime));
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _isPanning = true;
            _panOriginWorld = RaycastGroundPlane(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(2)) _isPanning = false;
        if (!_isPanning) return;

        Vector3 currentWorld = RaycastGroundPlane(Input.mousePosition);
        SetFollowTargetXZ(followTarget.position + (_panOriginWorld - currentWorld));
    }

    private void HandleClickAndHold()
    {
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            _mouseDownTime = Time.time;
            _mouseDownScreenPos = Input.mousePosition;
            _holdTriggered = false;
            _holdCoroutine = StartCoroutine(HoldRoutine());
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_holdCoroutine != null) { StopCoroutine(_holdCoroutine); _holdCoroutine = null; }

            if (!_holdTriggered)
            {
                float drag = Vector2.Distance(_mouseDownScreenPos, Input.mousePosition);
                if (drag < maxDragDistance) HandleShortClick(Input.mousePosition);
            }

            _holdTriggered = false;
        }
    }

    private IEnumerator HoldRoutine()
    {
        yield return new WaitForSeconds(holdThreshold);

        float drag = Vector2.Distance(_mouseDownScreenPos, Input.mousePosition);
        if (drag < maxDragDistance)
        {
            _holdTriggered = true;
            HandleHoldAction(Input.mousePosition);
        }
    }

    private void HandleShortClick(Vector2 screenPos)
    {
        if (IsPointerOverUI(screenPos))
        {
            // clicking UI shouldn't disturb the selection cycle state
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // Priority 1: Collectibles
        if (Physics.Raycast(ray, out RaycastHit hitItem, raycastMaxDistance, collectibleLayer))
        {
            InteractableComponent interactable = hitItem.collider.GetComponentInParent<InteractableComponent>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }

        // Priority 2: Selectable entities — cycle through overlapping colliders
        if (TryGetCycledSelectable(ray, out SelectableComponent selectable))
        {
            if (SelectionManager.Instance.CurrentSelected == selectable)
            {
                var node = selectable.GetComponent<ResourceNode>();
                if (node != null)
                {
                    node.TryHarvest();
                }
                return;
            }

            SelectionManager.Instance.Select(selectable);
            return;
        }

        // Priority 3: Nothing hit — deselect
        SelectionManager.Instance.Deselect();
    }

    // NEW: cycles through all selectable colliders under the cursor on repeated clicks
    private bool TryGetCycledSelectable(Ray ray, out SelectableComponent selectable)
    {
        selectable = null;

        RaycastHit[] hits = Physics.RaycastAll(ray, raycastMaxDistance, selectableLayer);
        if (hits.Length == 0)
        {
            _lastCycleHits.Clear();
            _cycleIndex = -1;
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var hitColliders = new List<Collider>(hits.Length);
        foreach (var h in hits) hitColliders.Add(h.collider);

        bool sameStack = SameColliderSet(hitColliders, _lastCycleHits);
        _cycleIndex = sameStack ? (_cycleIndex + 1) % hitColliders.Count : 0;
        _lastCycleHits = hitColliders;

        selectable = hitColliders[_cycleIndex].GetComponentInParent<SelectableComponent>();
        return selectable != null;
    }

    private static bool SameColliderSet(List<Collider> a, List<Collider> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void HandleHoldAction(Vector2 screenPos)
    {
        if (IsPointerOverUI(screenPos)) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance))
            CollectInRadius(hit.point);
    }

    private void CollectInRadius(Vector3 worldPos)
    {
        Collider[] hits = Physics.OverlapSphere(worldPos, collectRadius, collectibleLayer);
        foreach (Collider col in hits)
        {
            InteractableComponent interactable = col.GetComponentInParent<InteractableComponent>();
            interactable?.Interact();
        }
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        if (uiCamera != null)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            if (results.Count > 0) return true;
        }

        return false;
    }

    private void HandleHover()
    {
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsActive)
        {
            ClearHover();
            return;
        }

        if (IsPointerOverUI(Input.mousePosition))
        {
            ClearHover();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, hoverLayer))
        {
            GameObject hitRoot = hit.collider.transform.root.gameObject; // or GetComponentInParent<Outlinable>()?.gameObject

            if (hitRoot != _currentHoveredObject)
            {
                ClearHover();
                SetHover(hit.collider);
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void SetHover(Collider hitCollider)
    {
        var outlinable = hitCollider.GetComponentInParent<EPOOutline.Outlinable>();
        if (outlinable == null) return;

        outlinable.FrontParameters.Enabled = true;

        _currentHoveredObject = outlinable.gameObject;
        _currentHoverOutline = outlinable;
    }

    private void ClearHover()
    {
        if (_currentHoverOutline != null)
            _currentHoverOutline.FrontParameters.Enabled = false;

        _currentHoveredObject = null;
        _currentHoverOutline = null;
    }

    // FIXED: now restricted to groundLayer, and the plane fallback uses the
    // rig's actual fixed height instead of assuming y = 0.
    private Vector3 RaycastGroundPlane(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, groundLayer))
            return hit.point;

        if (Mathf.Abs(ray.direction.y) > 0.001f)
        {
            float t = (_followTargetFixedY - ray.origin.y) / ray.direction.y;
            if (t > 0f) return ray.origin + ray.direction * t;
        }

        return followTarget.position;
    }

    private void ClampPosition()
    {
        if (!useBounds) return;
        Vector3 pos = followTarget.position;
        pos.x = Mathf.Clamp(pos.x, boundsMinX, boundsMaxX);
        pos.z = Mathf.Clamp(pos.z, boundsMinZ, boundsMaxZ);
        SetFollowTargetXZ(pos);
    }

    private void OnDrawGizmosSelected()
    {
        if (followTarget == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(followTarget.position, collectRadius);
    }
}