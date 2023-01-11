using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPController
{
    public class FPInputManager : MonoBehaviour
    {
        public Vector2 MovementInput;
        public Vector2 LookInput;
        public bool SprintInput;
        public bool JumpInput;
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

        // Deregistering events
        private void OnDisable()
        {
            _input.PlayerControls.Movement.performed -= OnMovement;
            _input.PlayerControls.Movement.canceled -= OnMovement;
            _input.PlayerControls.Look.performed -= OnLook;
            _input.PlayerControls.Look.canceled -= OnLook;
            _input.PlayerControls.Sprint.performed -= OnSprint;
            _input.PlayerControls.Sprint.canceled -= OnSprint;
            _input.PlayerControls.Jump.performed += OnJump;
            _input.PlayerControls.Jump.canceled += OnJump;
            _input.Disable();
        }

        private void OnMovement(InputAction.CallbackContext obj) => MovementInput = obj.ReadValue<Vector2>();
        private void OnLook(InputAction.CallbackContext obj) => LookInput = obj.ReadValue<Vector2>();
        private void OnSprint(InputAction.CallbackContext obj) => SprintInput = obj.ReadValueAsButton();
        private void OnJump(InputAction.CallbackContext obj)
        {
            JumpInput = obj.ReadValueAsButton();
            if (JumpInput)
                OnJumpPressed?.Invoke();
        }
    }
}
