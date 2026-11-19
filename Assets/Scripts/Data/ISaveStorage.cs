namespace TheLostDungeon.Data
{
    /// <summary>
    /// Where the JSON text physically lives. SaveManager only talks to this interface, so the
    /// storage can differ per platform (file on PC/mobile, browser storage on WebGL) without
    /// SaveManager changing at all.
    /// </summary>
    public interface ISaveStorage
    {
        bool Exists();
        string Read();
        void Write(string json);
        void Delete();
    }
}
