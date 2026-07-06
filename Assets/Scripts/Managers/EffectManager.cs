using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[System.Serializable]
public class EffectData
{
    public string effectName;
    public GameObject effectPrefab;
    public float duration = 2f; // How long the effect lasts
    public bool useObjectPool = true;
    public int poolSize = 5;
}

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("Effect Settings")]
    public EffectData[] effects;

    // Dictionary for quick effect lookup
    private Dictionary<string, EffectData> effectDict = new Dictionary<string, EffectData>();

    // Object pooling
    private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, List<GameObject>> activeEffects = new Dictionary<string, List<GameObject>>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEffects();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeEffects()
    {
        foreach (var effect in effects)
        {
            if (effect.effectPrefab != null)
            {
                effectDict[effect.effectName] = effect;

                if (effect.useObjectPool)
                {
                    CreatePool(effect);
                }
            }
        }
    }

    void CreatePool(EffectData effectData)
    {
        Queue<GameObject> pool = new Queue<GameObject>();
        List<GameObject> activeList = new List<GameObject>();

        // Pre-instantiate pool objects
        for (int i = 0; i < effectData.poolSize; i++)
        {
            GameObject pooledEffect = Instantiate(effectData.effectPrefab);
            pooledEffect.SetActive(false);
            pooledEffect.transform.SetParent(transform); // Keep hierarchy clean
            pool.Enqueue(pooledEffect);
        }

        effectPools[effectData.effectName] = pool;
        activeEffects[effectData.effectName] = activeList;
    }

    public GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation = default)
    {
        if (!effectDict.ContainsKey(effectName))
        {
            Debug.LogWarning($"Effect '{effectName}' not found in EffectManager!");
            return null;
        }

        EffectData effectData = effectDict[effectName];
        GameObject effectInstance = null;

        if (effectData.useObjectPool && effectPools.ContainsKey(effectName))
        {
            effectInstance = GetPooledEffect(effectName);
        }
        else
        {
            // Create new instance if not using pooling
            effectInstance = Instantiate(effectData.effectPrefab);
        }

        if (effectInstance != null)
        {
            // Set position and rotation
            effectInstance.transform.position = position;
            effectInstance.transform.rotation = rotation == default ? Quaternion.identity : rotation;
            effectInstance.SetActive(true);

            // Play particle system if it exists
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }

            // Start coroutine to return to pool or destroy after duration
            if (effectData.duration >= 0)
            {
                StartCoroutine(HandleEffectDuration(effectName, effectInstance, effectData.duration));
            }
            
        }

        return effectInstance;
    }

    public GameObject PlayEffect(string effectName, Transform parent, Vector3 localPosition = default)
    {
        GameObject effect = PlayEffect(effectName, parent.position + localPosition);
        if (effect != null)
        {
            effect.transform.SetParent(parent);
            effect.transform.localPosition = localPosition;
        }
        return effect;
    }

    GameObject GetPooledEffect(string effectName)
    {
        if (effectPools[effectName].Count > 0)
        {
            GameObject pooledEffect = effectPools[effectName].Dequeue();
            activeEffects[effectName].Add(pooledEffect);
            return pooledEffect;
        }
        else
        {
            // Pool is empty, create new instance
            Debug.Log($"Pool for '{effectName}' is empty, creating new instance");
            GameObject newEffect = Instantiate(effectDict[effectName].effectPrefab);
            activeEffects[effectName].Add(newEffect);
            return newEffect;
        }
    }

    IEnumerator HandleEffectDuration(string effectName, GameObject effectInstance, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (effectInstance != null)
        {
            ReturnToPool(effectName, effectInstance);
        }
    }

    public void ReturnToPool(string effectName, GameObject effectInstance)
    {
        if (effectDict[effectName].useObjectPool)
        {
            // Return to pool
            effectInstance.SetActive(false);
            effectInstance.transform.SetParent(transform);
            effectPools[effectName].Enqueue(effectInstance);
            activeEffects[effectName].Remove(effectInstance);
        }
        else
        {
            // Destroy if not using pooling
            Destroy(effectInstance);
        }
    }

    public void StopEffect(string effectName)
    {
        if (activeEffects.ContainsKey(effectName))
        {
            var effects = activeEffects[effectName].ToArray();
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    ReturnToPool(effectName, effect);
                }
            }
        }
    }

    public void StopAllEffects()
    {
        foreach (var effectName in activeEffects.Keys)
        {
            StopEffect(effectName);
        }
    }
}