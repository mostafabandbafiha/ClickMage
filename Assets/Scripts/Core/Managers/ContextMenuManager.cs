using UnityEngine;
using ClickMage.Interaction;
using System;

public class ContextMenuManager : MonoBehaviour
{
    public static ContextMenuManager Instance { get; private set; }

    [SerializeField] private ContextUIMenu contextMenuPrefab;

    private ContextUIMenu _activeMenu;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SelectionManager.Instance.OnDeselected += OnDeselect;
    }

    private void OnDestroy()
    {
        SelectionManager.Instance.OnDeselected -= OnDeselect;
    }

    private void OnDeselect(SelectableComponent component)
    {
        CloseMenu();
    }

    public void OpenMenu(BaseCharacter character, GameObject target)
    {
        CloseMenu();

        var actions = target.GetComponents<IContextAction>();
        if (actions.Length == 0) return;

        _activeMenu = Instantiate(contextMenuPrefab, target.transform.position, Quaternion.identity);
        _activeMenu.Build(character, actions);
    }

    public void CloseMenu()
    {
        if (_activeMenu == null) return;
        Destroy(_activeMenu.gameObject);
        _activeMenu = null;
    }

    public void OpenBuildMenu(Transform ghostTransform)
    {
        CloseMenu();
        var menu = Instantiate(contextMenuPrefab, ghostTransform.position + Vector3.up * 2f, Quaternion.identity);
        menu.SetFollowTarget(ghostTransform);
        menu.BuildForStructure(
            () => BuildModeController.Instance.ConfirmCurrent(),
            () => BuildModeController.Instance.CancelCurrent()
        );
        _activeMenu = menu;
    }



}
