using UnityEngine;
using ClickMage.Interaction;

public class ContextUIMenu : MonoBehaviour
{
    [SerializeField] private ContextMenuButton buttonPrefab;
    [SerializeField] private Transform buttonContainer;

    private BaseCharacter _character;

    private Camera _cam;
    private Transform _followTarget;

    private void Start()
    {
        _cam = Camera.main;
    }

    public void SetFollowTarget(Transform target) => _followTarget = target;

    private void LateUpdate()
    {
        if (_followTarget != null)
            transform.position = _followTarget.position - Vector3.up * 5f;

        if (_cam == null) return;

        // match camera's facing direction, not point at camera position
        transform.rotation = _cam.transform.rotation;
    }

    public void Build(BaseCharacter character, IContextAction[] actions)
    {
        _character = character;

        foreach (var action in actions)
        {
            if (!action.IsAvailable(character)) continue;

            var btn = Instantiate(buttonPrefab, buttonContainer);
            btn.Setup(action.ActionLabel, () => OnActionSelected(action));
        }
    }

    public void BuildForStructure(System.Action onAccept, System.Action onCancel)
    {
        // clear any existing buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        var acceptBtn = Instantiate(buttonPrefab, buttonContainer);
        acceptBtn.Setup("Accept", () =>
        {
            onAccept();
            ContextMenuManager.Instance.CloseMenu();
        });

        var cancelBtn = Instantiate(buttonPrefab, buttonContainer);
        cancelBtn.Setup("Cancel", () =>
        {
            onCancel();
            ContextMenuManager.Instance.CloseMenu();
        });
    }

    private void OnActionSelected(IContextAction action)
    {
        action.Execute(_character);
        ContextMenuManager.Instance.CloseMenu();
    }
}
