using System.Collections.Generic;
using TheLostDungeon.Online;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// Online leaderboard (REST API): top campaign scores ranked with merge sort, the player's own
    /// row highlighted, an editable player name, and clear feedback for every network state:
    /// loading, online, offline (cached scores + queued score), local-only (no key configured).
    /// </summary>
    public class LeaderboardScreen : UIScreen
    {
        private const int Rows = 10;

        private class Row
        {
            public Image Background;
            public Text Rank, Name, Progress, Score;
            public Color BaseColour;
        }

        private readonly Row[] rows = new Row[Rows];
        private Text statusText;
        private Image statusIcon;
        private Text yourScore;
        private InputField nameField;
        private Button refreshButton;
        private Text emptyText;

        protected override Selectable FirstSelected => refreshButton;

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Background);

            Text title = UIBuilder.Label(Root, "Title", "LEADERBOARD", 64, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1200f, 90f));

            // Status line: icon + text (Online / Offline / Loading...).
            RectTransform status = UIBuilder.Place(UIBuilder.Rect("Status", Root), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1200f, 40f));
            UIBuilder.Horizontal(status, 10f, TextAnchor.MiddleCenter);
            statusIcon = UIBuilder.Size(UIBuilder.Icon(status, "Icon", IconFactory.Check, UITheme.Health, 28f), 28f, 28f);
            statusText = UIBuilder.Size(UIBuilder.Label(status, "Text", "", 24, UITheme.TextMuted, TextAnchor.MiddleLeft), 900f, 40f);

            // Table.
            Image table = UIBuilder.Panel(Root, "Table", UITheme.PanelColour);
            UIBuilder.Place(table.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1100f, 640f));
            UIBuilder.Vertical(table, 4f, TextAnchor.UpperCenter, 20);
            BuildRow(table.transform, "Header", true);
            for (int i = 0; i < Rows; i++) rows[i] = BuildRow(table.transform, "Row_" + (i + 1), false);

            emptyText = UIBuilder.Label(table.transform, "Empty", "No scores yet. Complete a level to set one!", 26, UITheme.TextMuted);
            UIBuilder.GetOrAdd<LayoutElement>(emptyText.gameObject).ignoreLayout = true;
            UIBuilder.Place(emptyText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 60f));

            // Bottom bar: your score, name field, refresh, back.
            RectTransform bottom = UIBuilder.Place(UIBuilder.Rect("Bottom", Root), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1500f, 70f));
            UIBuilder.Horizontal(bottom, 16f, TextAnchor.MiddleCenter);
            yourScore = UIBuilder.Size(UIBuilder.Label(bottom, "YourScore", "", 24, UITheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold), 330f, 60f);
            nameField = UIBuilder.InputField(bottom, "NameField", "", 16, OnNameChanged, 320f);
            refreshButton = UIBuilder.Button(bottom, "Refresh", Refresh, 220f, 60f);
            UIBuilder.Button(bottom, "Back", () => UI.ShowMainMenu(), 220f, 60f, isBack: true);
        }

        private Row BuildRow(Transform parent, string name, bool header)
        {
            var row = new Row();
            row.Background = UIBuilder.Panel(parent, name, header ? new Color(0f, 0f, 0f, 0f) : new Color(1f, 1f, 1f, 0.04f));
            UIBuilder.Size(row.Background, 1060f, header ? 40f : 50f);
            UIBuilder.Horizontal(row.Background, 10f, TextAnchor.MiddleLeft, 8);
            row.BaseColour = row.Background.color;

            Color colour = header ? UITheme.Accent : UITheme.Text;
            int size = header ? 20 : 24;
            FontStyle style = header ? FontStyle.Bold : FontStyle.Normal;
            row.Rank = UIBuilder.Size(UIBuilder.Label(row.Background.transform, "Rank", header ? "#" : "", size, colour, TextAnchor.MiddleCenter, FontStyle.Bold), 80f, 40f);
            row.Name = UIBuilder.Size(UIBuilder.Label(row.Background.transform, "Name", header ? "PLAYER" : "", size, colour, TextAnchor.MiddleLeft, style), 480f, 40f);
            row.Progress = UIBuilder.Size(UIBuilder.Label(row.Background.transform, "Progress", header ? "LEVELS / STARS" : "", size, colour, TextAnchor.MiddleCenter, style), 240f, 40f);
            row.Score = UIBuilder.Size(UIBuilder.Label(row.Background.transform, "Score", header ? "SCORE" : "", size, colour, TextAnchor.MiddleRight, FontStyle.Bold), 210f, 40f);
            return row;
        }

        // Subscribed only while visible (the manager may be created after the UI).
        protected override void OnHide()
        {
            if (LeaderboardManager.HasInstance) LeaderboardManager.Instance.Changed -= Redraw;
        }

        protected override void OnShow()
        {
            if (!LeaderboardManager.HasInstance) return;
            LeaderboardManager.Instance.Changed -= Redraw;
            LeaderboardManager.Instance.Changed += Redraw;
            nameField.SetTextWithoutNotify(LeaderboardManager.Instance.PlayerName);
            Redraw();
            Refresh();
        }

        protected override void OnTick(float dt)
        {
            if (LeaderboardManager.HasInstance && LeaderboardManager.Instance.Status == LeaderboardStatus.Loading)
                statusIcon.rectTransform.Rotate(0f, 0f, -240f * dt);
            else
                statusIcon.rectTransform.localRotation = Quaternion.identity;
        }

        private void Refresh()
        {
            if (LeaderboardManager.HasInstance) LeaderboardManager.Instance.Refresh();
        }

        private void OnNameChanged(string value)
        {
            if (!LeaderboardManager.HasInstance) return;
            LeaderboardManager.Instance.PlayerName = value;
            nameField.SetTextWithoutNotify(LeaderboardManager.Instance.PlayerName);
        }

        private void Redraw()
        {
            if (!IsVisible || !LeaderboardManager.HasInstance) return;
            LeaderboardManager board = LeaderboardManager.Instance;

            List<LeaderboardEntry> entries = board.Entries;
            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                bool has = entries != null && i < entries.Count;
                row.Background.gameObject.SetActive(has);
                if (!has) continue;

                LeaderboardEntry e = entries[i];
                row.Rank.text = e.rank.ToString();
                row.Name.text = e.pending ? e.name + "  (not sent yet)" : e.name;
                row.Progress.text = $"{e.levels} / {e.stars}";
                row.Score.text = e.score.ToString();
                row.Rank.color = e.rank == 1 ? UITheme.Gold : UITheme.Text;
                row.Background.color = e.isLocalPlayer ? new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0.18f) : row.BaseColour;
            }
            emptyText.gameObject.SetActive(entries == null || entries.Count == 0);

            LeaderboardEntry mine = board.BuildPlayerEntry();
            yourScore.text = $"Your campaign score: {mine.score}";

            switch (board.Status)
            {
                case LeaderboardStatus.Loading:
                    SetStatus(IconFactory.Arc, UITheme.Accent, "Connecting to the leaderboard server...");
                    break;
                case LeaderboardStatus.Online:
                    SetStatus(IconFactory.Check, UITheme.Health, "Online: global top scores");
                    break;
                case LeaderboardStatus.Offline:
                    SetStatus(IconFactory.Ring, UITheme.Warning, "Offline: showing saved scores. Your score will be sent when you're back online. (" + board.LastError + ")");
                    break;
                case LeaderboardStatus.NotConfigured:
                    SetStatus(IconFactory.Ring, UITheme.TextMuted, "Local scores only (no online leaderboard key configured)");
                    break;
                default:
                    SetStatus(IconFactory.Ring, UITheme.TextMuted, "");
                    break;
            }
        }

        private void SetStatus(Sprite icon, Color colour, string text)
        {
            statusIcon.sprite = icon;
            statusIcon.color = colour;
            statusText.text = text;
        }
    }
}
