using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ObjectPooler singleton. 
/// - Use Spawn(prefab, pos, rot) para pegar uma instância.
/// - Use Despawn(instance) para retornar ao pool.
/// - Prewarm(prefab, count) para pré-instanciar.
/// - Trabalha com qualquer prefab; se o objeto tiver IPoolable, chamará callbacks.
/// </summary>
public class ObjectPooler : Singleton<ObjectPooler>
{
    [Serializable]
    public class PoolStats
    {
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 100; // 0 = unlimited
    }

    [Tooltip("Pools configuráveis (opcional). Você pode prewarm via inspector.")]
    public PoolStats[] pools;

    // Internal storage: key = prefab, value = queue of inactive instances
    private readonly Dictionary<GameObject, Queue<GameObject>> poolQueues = new();
    private readonly Dictionary<GameObject, Transform> poolParents = new(); // container per prefab
    private readonly Dictionary<GameObject, int> currentCounts = new(); // total created

    protected override void Awake()
    {
        // Initialize configured pools
        foreach (var p in pools)
        {
            if (p == null || p.prefab == null) continue;
            CreatePoolContainer(p.prefab);
            Prewarm(p.prefab, p.initialSize);
            if (!currentCounts.ContainsKey(p.prefab)) currentCounts[p.prefab] = p.initialSize;
        }
    }

    private Transform CreatePoolContainer(GameObject prefab)
    {
        if (poolParents.ContainsKey(prefab)) return poolParents[prefab];
        var go = new GameObject($"Pool::{prefab.name}");
        go.transform.SetParent(transform, false);
        poolParents[prefab] = go.transform;
        poolQueues[prefab] = new Queue<GameObject>();
        currentCounts[prefab] = 0;
        return poolParents[prefab];
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));
        if (!poolQueues.ContainsKey(prefab)) CreatePoolContainer(prefab);

        var queue = poolQueues[prefab];
        GameObject instance = null;

        // Reuse or instantiate
        if (queue.Count > 0)
        {
            instance = queue.Dequeue();
            instance.transform.SetParent(null);
            instance.SetActive(true);
        }
        else
        {
            // Check max size if configured in pools array
            var stats = Array.Find(pools, p => p != null && p.prefab == prefab);
            if (stats != null && stats.maxSize > 0 && currentCounts[prefab] >= stats.maxSize)
            {
                // max reached - fallback: reuse oldest active by instantiating and destroying? 
                // Simpler: instantiate anyway but log a warning (avoid silent failure).
                Debug.LogWarning($"[ObjectPooler] Max pool size reached for {prefab.name} ({stats.maxSize}). Instantiating extra object.");
            }

            instance = Instantiate(prefab, position, rotation);
            currentCounts[prefab] = currentCounts.ContainsKey(prefab) ? currentCounts[prefab] + 1 : 1;
        }

        instance.transform.position = position;
        instance.transform.rotation = rotation;

        // Notify poolable
        var poolable = instance.GetComponent<IPoolable>();
        poolable?.OnSpawned();

        return instance;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null) return;

        // call OnDespawned if IPoolable
        var poolable = instance.GetComponent<IPoolable>();
        poolable?.OnDespawned();

        // Attempt to find original prefab key:
        // We store original prefab as a hidden component? Simplest: require that pooled instances have PoolMember component.
        var member = instance.GetComponent<PoolMember>();
        if (member != null && member.OriginalPrefab != null)
        {
            var prefab = member.OriginalPrefab;
            if (!poolQueues.ContainsKey(prefab)) CreatePoolContainer(prefab);
            instance.SetActive(false);
            instance.transform.SetParent(poolParents[prefab], false);
            poolQueues[prefab].Enqueue(instance);
            return;
        }

        // If no PoolMember present, fallback: destroy (not ideal). To avoid this, our Spawn will add PoolMember to clones.
        // But we can also try to match by prefab name - risky. So: if no PoolMember, destroy and warn.
        Debug.LogWarning($"[ObjectPooler] Despawning instance without PoolMember: {instance.name}. Destroying.");
        Destroy(instance);
    }

    /// <summary>
    /// Prewarm: creates `count` instances of prefab and places them in the pool (inactive).
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));
        if (!poolQueues.ContainsKey(prefab)) CreatePoolContainer(prefab);

        var parent = poolParents[prefab];
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, parent);
            go.SetActive(false);

            // attach PoolMember so we can map back to prefab on Despawn
            var member = go.GetComponent<PoolMember>();
            if (member == null) member = go.AddComponent<PoolMember>();
            member.OriginalPrefab = prefab;

            poolQueues[prefab].Enqueue(go);
            currentCounts[prefab] = currentCounts.ContainsKey(prefab) ? currentCounts[prefab] + 1 : 1;
        }
    }

    /// <summary>
    /// Spawns and ensures the returned GameObject has PoolMember set (if newly instantiated).
    /// Use this overload if you want to get typed component.
    /// </summary>
    public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        var go = Spawn(prefab, position, rotation);
        // ensure PoolMember exists on newly created clones
        var member = go.GetComponent<PoolMember>();
        if (member == null)
        {
            member = go.AddComponent<PoolMember>();
            member.OriginalPrefab = prefab;
        }
        return go.GetComponent<T>();
    }

    /// <summary> Return pool size for a prefab (inactive count) </summary>
    public int GetInactiveCount(GameObject prefab)
    {
        if (!poolQueues.ContainsKey(prefab)) return 0;
        return poolQueues[prefab].Count;
    }
}

/// <summary>
/// Helper component to mark pooled instantiated objects so we know their original prefab on Despawn.
/// </summary>
public class PoolMember : MonoBehaviour
{
    [HideInInspector] public GameObject OriginalPrefab;
}