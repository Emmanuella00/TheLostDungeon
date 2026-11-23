using System;
using System.Collections;
using TheLostDungeon.Levels;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace TheLostDungeon.Content
{
    /// <summary>
    /// ASSET &amp; CONTENT MANAGEMENT (Unity Addressables): loads each level scene from its
    /// AssetBundle at runtime and releases it when the player leaves.
    ///
    /// Lifecycle:
    ///   Load    - Addressables.LoadSceneAsync (Single mode) with ReleaseSceneWhenSceneUnloaded,
    ///             so the scene's bundles are released when another scene replaces it.
    ///   Release - as a safety net we also release the previous handle explicitly once the new
    ///             scene is in, so reference counts can't leak between levels.
    ///   Failure - a missing/corrupt bundle reports a readable reason through onFailed; the game
    ///             never hangs on the loading screen.
    /// </summary>
    public class AddressablesSceneLoader : ISceneContentLoader
    {
        private AsyncOperationHandle<SceneInstance> currentLevel;

        public IEnumerator LoadLevel(LevelDefinition level, Action<float> onProgress, Action<string> onFailed)
        {
            if (level == null || !level.HasAddressableScene)
            {
                onFailed?.Invoke("This level has no Addressable scene. Run Tools > The Lost Dungeon > Setup Addressables.");
                yield break;
            }

            AsyncOperationHandle<SceneInstance> previous = currentLevel;
            AsyncOperationHandle<SceneInstance> handle;

            try
            {
                handle = Addressables.LoadSceneAsync(level.SceneAsset.RuntimeKey, LoadSceneMode.Single, true, 100,
                                                     SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
            }
            catch (Exception e)
            {
                onFailed?.Invoke($"Couldn't start loading '{level.DisplayName}': {e.Message}");
                yield break;
            }

            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                yield return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                string reason = handle.OperationException != null ? handle.OperationException.Message : "unknown error";
                if (handle.IsValid()) Addressables.Release(handle);
                onFailed?.Invoke($"Couldn't load '{level.DisplayName}': {reason}");
                yield break;
            }

            onProgress?.Invoke(1f);
            currentLevel = handle;
            if (previous.IsValid()) Addressables.Release(previous);   // safety net (normally already released)
        }

        public void ReleaseCurrentLevel()
        {
            if (currentLevel.IsValid()) Addressables.Release(currentLevel);
            currentLevel = default;
        }
    }
}
