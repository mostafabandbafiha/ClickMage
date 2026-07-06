using UnityEngine;
using deVoid.Utils;

public class WorldItemSpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Spawn Settings")]
    [Tooltip("Extra upward offset applied on top of whatever DragDropController sends.")]
    [SerializeField] private float _additionalHeightOffset = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable()
        => Signals.Get<ItemDroppedToWorldSignal>().AddListener(OnItemDropped);

    private void OnDisable()
        => Signals.Get<ItemDroppedToWorldSignal>().RemoveListener(OnItemDropped);

    // ── Handler ───────────────────────────────────────────────────────────────
    private void OnItemDropped(ItemDroppedToWorldData data)
    {
        if (data.Stack.IsEmpty || data.Stack.Data == null)
        {
            Debug.LogWarning("[WorldItemSpawner] Received empty or invalid stack.");
            return;
        }

        if (data.Stack.Data.WorldPrefab == null)
        {
            Debug.LogWarning($"[WorldItemSpawner] '{data.Stack.Data.name}' has no WorldPrefab assigned.");
            return;
        }

        int count = data.Stack.Amount;

        for (int i = 0; i < count; i++)
        {
            // First item spawns exactly at drop point.
            // Extra items scatter inside DropRadius.
            Vector3 spawnPos = data.WorldPosition
                + Vector3.up * _additionalHeightOffset;

            if (count > 1)
            {
                Vector2 circle = Random.insideUnitCircle * data.DropRadius;
                spawnPos += new Vector3(circle.x, 0f, circle.y);
            }

            // Random Y rotation so items don't all face the same direction
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject spawned = Instantiate(data.Stack.Data.WorldPrefab, spawnPos, rot);

            // Each world object represents exactly 1 item
            if (spawned.TryGetComponent<WorldItemPickup>(out var pickup))
                pickup.Init(new ItemStack(data.Stack.Data, 1));

            Debug.Log($"[WorldItemSpawner] Spawned '{data.Stack.Data.name}' #{i + 1}/{count} at {spawnPos}");
        }
    }
}
