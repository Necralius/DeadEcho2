namespace Game.Core.Damage
{
    public interface IDamageable
    {
        void ApplyDamage(in DamageInfo info);
    }
}