using System.Collections.Generic;
using UnityEngine;
using Game.Core.Events;

namespace Game.Core.Pooling
{
    public sealed class PoolService : IPoolService
    {
        private readonly IEventBus _eventBus;
        private readonly PoolCatalog _catalog;
        private readonly Transform _root;

        private readonly Dictionary<string, ObjectPool> _poolsById = new();
        private readonly Dictionary<int, ObjectPool> _poolByInstanceId = new();

        public PoolService(IEventBus eventBus, PoolCatalog catalog, Transform root)
        {
            _eventBus = eventBus;
            _catalog = catalog;
            _root = root;

            BuildPools();
        }

        private void BuildPools()
        {
            if (_catalog == null || _catalog.Entries == null) return;

            foreach (var e in _catalog.Entries)
            {
                if (string.IsNullOrWhiteSpace(e.Id) || e.Prefab == null) continue;
                if (_poolsById.ContainsKey(e.Id))
                {
                    Debug.LogWarning($"PoolService: Pool duplicado: {e.Id}");
                    continue;
                }

                var poolParent = new GameObject($"Pool_{e.Id}").transform;
                poolParent.SetParent(_root, worldPositionStays: false);

                _poolsById.Add(e.Id, new ObjectPool(e.Id, e.Prefab, poolParent, e.MaxSize));
            }
        }

        public void WarmupAll()
        {
            if (_catalog == null || _catalog.Entries == null) return;

            foreach (var e in _catalog.Entries)
            {
                if (!_poolsById.TryGetValue(e.Id, out var pool)) continue;
                pool.Prewarm(e.Prewarm, RegisterInstancePool);
            }
        }

        public GameObject Spawn(PoolKey key, Vector3 position, Quaternion rotation)
        {
            if (key.Id == null || !_poolsById.TryGetValue(key.Id, out var pool))
            {
                Debug.LogError($"PoolService: Pool não encontrado: {key}");
                return null;
            }

            var go = pool.Spawn(position, rotation, RegisterInstancePool);
            return go;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            var id = instance.GetInstanceID();
            if (_poolByInstanceId.TryGetValue(id, out var pool))
            {
                pool.Despawn(instance);
                return;
            }

            // fallback: se não foi criado por pool, desativa apenas
            instance.SetActive(false);
        }

        private void RegisterInstancePool(GameObject instance, ObjectPool pool)
        {
            _poolByInstanceId[instance.GetInstanceID()] = pool;
        }

        private sealed class ObjectPool
        {
            private readonly string _id;
            private readonly GameObject _prefab;
            private readonly Transform _parent;
            private readonly int _maxSize;

            private readonly Stack<GameObject> _stack = new();
            private int _created;

            public ObjectPool(string id, GameObject prefab, Transform parent, int maxSize)
            {
                _id = id;
                _prefab = prefab;
                _parent = parent;
                _maxSize = Mathf.Max(1, maxSize);
            }

            public void Prewarm(int count, System.Action<GameObject, ObjectPool> register)
            {
                count = Mathf.Max(0, count);
                for (int i = 0; i < count; i++)
                {
                    if (_created >= _maxSize) break;
                    var go = CreateInstance(register);
                    go.SetActive(false);
                    _stack.Push(go);
                }
            }

            public GameObject Spawn(Vector3 pos, Quaternion rot, System.Action<GameObject, ObjectPool> register)
            {
                GameObject go;
                if (_stack.Count > 0)
                {
                    go = _stack.Pop();
                }
                else
                {
                    if (_created >= _maxSize)
                    {
                        // Estratégia: reaproveitar “o mais velho” não é trivial sem fila.
                        // Por enquanto: instancia extra (ou retorna null). Aqui escolho instanciar extra com warning.
                        Debug.LogWarning($"Pool({_id}): MaxSize atingido ({_maxSize}). Instanciando extra.");
                    }
                    go = CreateInstance(register);
                }

                var t = go.transform;
                t.SetPositionAndRotation(pos, rot);
                go.SetActive(true);

                // Notifica poolable(s)
                var poolables = go.GetComponentsInChildren<IPoolable>(true);
                for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawned();

                return go;
            }

            public void Despawn(GameObject go)
            {
                if (go == null) return;

                var poolables = go.GetComponentsInChildren<IPoolable>(true);
                for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawned();

                go.SetActive(false);
                go.transform.SetParent(_parent, worldPositionStays: false);
                _stack.Push(go);
            }

            private GameObject CreateInstance(System.Action<GameObject, ObjectPool> register)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.name = $"{_prefab.name}_Pooled";
                _created++;
                register?.Invoke(go, this);
                return go;
            }
        }
    }
}