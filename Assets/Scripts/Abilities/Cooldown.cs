using System;

namespace TheLostDungeon.Abilities
{
    /// <summary>A simple cooldown timer, pure C# so it can be unit tested.</summary>
    public class Cooldown
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;

        /// <summary>1 right after use, 0 when ready. Handy for radial HUD fills.</summary>
        public float Normalized => Duration > 0f ? Remaining / Duration : 0f;

        public Cooldown(float duration)
        {
            if (duration < 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            Duration = duration;
        }

        public void Start() => Remaining = Duration;

        /// <summary>Counts down. Returns true on the frame it becomes ready again.</summary>
        public bool Tick(float deltaTime)
        {
            if (Remaining <= 0f || deltaTime <= 0f) return false;
            Remaining = Math.Max(0f, Remaining - deltaTime);
            return Remaining <= 0f;
        }

        public void Reset() => Remaining = 0f;
    }
}
