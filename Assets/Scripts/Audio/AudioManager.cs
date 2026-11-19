using System.Collections;
using TheLostDungeon.Core;
using TheLostDungeon.Data;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Audio
{
    /// <summary>
    /// Plays music, gameplay sounds and UI sounds.
    ///
    /// It is an OBSERVER: it subscribes to GameEvents and plays the right sound when something
    /// happens (enemy defeated, item collected, level complete...). Gameplay code never calls
    /// "play sound" for these; it just raises the event.
    /// UI buttons call PlayUIClick()/PlayUIBack() directly, which is fine because UI → audio
    /// is a one-way dependency on a global service.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private SoundLibrary library;
        [SerializeField, Range(0f, 1f)] private float pausedMusicFactor = 0.35f;
        [SerializeField, Min(0f)] private float musicFadeSeconds = 0.75f;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioSource uiSource;
        private Coroutine musicFade;
        private float musicDuck = 1f;

        public SoundLibrary Library => library;

        protected override void OnSingletonAwake()
        {
            musicSource = CreateSource("Music", loop: true);
            sfxSource = CreateSource("SFX", loop: false);
            uiSource = CreateSource("UI", loop: false);
            uiSource.ignoreListenerPause = true;
            ApplyVolumes();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += HandleGameStateChanged;
            GameEvents.EnemyDefeated += HandleEnemyDefeated;
            GameEvents.ItemCollected += HandleItemCollected;
            GameEvents.AbilityActivated += HandleAbilityActivated;
            GameEvents.PlayerDied += HandlePlayerDied;
            GameEvents.LevelCompleted += HandleLevelCompleted;
            GameEvents.SettingsChanged += ApplyVolumes;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= HandleGameStateChanged;
            GameEvents.EnemyDefeated -= HandleEnemyDefeated;
            GameEvents.ItemCollected -= HandleItemCollected;
            GameEvents.AbilityActivated -= HandleAbilityActivated;
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.LevelCompleted -= HandleLevelCompleted;
            GameEvents.SettingsChanged -= ApplyVolumes;
        }

        // ---------- Public API ----------

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource.clip == clip) return;
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(CrossFadeMusic(clip));
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null) sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void PlayUI(AudioClip clip)
        {
            if (clip != null) uiSource.PlayOneShot(clip);
        }

        public void PlayUIClick() => PlayUI(library != null ? library.uiClick : null);
        public void PlayUIBack() => PlayUI(library != null ? library.uiBack : null);
        public void PlayUILocked() => PlayUI(library != null ? library.uiLocked : null);

        // ---------- Event handlers ----------

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            if (library == null) return;

            switch (current)
            {
                case GameState.MainMenu:
                    PlayMusic(library.menuMusic);
                    break;
                case GameState.Playing:
                    PlayMusic(library.levelMusic);
                    break;
                case GameState.GameOver:
                    PlaySfx(library.gameOver);
                    break;
            }

            // Duck the music while paused instead of stopping it.
            musicDuck = current == GameState.Paused ? pausedMusicFactor : 1f;
            ApplyVolumes();
        }

        private void HandleEnemyDefeated(EnemyDefeatedInfo info) => PlaySfx(library != null ? library.enemyDefeated : null);
        private void HandleItemCollected(ItemCollectedInfo info) => PlaySfx(library != null ? library.itemCollected : null);
        private void HandleAbilityActivated(string abilityId, float cooldown) => PlaySfx(library != null ? library.abilityActivated : null);
        private void HandlePlayerDied() => PlaySfx(library != null ? library.playerDied : null);
        private void HandleLevelCompleted(LevelResult result) => PlaySfx(library != null ? library.levelComplete : null);

        // ---------- Internals ----------

        private void ApplyVolumes()
        {
            AudioListener.volume = GameSettings.MasterVolume;
            if (musicSource != null && musicFade == null) musicSource.volume = TargetMusicVolume;
            if (sfxSource != null) sfxSource.volume = GameSettings.SfxVolume;
            if (uiSource != null) uiSource.volume = GameSettings.SfxVolume;
        }

        private float TargetMusicVolume => GameSettings.MusicVolume * musicDuck;

        private IEnumerator CrossFadeMusic(AudioClip next)
        {
            float half = musicFadeSeconds * 0.5f;
            float start = musicSource.volume;

            // Unscaled time so fades still work while the game is paused (timeScale = 0).
            for (float t = 0f; t < half && musicSource.isPlaying; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(start, 0f, t / half);
                yield return null;
            }

            musicSource.clip = next;
            musicSource.Play();

            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, TargetMusicVolume, t / half);
                yield return null;
            }

            musicSource.volume = TargetMusicVolume;
            musicFade = null;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
