namespace TheLostDungeon.Core
{
    /// <summary>High-level states of the whole game. Owned by <see cref="GameManager"/>.</summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        Loading,
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }
}
