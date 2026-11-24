using System.Collections.Generic;
using TheLostDungeon.Core;
using TheLostDungeon.Data;
using TheLostDungeon.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// One card per level. The lock state is shown ONLY with icons (as the brief asks):
    ///   padlock = locked, play arrow = available, check mark = completed,
    /// plus 3 star icons for the best result. Locked cards are dimmed and play the "locked" sound.
    /// </summary>
    public class LevelSelectScreen : UIScreen
    {
        private class Card
        {
            public Button Button;
            public Image Background;
            public Image StatusIcon;
            public Image StatusRing;
            public Text Number;
            public Text Name;
            public Text Best;
            public Image[] Stars;
            public CanvasGroup Group;
            public float Shake;
        }

        private readonly List<Card> cards = new List<Card>();
        private RectTransform row;
        private Button backButton;

        protected override Selectable FirstSelected
        {
            get
            {
                foreach (Card card in cards)
                    if (card.Button.interactable && card.Group.alpha > 0.9f) return card.Button;
                return backButton;
            }
        }

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Background);

            Text title = UIBuilder.Label(Root, "Title", "SELECT LEVEL", 64, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1200f, 90f));

            row = UIBuilder.Place(UIBuilder.Rect("Cards", Root), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1700f, 460f));
            UIBuilder.Horizontal(row, 26f, TextAnchor.MiddleCenter);

            backButton = UIBuilder.Button(Root, "Back", () => UI.ShowMainMenu(), 300f, 64f, isBack: true);
            UIBuilder.GetOrAdd<LayoutElement>(backButton.gameObject).ignoreLayout = true;
            UIBuilder.Place((RectTransform)backButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(300f, 64f));

            BuildLegend();
            EnsureCards(DefaultCardCount);   // pre-built so the cards are editable in the baked prefab
        }

        private const int DefaultCardCount = 5;

        private void BuildLegend()
        {
            RectTransform legend = UIBuilder.Place(UIBuilder.Rect("Legend", Root), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(900f, 40f));
            UIBuilder.Horizontal(legend, 14f, TextAnchor.MiddleCenter);
            LegendItem(legend, "Locked", IconFactory.Lock, UITheme.Locked);
            LegendItem(legend, "Available", IconFactory.Play, UITheme.Accent);
            LegendItem(legend, "Completed", IconFactory.Check, UITheme.Health);
            LegendItem(legend, "Star", IconFactory.Star, UITheme.Gold);
        }

        private static void LegendItem(Transform parent, string name, Sprite icon, Color colour)
        {
            UIBuilder.Size(UIBuilder.Icon(parent, "Legend_" + name, icon, colour, 30f), 30f, 30f);
        }

        protected override void OnShow()
        {
            if (!LevelManager.HasInstance) return;
            LevelManager levels = LevelManager.Instance;
            EnsureCards(levels.LevelCount);

            LevelProgression progression = levels.Progression;
            for (int i = 0; i < cards.Count; i++)
            {
                LevelDefinition definition = levels.Levels[i];
                LevelStatus status = progression.GetStatus(i);
                LevelProgressData data = FindProgress(i);
                Refresh(cards[i], i, definition, status, data);
            }
        }

        protected override void OnTick(float dt)
        {
            foreach (Card card in cards)
            {
                if (card.Shake <= 0f) continue;
                card.Shake = Mathf.Max(0f, card.Shake - dt);
                float offset = Mathf.Sin(card.Shake * 60f) * 12f * card.Shake / 0.35f;
                card.Background.rectTransform.anchoredPosition = new Vector2(offset, 0f);
            }
        }

        private void EnsureCards(int count)
        {
            while (cards.Count < count) cards.Add(CreateCard(cards.Count));
        }

        private Card CreateCard(int index)
        {
            var card = new Card();

            // Outer slot keeps the layout stable while the inner card shakes.
            RectTransform slot = UIBuilder.Rect("Slot_" + (index + 1), row);
            UIBuilder.Size(slot, 300f, 440f);

            card.Background = UIBuilder.Panel(slot, "Card", Color.white);
            bool isNew = UIBuilder.IsNew(card.Background);
            UIBuilder.Stretch(card.Background.rectTransform);
            card.Background.raycastTarget = true;
            card.Group = UIBuilder.GetOrAdd<CanvasGroup>(card.Background.gameObject);

            card.Button = UIBuilder.GetOrAdd<Button>(card.Background.gameObject);
            if (isNew)
            {
                card.Button.targetGraphic = card.Background;
                ColorBlock colours = card.Button.colors;
                colours.normalColor = UITheme.PanelColour;
                colours.highlightedColor = UITheme.ButtonHover;
                colours.selectedColor = UITheme.ButtonHover;
                colours.pressedColor = UITheme.Accent;
                card.Button.colors = colours;
            }
            int captured = index;
            card.Button.onClick.AddListener(() => OnCardClicked(captured));

            Transform content = card.Background.transform;
            card.Number = UIBuilder.Label(content, "Number", (index + 1).ToString("00"), 72, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIBuilder.Place(card.Number.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(260f, 90f));

            card.StatusRing = UIBuilder.Icon(content, "StatusRing", IconFactory.Ring, Color.white, 130f);
            UIBuilder.Place(card.StatusRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(130f, 130f));
            card.StatusIcon = UIBuilder.Icon(card.StatusRing.transform, "StatusIcon", IconFactory.Lock, Color.white, 70f);
            UIBuilder.Place(card.StatusIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));

            card.Name = UIBuilder.Label(content, "Name", "", 26, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIBuilder.Place(card.Name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(270f, 40f));

            RectTransform stars = UIBuilder.Place(UIBuilder.Rect("Stars", content), new Vector2(0.5f, 0f), new Vector2(0f, 66f), new Vector2(200f, 44f));
            UIBuilder.Horizontal(stars, 8f, TextAnchor.MiddleCenter);
            card.Stars = new Image[3];
            for (int s = 0; s < 3; s++) card.Stars[s] = UIBuilder.Size(UIBuilder.Icon(stars, "Star_" + (s + 1), IconFactory.StarOutline, UITheme.Locked, 40f), 40f, 40f);

            card.Best = UIBuilder.Label(content, "Best", "", 20, UITheme.TextMuted, TextAnchor.MiddleCenter);
            UIBuilder.Place(card.Best.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(270f, 30f));

            return card;
        }

        private static void Refresh(Card card, int index, LevelDefinition definition, LevelStatus status, LevelProgressData data)
        {
            bool locked = status == LevelStatus.Locked;
            card.Name.text = definition != null ? definition.DisplayName : "Level " + (index + 1);
            card.Group.alpha = locked ? 0.45f : 1f;

            switch (status)
            {
                case LevelStatus.Locked:
                    card.StatusIcon.sprite = IconFactory.Lock;
                    card.StatusIcon.color = UITheme.Locked;
                    card.StatusRing.color = UITheme.Locked;
                    break;
                case LevelStatus.Available:
                    card.StatusIcon.sprite = IconFactory.Play;
                    card.StatusIcon.color = UITheme.Accent;
                    card.StatusRing.color = UITheme.Accent;
                    break;
                default:
                    card.StatusIcon.sprite = IconFactory.Check;
                    card.StatusIcon.color = UITheme.Health;
                    card.StatusRing.color = UITheme.Health;
                    break;
            }

            int stars = data != null ? data.bestStars : 0;
            for (int s = 0; s < card.Stars.Length; s++)
            {
                bool earned = s < stars;
                card.Stars[s].sprite = earned ? IconFactory.Star : IconFactory.StarOutline;
                card.Stars[s].color = earned ? UITheme.Gold : UITheme.Locked;
            }

            card.Best.text = data != null && data.completed && data.bestTimeSeconds > 0f
                ? UIBuilder.FormatTime(data.bestTimeSeconds) + "   " + data.bestScore
                : "";
        }

        private void OnCardClicked(int index)
        {
            if (!LevelManager.HasInstance) return;
            if (!LevelManager.Instance.Progression.IsUnlocked(index))
            {
                UISounds.Locked();
                cards[index].Shake = 0.35f;
                return;
            }

            UISounds.Click();
            LevelManager.Instance.TryLoadLevel(index);
        }

        private static LevelProgressData FindProgress(int index)
        {
            if (!SaveManager.HasInstance) return null;
            return SaveManager.Instance.Data.levels.Find(l => l.levelIndex == index);
        }
    }
}
