using UnityEngine;

namespace Game.Core.Damage
{
    public enum DamageType
    {
        Generic,
        Bullet,
        Explosive,
        Melee,
        Fall
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;

        public readonly GameObject Instigator; // quem causou
        public readonly GameObject Source;     // arma/projétil etc.

        public DamageInfo(float amount, DamageType type, Vector3 point, Vector3 normal, GameObject instigator, GameObject source)
        {
            Amount = amount;
            Type = type;
            Point = point;
            Normal = normal;
            Instigator = instigator;
            Source = source;
        }
    }
}