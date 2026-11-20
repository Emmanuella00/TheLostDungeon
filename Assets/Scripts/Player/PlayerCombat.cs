using TheLostDungeon.Combat;
using TheLostDungeon.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheLostDungeon.Player
{
    /// <summary>
    /// Reads the Attack (hold to fire) and Melee buttons and forwards them to two weapon slots.
    /// It only knows the IWeapon interface (Strategy pattern), so swapping the blaster for another
    /// weapon is an Inspector change, not a code change.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform aimOrigin;

        [Tooltip("Any component that implements IWeapon (e.g. BlasterWeapon).")]
        [SerializeField] private MonoBehaviour primaryWeapon;
        [Tooltip("Any component that implements IWeapon (e.g. MeleeWeapon).")]
        [SerializeField] private MonoBehaviour secondaryWeapon;

        private IWeapon primary;
        private IWeapon secondary;
        private Health health;
        private InputAction attackAction;
        private InputAction meleeAction;

        public IWeapon Primary => primary;
        public IWeapon Secondary => secondary;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();

            primary = primaryWeapon as IWeapon;
            secondary = secondaryWeapon as IWeapon;
            if (primaryWeapon != null && primary == null) Debug.LogError($"{primaryWeapon.name} doesn't implement IWeapon.", this);
            if (secondaryWeapon != null && secondary == null) Debug.LogError($"{secondaryWeapon.name} doesn't implement IWeapon.", this);

            InputActionMap map = controller.Controls.FindActionMap("Player", true);
            attackAction = map.FindAction("Attack", true);
            meleeAction = map.FindAction("Melee", false);
        }

        private void OnValidate()
        {
            if (primaryWeapon != null && !(primaryWeapon is IWeapon)) primaryWeapon = null;
            if (secondaryWeapon != null && !(secondaryWeapon is IWeapon)) secondaryWeapon = null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            primary?.Tick(dt);
            secondary?.Tick(dt);

            if (!CanAttack()) return;

            var context = new WeaponContext(aimOrigin, gameObject);

            if (meleeAction != null && meleeAction.WasPressedThisFrame())
            {
                secondary?.TryUse(context);
            }
            else if (attackAction.IsPressed())
            {
                primary?.TryUse(context);   // fires repeatedly while held; the weapon limits the rate
            }
        }

        private bool CanAttack()
        {
            if (controller != null && !controller.ControlEnabled) return false;
            if (health != null && !health.IsAlive) return false;
            if (Time.timeScale <= 0f) return false;

#if !(UNITY_ANDROID || UNITY_IOS)
            // On PC/WebGL the first click only recaptures the mouse; don't also fire.
            if (Cursor.lockState != CursorLockMode.Locked) return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
#endif
            return true;
        }
    }
}
