using TheLostDungeon.Combat;
using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Everything that makes one enemy type different, as data. Warrior, Archer and Guardian share
    /// the same EnemyBrain; they differ only by their profile and their attack strategy.
    /// A new enemy type = a new profile asset (+ an attack strategy if it attacks differently).
    /// </summary>
    [CreateAssetMenu(menuName = "The Lost Dungeon/Enemy Profile", fileName = "EP_")]
    public class EnemyProfile : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "enemy";
        public string displayName = "Enemy";
        public int scoreValue = 100;

        [Header("Health & defence")]
        [Min(1f)] public float maxHealth = 60f;
        public DefenceProfile defence;
        [Tooltip("Seconds stunned when a melee hit breaks its shield guard.")]
        [Min(0f)] public float staggerSeconds = 1.5f;

        [Header("Movement")]
        [Min(0.1f)] public float walkSpeed = 1.6f;
        [Min(0.1f)] public float runSpeed = 4f;
        [Tooltip("Wanders this far from its post when it has no patrol waypoints.")]
        [Min(0f)] public float wanderRadius = 5f;
        [Tooltip("Gives up the chase beyond this distance from its post (0 = never). Guardians stay close.")]
        [Min(0f)] public float leashRadius = 0f;

        [Header("Perception")]
        [Min(1f)] public float sightRange = 16f;
        [Range(10f, 360f)] public float sightAngle = 110f;
        [Min(0f)] public float hearingRange = 25f;
        [Tooltip("Height of the eyes / aim point above the enemy's pivot.")]
        [Min(0f)] public float eyeHeight = 1f;
        [Min(0.5f)] public float investigateSeconds = 4f;

        [Header("Combat positioning")]
        [Tooltip("Backs away if the player is closer than this (Archers). 0 = never backs away.")]
        [Min(0f)] public float preferredMinDistance = 0f;
        [Tooltip("Attacks when the player is within this distance.")]
        [Min(0.5f)] public float attackRange = 1.8f;
        [Min(0.1f)] public float attackCooldown = 1.2f;
    }
}
