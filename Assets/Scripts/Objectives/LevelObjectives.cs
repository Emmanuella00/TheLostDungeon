using TheLostDungeon.Core;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>
    /// One per level scene. Runs the level's objectives in order, reports progress through
    /// GameEvents.ObjectiveUpdated (the HUD listens), adds up the score from defeated enemies,
    /// and completes the level through LevelManager once every objective is done.
    ///
    /// Set up: add Objective components to child objects and drag them into the list in the order
    /// the player should do them. The last one is usually "Reach the exit".
    /// </summary>
    public class LevelObjectives : MonoBehaviour
    {
        [SerializeField] private Objective[] objectives = new Objective[0];
        [SerializeField] private int completionBonus = 500;

        [Tooltip("Progression reward: ability id unlocked (and saved) when this level is completed, e.g. 'shield'.")]
        [SerializeField] private string unlockAbilityOnComplete = "";

        private int index = -1;
        private int score;
        private bool finished;

        public Objective CurrentObjective => index >= 0 && index < objectives.Length ? objectives[index] : null;
        public int Score => score;
        public bool Finished => finished;

        private void OnEnable() => GameEvents.EnemyDefeated += HandleEnemyDefeated;
        private void OnDisable() => GameEvents.EnemyDefeated -= HandleEnemyDefeated;

        private void Start()
        {
            foreach (Objective objective in objectives)
            {
                if (objective != null) objective.ProgressChanged += HandleProgress;
            }
            Advance();
        }

        private void OnDestroy()
        {
            foreach (Objective objective in objectives)
            {
                if (objective != null) objective.ProgressChanged -= HandleProgress;
            }
        }

        private void HandleEnemyDefeated(EnemyDefeatedInfo info) => score += info.ScoreValue;

        private void HandleProgress(Objective objective)
        {
            if (objective != CurrentObjective || finished) return;

            Report();
            if (objective.IsComplete)
            {
                objective.End();
                Advance();
            }
        }

        /// <summary>Moves to the next unfinished objective, or finishes the level.</summary>
        private void Advance()
        {
            do
            {
                index++;
            }
            while (index < objectives.Length && (objectives[index] == null || objectives[index].IsComplete));

            if (index >= objectives.Length)
            {
                Finish();
                return;
            }

            CurrentObjective.Begin();   // triggers HandleProgress -> Report
            Report();
        }

        private void Report()
        {
            Objective o = CurrentObjective;
            if (o != null) GameEvents.RaiseObjectiveUpdated(o.Description, o.Current, o.Target);
        }

        /// <summary>Saves the unlocked ability; LevelManager's save after completion writes it to JSON.</summary>
        private void UnlockReward()
        {
            if (string.IsNullOrEmpty(unlockAbilityOnComplete) || !SaveManager.HasInstance) return;
            var unlocked = SaveManager.Instance.Data.unlockedAbilities;
            if (!unlocked.Contains(unlockAbilityOnComplete)) unlocked.Add(unlockAbilityOnComplete);
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            GameEvents.RaiseObjectiveUpdated("All objectives complete", 1, 1);
            UnlockReward();

            if (LevelManager.HasInstance) LevelManager.Instance.CompleteCurrentLevel(score + completionBonus);
            else Debug.Log($"[LevelObjectives] Level complete. Score {score + completionBonus} (no LevelManager in this scene).");
        }
    }
}
