using System;

namespace TheLostDungeon.Weapons
{
    /// <summary>
    /// Overheat logic for the blaster, as pure C# so it can be unit tested.
    /// Each shot adds heat. Heat cools over time. Reaching 100% overheats the weapon: it's
    /// locked for a few seconds while the gauge drains, then it's ready again.
    /// </summary>
    public class HeatGauge
    {
        private readonly float coolPerSecond;
        private readonly float overheatLockSeconds;
        private float lockRemaining;

        public float Heat { get; private set; }          // 0..1
        public bool IsOverheated { get; private set; }
        public bool CanFire => !IsOverheated;

        public HeatGauge(float coolPerSecond, float overheatLockSeconds)
        {
            if (coolPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(coolPerSecond));
            if (overheatLockSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(overheatLockSeconds));
            this.coolPerSecond = coolPerSecond;
            this.overheatLockSeconds = overheatLockSeconds;
        }

        /// <summary>Adds heat for one shot. Returns true if this shot caused an overheat.</summary>
        public bool AddHeat(float amount)
        {
            if (IsOverheated || amount <= 0f) return false;

            Heat = Math.Min(1f, Heat + amount);
            if (Heat >= 1f)
            {
                IsOverheated = true;
                lockRemaining = overheatLockSeconds;
                return true;
            }
            return false;
        }

        public void Cool(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            if (IsOverheated)
            {
                lockRemaining -= deltaTime;
                Heat = Math.Max(0f, lockRemaining / overheatLockSeconds);   // drains visually during the lock
                if (lockRemaining <= 0f)
                {
                    IsOverheated = false;
                    Heat = 0f;
                }
                return;
            }

            Heat = Math.Max(0f, Heat - coolPerSecond * deltaTime);
        }
    }
}
