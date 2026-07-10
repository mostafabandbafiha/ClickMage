using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}



public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private readonly Dictionary<int, Queue<GameObject>> _pools = new();
    private Transform _root;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _root = transform;
        DontDestroyOnLoad(gameObject);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        int id = prefab.GetInstanceID();
        if (!_pools.ContainsKey(id)) _pools[id] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var obj = CreateNew(prefab, id);
            obj.SetActive(false);
            _pools[id].Enqueue(obj);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int id = prefab.GetInstanceID();
        if (!_pools.TryGetValue(id, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[id] = queue;
        }

        GameObject obj = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefab, id);

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        foreach (var p in obj.GetComponents<IPoolable>())
            p.OnSpawn();

        return obj;
    }

    public void Release(GameObject obj)
    {
        var tag = obj.GetComponent<PooledObjectTag>();
        if (tag == null || !_pools.ContainsKey(tag.PrefabId))
        {
            Destroy(obj); // wasn't pooled - safe fallback
            return;
        }

        foreach (var p in obj.GetComponents<IPoolable>())
            p.OnDespawn();

        obj.SetActive(false);
        obj.transform.SetParent(_root);
        _pools[tag.PrefabId].Enqueue(obj);
    }

    private GameObject CreateNew(GameObject prefab, int id)
    {
        var obj = Instantiate(prefab, _root);
        var tag = obj.AddComponent<PooledObjectTag>();
        tag.PrefabId = id;
        return obj;
    }
}