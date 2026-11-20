namespace TheLostDungeon.Pooling
{
    /// <summary>
    /// Optional hooks for pooled objects. Because pooled objects are reused, they must reset
    /// themselves (timers, trails, velocity) in OnSpawn instead of relying on Awake/Start.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}
