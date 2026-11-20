using TheLostDungeon.Combat;
using TheLostDungeon.Core;
using TheLostDungeon.Data;
using TheLostDungeon.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheLostDungeon.Player
{
    /// <summary>
    /// First-person movement and look, driven entirely by the new Input System.
    /// Works with keyboard + mouse, gamepad, and the on-screen sticks used on mobile.
    ///
    /// Reacts to the game state through GameEvents: control is only enabled while Playing, so
    /// pausing, game over and level complete automatically free the cursor. Also handles the
    /// Pause button and stops all control when the player's Health dies.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset controls;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.1f;   // degrees per mouse pixel
        [SerializeField] private float stickSensitivity = 120f;   // degrees per second at full tilt
        [SerializeField] private float maxPitch = 80f;

        [Header("Optional")]
        [Tooltip("If set, control stops when this Health dies.")]
        [SerializeField] private Health health;

        private CharacterController characterController;
        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction lookStickAction;
        private InputAction pauseAction;

        private float pitch;
        private float verticalVelocity;
        private bool controlEnabled = true;

        public bool ControlEnabled => controlEnabled;
        public InputActionAsset Controls => controls;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            playerMap = controls.FindActionMap("Player", true);
            moveAction = playerMap.FindAction("Move", true);
            lookAction = playerMap.FindAction("Look", true);
            lookStickAction = playerMap.FindAction("LookStick", true);
            pauseAction = playerMap.FindAction("Pause", false);

            if (health == null) health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            playerMap.Enable();
            GameEvents.GameStateChanged += HandleGameStateChanged;
            if (health != null) health.Died += HandleDied;
        }

        private void OnDisable()
        {
            playerMap.Disable();
            GameEvents.GameStateChanged -= HandleGameStateChanged;
            if (health != null) health.Died -= HandleDied;
        }

        private void Start()
        {
            // If there's no GameManager (e.g. a test scene) just play; otherwise follow its state.
            bool playing = !GameManager.HasInstance || GameManager.Instance.IsPlaying;
            SetControlEnabled(playing);
        }

        private void Update()
        {
            HandlePauseInput();
            RelockCursorOnClick();

            if (controlEnabled)
            {
                Look();
            }

            Move();
        }

        /// <summary>Called by UI and game state changes to pause or resume player control.</summary>
        public void SetControlEnabled(bool value)
        {
            if (value && health != null && !health.IsAlive) value = false;

            controlEnabled = value;
#if UNITY_ANDROID || UNITY_IOS
            // Touch devices have no cursor to lock.
            Cursor.lockState = CursorLockMode.None;
#else
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
#endif
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            SetControlEnabled(current == GameState.Playing);
        }

        private void HandleDied() => SetControlEnabled(false);

        private void HandlePauseInput()
        {
            if (pauseAction != null && pauseAction.WasPressedThisFrame() && GameManager.HasInstance)
            {
                GameManager.Instance.TogglePause();
            }
        }

        private void Look()
        {
            float sensitivityScale = GameSettings.LookSensitivity;
            Vector2 look = lookStickAction.ReadValue<Vector2>() * stickSensitivity * sensitivityScale * Time.deltaTime;

            // Mouse delta is only used while the cursor is captured.
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                look += lookAction.ReadValue<Vector2>() * mouseSensitivity * sensitivityScale;
            }

            transform.Rotate(Vector3.up * look.x);
            pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Move()
        {
            Vector2 input = controlEnabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        // Browsers (WebGL) and the editor release the cursor on Esc; a click recaptures it.
        private void RelockCursorOnClick()
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (controlEnabled
                && !overUI
                && Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame)
            {
#if !(UNITY_ANDROID || UNITY_IOS)
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
#endif
            }
        }
    }
}
