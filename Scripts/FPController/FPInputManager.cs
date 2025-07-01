using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPController
{
    public class FPInputManager : MonoBehaviour
    {
        public Vector2 MovementInput { get; private set; }       // Movement input 2D Vector (X-axis: AD keys) (Y-axis: WS keys)
        public Vector2 LookInput { get; private set; }           // Mouse delta input
        public bool SprintInput { get; private set; }            // Sprint input
        public bool JumpInput { get; private set; }              // Jump Input

        public event Action OnJumpPressed;
        private PlayerInputControls _input;

        private void Awake() => _input = new PlayerInputControls();

        // Registering events
        private void OnEnable()
        {
            _input.PlayerControls.Movement.performed += OnMovement;
            _input.PlayerControls.Movement.canceled += OnMovement;
            _input.PlayerControls.Look.performed += OnLook;
            _input.PlayerControls.Look.canceled += OnLook;
            _input.PlayerControls.Sprint.performed += OnSprint;
            _input.PlayerControls.Sprint.canceled += OnSprint;
            _input.PlayerControls.Jump.performed += OnJump;
            _input.PlayerControls.Jump.canceled += OnJump;
            _input.Enable();
        }

        // De-Registering events
        private void OnDisable()
        {
            _input.PlayerControls.Movement.performed -= OnMovement;
            _input.PlayerControls.Movement.canceled -= OnMovement;
            _input.PlayerControls.Look.performed -= OnLook;
            _input.PlayerControls.Look.canceled -= OnLook;
            _input.PlayerControls.Sprint.performed -= OnSprint;
            _input.PlayerControls.Sprint.canceled -= OnSprint;
            _input.PlayerControls.Jump.performed -= OnJump;
            _input.PlayerControls.Jump.canceled -= OnJump;
            _input.Disable();
        }

        /// <summary>
        /// Captures and assigns player movement input as a 2D vector (Vector2) (WASD keys)
        /// </summary>
        private void OnMovement(InputAction.CallbackContext obj) => MovementInput = obj.ReadValue<Vector2>();

        /// <summary>
        /// Captures and assigns look direction input as a 2D vector (Vector2) (Mouse)
        /// </summary>
        private void OnLook(InputAction.CallbackContext obj) => LookInput = obj.ReadValue<Vector2>();

        /// <summary>
        /// Reads and assigns a bool value for the sprint input (Shift key)
        /// </summary>
        private void OnSprint(InputAction.CallbackContext obj) => SprintInput = obj.ReadValueAsButton();

        /// <summary>
        /// Reads and assigns a boolean value for the jump input and triggers OnJumpPressed event (Space key)
        /// </summary>
        private void OnJump(InputAction.CallbackContext obj)
        {
            JumpInput = obj.ReadValueAsButton();
            if (JumpInput)
                OnJumpPressed?.Invoke();
        }
    }
}
