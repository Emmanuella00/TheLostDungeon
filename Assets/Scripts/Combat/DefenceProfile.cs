using System;
using UnityEngine;

namespace TheLostDungeon.Combat
{
    /// <summary>
    /// How well something resists damage. Set in the Inspector on each Health component, so a
    /// Guardian's shield is just data: no special-case code in the damage system.
    /// </summary>
    [Serializable]
    public struct DefenceProfile
    {
        [Tooltip("0 = no armour, 0.5 = halves all damage. Capped at 0.9.")]
        [Range(0f, 0.9f)] public float armour;

        [Tooltip("A front-facing shield that blocks ranged hits (Guardian bots).")]
        public bool hasFrontShield;

        [Tooltip("Share of a ranged hit the shield absorbs (1 = blocks it completely).")]
        [Range(0f, 1f)] public float shieldBlockFraction;

        [Tooltip("Total width of the shield's protected cone, in degrees.")]
        [Range(0f, 360f)] public float shieldArcDegrees;

        public static DefenceProfile None => new DefenceProfile();

        public static DefenceProfile Shielded(float blockFraction = 0.9f, float arcDegrees = 120f, float armour = 0f) =>
            new DefenceProfile
            {
                armour = armour,
                hasFrontShield = true,
                shieldBlockFraction = blockFraction,
                shieldArcDegrees = arcDegrees
            };
    }
}
