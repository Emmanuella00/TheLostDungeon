namespace TheLostDungeon.Combat
{
    /// <summary>
    /// Anything that can be hurt: the player, enemies, breakable crates, explosive barrels.
    /// Bolts, melee strikes and explosions only look for this interface, so they work on every
    /// damageable thing, including ones added later, without any changes.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        DamageResult TakeDamage(in DamageInfo info);
    }
}
