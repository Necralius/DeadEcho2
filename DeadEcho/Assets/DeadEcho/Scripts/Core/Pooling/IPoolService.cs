using UnityEngine;

namespace Game.Core.Pooling
{
    public interface IPoolService
    {
        void WarmupAll();
        GameObject Spawn(PoolKey key, Vector3 position, Quaternion rotation);
        void Despawn(GameObject instance);
    }
}
