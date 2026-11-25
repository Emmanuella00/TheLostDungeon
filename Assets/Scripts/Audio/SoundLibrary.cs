using UnityEngine;

namespace TheLostDungeon.Audio
{
    /// <summary>
    /// All shared game sounds in one asset. Swapping a sound is a drag-and-drop in the Inspector;
    /// no code changes. (Enemy-specific sounds will live on each enemy's own data asset.)
    /// </summary>
    [CreateAssetMenu(menuName = "The Lost Dungeon/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip levelMusic;

        [Header("Player")]
        public AudioClip playerShoot;
        public AudioClip playerMelee;
        public AudioClip playerOverheat;
        public AudioClip playerHurt;

        [Header("Gameplay")]
        public AudioClip enemyDefeated;
        public AudioClip itemCollected;
        public AudioClip abilityActivated;
        public AudioClip playerDied;

        [Header("Stingers")]
        public AudioClip levelComplete;
        public AudioClip gameOver;
        [Tooltip("Played instead of Level Complete when the LAST level is finished.")]
        public AudioClip gameComplete;

        [Header("UI")]
        public AudioClip uiClick;
        public AudioClip uiBack;
        public AudioClip uiLocked;
    }
}
