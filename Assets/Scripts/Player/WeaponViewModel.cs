using TheLostDungeon.Weapons;
using UnityEngine;

namespace TheLostDungeon.Player
{
    /// <summary>
    /// Makes the first-person gun feel alive, using code instead of animation files:
    ///   - walking bob (sways with movement speed)
    ///   - recoil kick on every shot (springs back)
    ///   - a forward bash motion for melee
    /// If an Animator is assigned, it also sets "Fire" / "Melee" triggers so you can replace any of
    /// this with your own clips made in the Animation window.
    /// </summary>
    public class WeaponViewModel : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private BlasterWeapon blaster;
        [SerializeField] private MeleeWeapon melee;
        [SerializeField] private PlayerController player;
        [SerializeField] private Animator animator;

        [Header("Bob")]
        [SerializeField] private float bobAmount = 0.015f;
        [SerializeField] private float bobSpeed = 9f;

        [Header("Recoil")]
        [SerializeField] private float recoilKickBack = 0.06f;
        [SerializeField] private float recoilKickUpDegrees = 4f;
        [SerializeField] private float recoilReturnSpeed = 14f;

        [Header("Melee bash")]
        [SerializeField] private float bashDistance = 0.35f;
        [SerializeField] private float bashDegrees = -35f;
        [SerializeField] private float bashDuration = 0.3f;

        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int MeleeHash = Animator.StringToHash("Melee");

        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 recoilOffset;
        private float recoilPitch;
        private float bobTime;
        private float bashTimer = -1f;

        private void Awake()
        {
            if (model == null) model = transform;
            restPosition = model.localPosition;
            restRotation = model.localRotation;
        }

        private void OnEnable()
        {
            if (blaster != null) blaster.Fired += HandleFired;
            if (melee != null) melee.Swung += HandleSwung;
        }

        private void OnDisable()
        {
            if (blaster != null) blaster.Fired -= HandleFired;
            if (melee != null) melee.Swung -= HandleSwung;
        }

        private void HandleFired()
        {
            recoilOffset += Vector3.back * recoilKickBack;
            recoilPitch -= recoilKickUpDegrees;
            if (animator != null) animator.SetTrigger(FireHash);
        }

        private void HandleSwung()
        {
            bashTimer = 0f;
            if (animator != null) animator.SetTrigger(MeleeHash);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Bob based on horizontal speed.
            float speed = 0f;
            if (player != null)
            {
                Vector3 v = player.Velocity;
                v.y = 0f;
                speed = Mathf.Clamp01(v.magnitude / 3.5f);
            }
            bobTime += dt * bobSpeed * speed;
            Vector3 bob = new Vector3(Mathf.Cos(bobTime * 0.5f) * bobAmount, Mathf.Abs(Mathf.Sin(bobTime)) * bobAmount, 0f) * speed;

            // Recoil springs back to zero.
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilReturnSpeed * dt);
            recoilPitch = Mathf.Lerp(recoilPitch, 0f, recoilReturnSpeed * dt);

            // Melee: out and back along a sine curve.
            Vector3 bashOffset = Vector3.zero;
            float bashPitch = 0f;
            if (bashTimer >= 0f)
            {
                bashTimer += dt;
                float t = Mathf.Clamp01(bashTimer / bashDuration);
                float curve = Mathf.Sin(t * Mathf.PI);
                bashOffset = Vector3.forward * bashDistance * curve;
                bashPitch = bashDegrees * curve;
                if (t >= 1f) bashTimer = -1f;
            }

            model.localPosition = restPosition + bob + recoilOffset + bashOffset;
            model.localRotation = restRotation * Quaternion.Euler(recoilPitch + bashPitch, 0f, 0f);
        }
    }
}
