using System;
using UnityEngine;

namespace Game.Core.Pooling
{
    [CreateAssetMenu(menuName = "Game/Core/Pool Catalog", fileName = "PoolCatalog")]
    public sealed class PoolCatalog : ScriptableObject
    {
        public PoolEntry[] Entries;

        [Serializable]
        public sealed class PoolEntry
        {
            public string Id;
            public GameObject Prefab;
            public int Prewarm = 16;
            public int MaxSize = 256;
        }
    }
}
