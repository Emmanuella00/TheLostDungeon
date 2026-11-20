using System;

namespace TheLostDungeon.Combat
{
    /// <summary>
    /// The health numbers and rules, separated from Unity so they can be unit tested.
    /// The Health MonoBehaviour wraps one of these.
    /// </summary>
    public class HealthModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;
        public float Normalized => Max > 0f ? Current / Max : 0f;

        /// <summary>(current, max) after any change.</summary>
        public event Action<float, float> Changed;
        /// <summary>Fires exactly once, when health first reaches zero.</summary>
        public event Action Died;

        public HealthModel(float max)
        {
            if (max <= 0f) throw new ArgumentOutOfRangeException(nameof(max));
            Max = max;
            Current = max;
        }

        /// <summary>Removes health. Returns the amount actually removed.</summary>
        public float Damage(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;

            float before = Current;
            Current = Math.Max(0f, Current - amount);
            Changed?.Invoke(Current, Max);

            if (Current <= 0f) Died?.Invoke();
            return before - Current;
        }

        /// <summary>Restores health, never above Max and never for a dead target.</summary>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;

            float before = Current;
            Current = Math.Min(Max, Current + amount);
            if (Current != before) Changed?.Invoke(Current, Max);
            return Current - before;
        }

        /// <summary>Full reset, e.g. respawning a training dummy.</summary>
        public void Revive()
        {
            Current = Max;
            Changed?.Invoke(Current, Max);
        }

        public void SetMax(float newMax, bool fillToMax)
        {
            if (newMax <= 0f) throw new ArgumentOutOfRangeException(nameof(newMax));
            Max = newMax;
            Current = fillToMax ? Max : Math.Min(Current, Max);
            Changed?.Invoke(Current, Max);
        }
    }
}
