using System.Collections.Generic;
using TheLostDungeon.Algorithms;
using TheLostDungeon.Combat;
using TheLostDungeon.Events;
using TheLostDungeon.Player;
using TheLostDungeon.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// The ONE enemy AI shared by every enemy type. It owns:
    ///   - perception: sight (view cone + line-of-sight raycast, so walls and closed doors block it)
    ///     and hearing (listens to GameEvents.NoiseMade from gunshots and explosions)
    ///   - a StateMachine running Patrol / Investigate / Chase / Attack / Stagger / Dead
    ///   - movement through a NavMeshAgent, and four Animator parameters (Speed, Attack, Hit, Dead)
    ///
    /// What makes a Warrior, Archer or Guardian different is NOT code in here: it's the
    /// EnemyProfile (data) and the attack strategy component on the prefab.
    /// Works when placed by hand in a scene or spawned from a pool by an EnemySpawner.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class EnemyBrain : MonoBehaviour, ITargetable, IPoolable
    {
        [SerializeField] private EnemyProfile profile;
        [SerializeField] private EnemyAttack attack;
        [SerializeField] private Animator animator;

        [Tooltip("Optional patrol route. Without waypoints the enemy wanders around where it started.")]
        [SerializeField] private Transform[] patrolWaypoints = new Transform[0];

        [SerializeField] private LayerMask sightMask = ~0;
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioClip alertSound;
        [SerializeField] private AudioClip deathSound;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private readonly RaycastHit[] sightHits = new RaycastHit[8];
        private readonly HashSet<int> animatorParams = new HashSet<int>();

        private NavMeshAgent agent;
        private Health health;
        private Collider[] colliders;
        private Transform target;
        private float lastHitAnimTime;

        public StateMachine Machine { get; } = new StateMachine();
        public PatrolState Patrol { get; private set; }
        public InvestigateState Investigate { get; private set; }
        public ChaseState Chase { get; private set; }
        public AttackState Attack { get; private set; }
        public StaggerState Stagger { get; private set; }
        public DeadState Dead { get; private set; }

        public EnemyProfile Profile => profile;
        public IEnemyAttack AttackStrategy => attack;
        public NavMeshAgent Agent => agent;
        public Transform Target => target;
        public Vector3 Post { get; private set; }
        public IReadOnlyList<Transform> PatrolWaypoints => patrolWaypoints;
        public bool IsAlive => health != null && health.IsAlive;
        public string CurrentStateName => Machine.Current?.Name ?? "-";

        public Vector3 EyePosition => transform.position + Vector3.up * profile.eyeHeight;
        public Vector3? TargetAimPoint => target != null ? target.position + Vector3.up * 1.4f : (Vector3?)null;

        // ITargetable (player aim assist)
        public Vector3 AimPoint => EyePosition;
        public bool IsTargetable => IsAlive;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            colliders = GetComponentsInChildren<Collider>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (attack == null) attack = GetComponent<EnemyAttack>();
            if (animator != null)
            {
                foreach (AnimatorControllerParameter p in animator.parameters) animatorParams.Add(p.nameHash);
            }

            health.Defence = profile.defence;
            health.SetMaxHealth(profile.maxHealth, fillToMax: true);

            Patrol = new PatrolState(this);
            Investigate = new InvestigateState(this);
            Chase = new ChaseState(this);
            Attack = new AttackState(this);
            Stagger = new StaggerState(this);
            Dead = new DeadState(this);
        }

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
            GameEvents.NoiseMade += HandleNoise;
            TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
            GameEvents.NoiseMade -= HandleNoise;
            TargetRegistry.Unregister(this);
        }

        private void Start() => ResetBrain();

        // Called by the pool when an EnemySpawner reuses this enemy.
        public void OnSpawn()
        {
            if (agent != null && agent.isOnNavMesh) agent.Warp(transform.position);
            health.Revive();
            ResetBrain();
        }

        public void OnDespawn() { }

        private void ResetBrain()
        {
            Post = transform.position;
            FindTarget();
            foreach (Collider c in colliders) c.enabled = true;
            agent.enabled = true;
            SetAnimBool(DeadHash, false);
            Machine.ChangeState(Patrol);
        }

        private void Update()
        {
            if (target == null) FindTarget();
            Machine.Tick(Time.deltaTime);
            SetAnimFloat(SpeedHash, agent.enabled ? agent.velocity.magnitude : 0f);
        }

        // ------------------------------------------------------------------ perception

        /// <summary>View cone + raycast. True only if nothing solid is between the eyes and the player.</summary>
        public bool CanSeeTarget()
        {
            if (target == null || !IsAlive) return false;

            Vector3 eye = EyePosition;
            Vector3 aim = TargetAimPoint.Value;
            if (!EnemyMath.IsInViewCone(eye, transform.forward, aim, profile.sightRange, profile.sightAngle)) return false;

            Vector3 toTarget = aim - eye;
            int count = Physics.RaycastNonAlloc(eye, toTarget.normalized, sightHits, toTarget.magnitude, sightMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            bool targetIsNearest = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = sightHits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;   // own body
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    targetIsNearest = hit.collider.transform.IsChildOf(target);
                }
            }

            return count == 0 || targetIsNearest;   // nothing in between, or the first thing hit is the player
        }

        /// <summary>Puts the enemy on alert toward a position (used by spawners and teammates).</summary>
        public void AlertTo(Vector3 position)
        {
            if (!IsAlive) return;
            if (CanSeeTarget()) Machine.ChangeState(Chase);
            else Investigate.InvestigateAt(position);
        }

        private void HandleNoise(Vector3 position, float radius)
        {
            if (!IsAlive || Machine.Current == Chase || Machine.Current == Attack || Machine.Current == Stagger) return;
            if (EnemyMath.CanHear(transform.position, position, radius, profile.hearingRange)) Investigate.InvestigateAt(position);
        }

        private void HandleDamaged(DamageInfo info, DamageResult result)
        {
            if (!IsAlive) return;

            if (Time.time - lastHitAnimTime > 0.4f)
            {
                lastHitAnimTime = Time.time;
                SetAnimTrigger(HitHash);
            }

            if (result.GuardBroken && profile.staggerSeconds > 0f)
            {
                Machine.ChangeState(Stagger);
                return;
            }

            // Shot by someone it hasn't noticed? Turn and hunt them.
            if (Machine.Current == Patrol || Machine.Current == Investigate)
            {
                Vector3 from = info.Source != null ? info.Source.transform.position : info.Point - info.Direction * 5f;
                AlertTo(from);
            }
        }

        private void HandleDied()
        {
            GameEvents.RaiseEnemyDefeated(new EnemyDefeatedInfo(profile.enemyId, transform.position, profile.scoreValue));
            Machine.ChangeState(Dead);
        }

        private void FindTarget()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        // ------------------------------------------------------------------ helpers used by states

        public void MoveTo(Vector3 destination, float speed)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(destination);
        }

        public void StopMoving()
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        public bool ReachedDestination =>
            agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;

        public float DistanceToTarget => target != null ? Vector3.Distance(transform.position, target.position) : float.PositiveInfinity;

        public void FaceTarget(float deltaTime, float degreesPerSecond = 360f)
        {
            if (target == null) return;
            Vector3 flat = target.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), degreesPerSecond * deltaTime);
        }

        /// <summary>Small direct movement (e.g. a shield-bash lunge) that still respects the NavMesh.</summary>
        public void Nudge(Vector3 offset)
        {
            if (agent.enabled && agent.isOnNavMesh) agent.Move(offset);
        }

        public bool TryFindRandomPointNear(Vector3 centre, float radius, out Vector3 point)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                if (NavMesh.SamplePosition(centre + new Vector3(r.x, 0f, r.y), out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    point = hit.position;
                    return true;
                }
            }
            point = centre;
            return false;
        }

        public void TriggerAttackAnimation() => SetAnimTrigger(AttackHash);

        public void PlayAlert()
        {
            if (alertSound != null) PlaySound(alertSound);
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            if (voice != null) voice.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        /// <summary>Death presentation: stop, switch off collisions, play the death animation.</summary>
        public void BeginDeath()
        {
            StopMoving();
            agent.enabled = false;
            foreach (Collider c in colliders) c.enabled = false;
            SetAnimBool(DeadHash, true);
            PlaySound(deathSound);
            TargetRegistry.Unregister(this);
        }

        /// <summary>Back to the pool if spawned from one; otherwise just switch off.</summary>
        public void Remove()
        {
            if (TryGetComponent(out PooledObject pooled) && pooled.Owner != null) pooled.ReturnToPool();
            else gameObject.SetActive(false);
        }

        private void SetAnimFloat(int id, float value)
        {
            if (animator != null && animatorParams.Contains(id)) animator.SetFloat(id, value);
        }

        private void SetAnimTrigger(int id)
        {
            if (animator != null && animatorParams.Contains(id)) animator.SetTrigger(id);
        }

        private void SetAnimBool(int id, bool value)
        {
            if (animator != null && animatorParams.Contains(id)) animator.SetBool(id, value);
        }

        private void OnDrawGizmosSelected()
        {
            if (profile == null) return;
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, profile.sightRange);
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, profile.attackRange);
        }
    }
}
