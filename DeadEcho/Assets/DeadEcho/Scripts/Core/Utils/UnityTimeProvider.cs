using UnityEngine;

namespace Game.Core.Utils
{
    public sealed class UnityTimeProvider : ITimeProvider
    {
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float Time => UnityEngine.Time.time;
    }
}