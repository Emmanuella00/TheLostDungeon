using UnityEngine;

namespace TheLostDungeon.Weapons
{
    /// <summary>
    /// STRATEGY PATTERN for weapons. PlayerCombat holds weapons through this interface and
    /// never knows how each one works. A new weapon (shotgun, grenade launcher) is a new class
    /// implementing IWeapon. Nothing existing has to change.
    /// </summary>
    public interface IWeapon
    {
        string Id { get; }
        bool IsReady { get; }

        /// <summary>Called every frame so the weapon can cool down, count cooldowns, etc.</summary>
        void Tick(float deltaTime);

        /// <summary>Attempts to attack. Returns false if the weapon isn't ready.</summary>
        bool TryUse(in WeaponContext context);
    }

    /// <summary>Who is attacking and where they're aiming from.</summary>
    public readonly struct WeaponContext
    {
        public readonly Transform AimOrigin;    // usually the player camera
        public readonly GameObject Owner;       // the player root (never hit yourself)

        public WeaponContext(Transform aimOrigin, GameObject owner)
        {
            AimOrigin = aimOrigin;
            Owner = owner;
        }
    }
}
