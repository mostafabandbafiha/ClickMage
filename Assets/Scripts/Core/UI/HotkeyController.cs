using System.Collections.Generic;
using UnityEngine;

public interface IHotkeyReceiver
{
    void OnHotkeyTriggered(string actionId);
}

/// <summary>
/// Central hotkey dispatcher. Maps KeyCode -> string action id,
/// fires that action to whatever has registered for it.
/// Knows nothing about panels, inventories, or anything else.
/// </summary>
public class HotkeyController : MonoBehaviour
{
    public static HotkeyController Instance { get; private set; }

    [System.Serializable]
    private struct KeyBinding
    {
        public string ActionId;
        public KeyCode Key;
    }

    [SerializeField] private KeyBinding[] _bindings;

    private readonly Dictionary<string, List<IHotkeyReceiver>> _subscribers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        foreach (var binding in _bindings)
            if (Input.GetKeyDown(binding.Key))
                Fire(binding.ActionId);
    }

    public void Register(string actionId, IHotkeyReceiver receiver)
    {
        if (!_subscribers.TryGetValue(actionId, out var list))
        {
            list = new List<IHotkeyReceiver>();
            _subscribers[actionId] = list;
        }
        if (!list.Contains(receiver))
            list.Add(receiver);
    }

    public void Unregister(string actionId, IHotkeyReceiver receiver)
    {
        if (_subscribers.TryGetValue(actionId, out var list))
            list.Remove(receiver);
    }

    private void Fire(string actionId)
    {
        if (!_subscribers.TryGetValue(actionId, out var list)) return;

        // snapshot in case a receiver unregisters itself mid-callback
        var snapshot = list.ToArray();
        foreach (var receiver in snapshot)
            receiver.OnHotkeyTriggered(actionId);
    }
}