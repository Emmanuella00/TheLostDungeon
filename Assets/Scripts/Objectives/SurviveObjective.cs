using TheLostDungeon.Core;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>"Survive the lockdown for 60 seconds". Counts time spent playing (not paused).</summary>
    public class SurviveObjective : Objective
    {
        [SerializeField, Min(1)] private int seconds = 60;

        private float elapsed;
        private int lastReported = -1;

        public override int Current => Mathf.Min(Mathf.FloorToInt(elapsed), seconds);
        public override int Target => seconds;

        private void Update()
        {
            if (!IsActive || IsComplete) return;
            if (GameManager.HasInstance && !GameManager.Instance.IsPlaying) return;

            elapsed += Time.deltaTime;
            if (Current != lastReported)
            {
                lastReported = Current;
                NotifyProgress();
            }
        }
    }
}
