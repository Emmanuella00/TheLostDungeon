using System;
using System.Collections;
using TheLostDungeon.Levels;

namespace TheLostDungeon.Content
{
    /// <summary>
    /// How level content gets loaded and released. LevelManager only talks to this interface, so
    /// the content system could be swapped (e.g. AssetBundles or Content Directories) without
    /// LevelManager or any gameplay code changing.
    /// </summary>
    public interface ISceneContentLoader
    {
        /// <summary>Loads the level's scene, reporting 0..1 progress. Calls onFailed instead of throwing.</summary>
        IEnumerator LoadLevel(LevelDefinition level, Action<float> onProgress, Action<string> onFailed);

        /// <summary>Releases the currently loaded level content (call after leaving the level).</summary>
        void ReleaseCurrentLevel();
    }
}
