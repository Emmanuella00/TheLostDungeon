using TheLostDungeon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// FAILURE HANDLING popup for Addressables: if a level can't be loaded (missing bundle,
    /// bad key, no connection for remote content) the player sees why, and can Retry or go
    /// back to the Main Menu instead of being stuck on a loading screen.
    /// </summary>
    public class MessageScreen : UIScreen
    {
        private Text title;
        private Text body;
        private Button retryButton;

        protected override Selectable FirstSelected => retryButton;

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Dim);

            Image panel = UIBuilder.Panel(Root, "Panel", UITheme.PanelColour);
            UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 520f));
            UIBuilder.Vertical(panel, 16f, TextAnchor.UpperCenter, 40);

            title = UIBuilder.Size(UIBuilder.Label(panel.transform, "Title", "LOAD FAILED", 48, UITheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold), 820f, 64f);
            body = UIBuilder.Size(UIBuilder.Label(panel.transform, "Body", "", 22, UITheme.Text, TextAnchor.UpperCenter), 820f, 190f);

            RectTransform buttons = UIBuilder.Rect("Buttons", panel.transform);
            UIBuilder.Size(buttons, 820f, 70f);
            UIBuilder.Horizontal(buttons, 20f, TextAnchor.MiddleCenter);
            retryButton = UIBuilder.Button(buttons, "Retry", Retry, 300f, 64f);
            UIBuilder.Button(buttons, "Main Menu", MainMenu, 300f, 64f, isBack: true);
        }

        public void ShowLoadFailure(string reason)
        {
            title.text = "LOAD FAILED";
            body.text = "The level couldn't be loaded.\n\n" + reason;
            Show();
            BringToFront();
        }

        public void BringToFront() => Root.SetAsLastSibling();

        private void Retry()
        {
            Hide();
            if (LevelManager.HasInstance) LevelManager.Instance.TryLoadLevel(LevelManager.Instance.LastRequestedLevelIndex);
        }

        private void MainMenu()
        {
            Hide();
            UI.GoToMainMenu();
        }
    }
}
