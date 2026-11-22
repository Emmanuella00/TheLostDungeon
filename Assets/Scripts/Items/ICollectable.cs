using UnityEngine;

namespace TheLostDungeon.Items
{
    /// <summary>
    /// Things picked up automatically by walking into them (keycards, health packs, credits).
    /// Returns false if it can't be collected right now (e.g. health pack at full health), so it
    /// stays in the world.
    /// </summary>
    public interface ICollectable
    {
        bool TryCollect(GameObject collector);
    }
}
