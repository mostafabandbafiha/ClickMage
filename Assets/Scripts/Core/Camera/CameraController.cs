using ClickMage.Interface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

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

    // ── Private state ──────────────────────────────────────────────────────

    private float _targetZoom;
    private float _zoomVelocity;
    private bool _isPanning;
    private Vector3 _panOriginWorld;
    private float _mouseDownTime;
    private Vector2 _mouseDownScreenPos;
    private bool _holdTriggered;
    private Coroutine _holdCoroutine;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (virtualCamera != null) _targetZoom = virtualCamera.m_Lens.OrthographicSize;
    }

    private void Update()
    {
        HandleZoom();
        HandleKeyboardMovement();
        HandlePan();
        HandleClickAndHold();
        HandleRightClick();
        ClampPosition();
    }

    // ── Input handlers — all unchanged ────────────────────────────────────

    private void HandleRightClick()
    {   

        if (!Input.GetMouseButtonDown(1)) return;

        // let BuildModeController consume right-click first
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsActive) return;

        if (IsPointerOverUI(Input.mousePosition)) return;

        // only act if selected entity is a character
        var selected = SelectionManager.Instance.CurrentSelected;
        if (selected == null) return;

        BaseCharacter character = selected.GetComponent<BaseCharacter>();
        if (character == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // priority 1: hit a selectable target → context menu
        if (Physics.Raycast(ray, out RaycastHit selectableHit, raycastMaxDistance, selectableLayer))
        {
            OpenContextMenu(character, selectableHit.collider.gameObject);
            return;
        }

        // priority 2: hit ground → move command
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

    /*private void OpenContextMenu(Character character, GameObject target)
    {
        Debug.Log($"[ContextMenu] {character.CharacterName} → {target.name}");

        if (target.TryGetComponent(out ResourceNode node))
        {
            Debug.Log($"Issuing HarvestCommand on {node.name}");

            bool isShift = Input.GetKey(KeyCode.LeftShift);
            ICommand harvestCmd = new HarvestCommand(node);

            if (isShift)
                character.QueueCommand(harvestCmd);
            else
                character.GiveCommand(harvestCmd);
        }
    }*/

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
        followTarget.position += dir * (moveSpeed * Time.deltaTime);
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
        followTarget.position += _panOriginWorld - currentWorld;
    }

    private void HandleClickAndHold()
    {
        // yield left-click entirely to BuildModeController
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
        if (IsPointerOverUI(screenPos)) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // Priority 1: Collectibles — check these first so items are always clickable
        if (Physics.Raycast(ray, out RaycastHit hitItem, raycastMaxDistance, collectibleLayer))
        {
            InteractableComponent interactable = hitItem.collider.GetComponentInParent<InteractableComponent>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }

        // Priority 2: Selectable entities
        if (Physics.Raycast(ray, out RaycastHit hitSelectable, raycastMaxDistance, selectableLayer))
        {
            SelectableComponent selectable = hitSelectable.collider.GetComponentInParent<SelectableComponent>();
            if (SelectionManager.Instance.CurrentSelected == selectable)
            {
                var node = selectable.GetComponent<ResourceNode>();
                if(node != null)
                {
                    node.TryHarvest();
                }
                return;
            }
            if (selectable != null)
            {
                SelectionManager.Instance.Select(selectable);
                return;
            }
        }

        // Priority 3: Nothing hit — deselect
        SelectionManager.Instance.Deselect();
    }


    private void HandleHoldAction(Vector2 screenPos)
    {
        if (IsPointerOverUI(screenPos)) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance))
            CollectInRadius(hit.point);
    }

    // ── CollectInRadius — unchanged, WorldItemPickup fires the signal internally

    private void CollectInRadius(Vector3 worldPos)
    {
        Collider[] hits = Physics.OverlapSphere(worldPos, collectRadius, collectibleLayer);
        foreach (Collider col in hits)
        {
            InteractableComponent interactable = col.GetComponentInParent<InteractableComponent>();
            interactable?.Interact();
        }
    }

    // ── UI detection — unchanged ───────────────────────────────────────────

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

    // ── Unchanged helpers ──────────────────────────────────────────────────

    private Vector3 RaycastGroundPlane(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance)) return hit.point;

        if (Mathf.Abs(ray.direction.y) > 0.001f)
        {
            float t = -ray.origin.y / ray.direction.y;
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
        followTarget.position = pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (followTarget == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(followTarget.position, collectRadius);
    }
}
