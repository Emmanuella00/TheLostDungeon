using System;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>
    /// Base class for one level goal ("Destroy 5 bots", "Reach the exit"). Each kind of goal is a
    /// small subclass that only knows how to measure its own progress; LevelObjectives runs them in
    /// order. A new kind of objective = one new subclass, nothing else changes.
    /// </summary>
    public abstract class Objective : MonoBehaviour
    {
        [SerializeField, TextArea] private string description = "Objective";

        public string Description => description;
        public abstract int Current { get; }
        public abstract int Target { get; }
        public bool IsComplete => Current >= Target;
        public bool IsActive { get; private set; }

        /// <summary>Raised whenever progress changes, so LevelObjectives can update the HUD.</summary>
        public event Action<Objective> ProgressChanged;

        public void Begin()
        {
            if (IsActive) return;
            IsActive = true;
            OnBegin();
            NotifyProgress();
        }

        public void End()
        {
            if (!IsActive) return;
            IsActive = false;
            OnEnd();
        }

        protected virtual void OnBegin() { }
        protected virtual void OnEnd() { }
        protected virtual void OnDisable() => End();

        protected void NotifyProgress() => ProgressChanged?.Invoke(this);

#if UNITY_EDITOR
        public void EditorSetDescription(string text) => description = text;
#endif
    }
}
