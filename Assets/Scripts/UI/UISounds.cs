using TheLostDungeon.Audio;

namespace TheLostDungeon.UI
{
    /// <summary>UI sound shortcuts. Safe to call when no AudioManager exists (e.g. tests).</summary>
    public static class UISounds
    {
        public static void Click()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayUIClick();
        }

        public static void Back()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayUIBack();
        }

        public static void Locked()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayUILocked();
        }
    }
}
