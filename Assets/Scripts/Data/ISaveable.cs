namespace TheLostDungeon.Data
{
    /// <summary>
    /// Anything that owns data worth saving (inventory, unlocked abilities, ...) implements
    /// this and registers with SaveManager. SaveManager doesn't need to know what the object
    /// is; it just asks every ISaveable to write itself into / read itself from SaveData.
    /// </summary>
    public interface ISaveable
    {
        void CaptureState(SaveData data);
        void RestoreState(SaveData data);
    }
}
