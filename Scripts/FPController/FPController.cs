using System;
using UnityEngine;

namespace FPController
{
    [RequireComponent(typeof(FPInputManager))]
    public class FPController : MonoBehaviour
    {
        #region Inspector Serialized Fields
        [Header("Movement")] // Movement Fields

        [Tooltip("Player's walking speed value")]
        [SerializeField] private float _movementSpeed = 4f;

        [Tooltip("Player's movement acceleration")]
        [SerializeField] private float _acceleration = 15f;

        [Tooltip("Percentage of player's mid-air movement control [0,1]")]
        [SerializeField][Range(0, 1)] private float _midAirControlMultiplier = 0.4f;

        [Header("Slopes")]

        [Tooltip("This field is used to calculate how fast the player is going to slide on a slope [Infinity,0]")]
        [SerializeField][Range(float.NegativeInfinity, 0)] private float _slopeSlideMultiplier = -4f;

        [Header("Gravity")] // Gravity Fields

        [Tooltip("Player's mass")]
        [SerializeField] private float _mass = 1f;

        [Tooltip("Gravity multiplier")]
        [SerializeField] private float _gravityMultiplier = 1f;

        [Header("Camera")] // Camera fields

        [Tooltip("Reference to the camera holder game object")]
        [SerializeField] private Transform _cameraHolderTransform;

        [Tooltip("Mouse sensitivity")]
        [SerializeField] private float _mouseSensitivity = 0.05f;

        [Tooltip("Will clamp the rotation of Y axis on this value")]
        [SerializeField] private float _maxCameraY = 90f;
        #endregion

        #region Events
        public event Action OnBeforeMove;
        public event Action<bool> OnGroundStateChange;
        #endregion

        #region Public Properties
        public bool IsGrounded => _controller.isGrounded;
        public bool IsOnSlope => _isOnSlope;
        public float MovementSpeedMultiplier
        {
            get { return _movementSpeedMultiplier; }
            set { _movementSpeedMultiplier = value; }
        }
        public bool IsJumping
        {
            get { return _isJumping; }
            set { _isJumping = value; }
        }
        public bool IsSprinting => _controller.isGrounded && _input.SprintInput;
        public bool IsSliding
        {
            get { return _isSliding; }
            set { _isSliding = value; }
        }
        public bool IsInputMoving => _controller.velocity.magnitude > 0.1f && _input.MovementInput != Vector2.zero;
        public float PlayerMass => _mass;
        public float GravityMultiplier => _gravityMultiplier;
        #endregion

        #region Public Fields
        internal Vector3 Velocity;
        #endregion

        #region Local Fields
        private FPInputManager _input;
        private CharacterController _controller;
        private Vector3 _moveDirection;
        private Vector3 _lastMoveDirection;
        private Vector2 _lookDirection;
        private float _movementSpeedMultiplier;
        private bool _wasGrounded;
        private bool _isJumping;
        private bool _isOnSlope;
        private bool _isSliding;
        private bool _didSphereCast;
        #endregion

        #region MonoBehaviour Methods
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<FPInputManager>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            UpdateGround();
            UpdateGravity();
            UpdateMovement();
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }
        #endregion

        #region Private Methods
        private void UpdateCamera()
        {
            // Get the mouse delta input
            _lookDirection += new Vector2(_input.LookInput.x * _mouseSensitivity, _input.LookInput.y * _mouseSensitivity);

            // Clamps the rotation of the Y-AXIS (vertical)
            _lookDirection.y = Mathf.Clamp(_lookDirection.y, -_maxCameraY, _maxCameraY);

            // Applies the rotations to the camera for vertical, and to the player for horizontal
            _cameraHolderTransform.localRotation = Quaternion.Euler(-_lookDirection.y, 0, 0);
            transform.localRotation = Quaternion.Euler(0, _lookDirection.x, 0);
        }

        private void UpdateMovement()
        {
            _movementSpeedMultiplier = 1f;
            // Invoke methods before the actual movement calculations if there are any
            OnBeforeMove?.Invoke();

            // Calculate & normalize the move direction from the input
            if (_controller.isGrounded)
            {
                _moveDirection = transform.forward * _input.MovementInput.y + transform.right * _input.MovementInput.x;
                _moveDirection.Normalize();
                _moveDirection *= _movementSpeed * _movementSpeedMultiplier;

                if (!_isJumping)
                    _lastMoveDirection = _moveDirection;
            }
            // Else if player is not grounded, then use the lastMoveDirection and by getting again a mid air direction,
            // player will be able to move slightly while on air, but that depends on the _midAirControlMultiplier
            else
            {
                // Get the movement direction, normalize it and apply the speed
                Vector3 midAirDirection = transform.forward * _input.MovementInput.y + transform.right * _input.MovementInput.x;
                midAirDirection.Normalize();
                midAirDirection *= _movementSpeed * _movementSpeedMultiplier;
                // Interpolate the last saved move direction with the mid air control multiplier
                // 0: no mid air control , 1: full mid air control
                _moveDirection.x = Mathf.Lerp(_lastMoveDirection.x, midAirDirection.x, _midAirControlMultiplier);
                _moveDirection.z = Mathf.Lerp(_lastMoveDirection.z, midAirDirection.z, _midAirControlMultiplier);

            }

            // Smooth the movement speed
            var smoothMovementFactor = _acceleration * Time.deltaTime;
            Velocity.x = Mathf.Lerp(Velocity.x, _moveDirection.x, smoothMovementFactor);
            Velocity.z = Mathf.Lerp(Velocity.z, _moveDirection.z, smoothMovementFactor);

            //Apply the move direction
            _controller.Move(Velocity * Time.deltaTime);
        }

        private void UpdateGravity()
        {
            var gravity = (Physics.gravity * _gravityMultiplier * _mass) * Time.deltaTime;
            if (_isOnSlope)
                Velocity.y = _slopeSlideMultiplier + gravity.y;
            else Velocity.y = _controller.isGrounded ? -1f : Velocity.y + gravity.y;
        }

        private void UpdateGround()
        {
            // Check if player is on slope and slide if so
            SlopeCheck();

            // _wasGrounded will update only when IsGrounded changes value, to capture the time that the player got last grounded
            if (_wasGrounded != IsGrounded)
            {
                OnGroundStateChange?.Invoke(IsGrounded);
                _wasGrounded = IsGrounded;
            }
        }

        // This method will either use a Raycast or a SphereCast,
        // The Raycast will check the ground beneath the player but if player is near an edge and can't get a hit then uses
        // The Spherecast which will manage to get a hit, but once player starts sliding from the Spherecast will not go back to Raycast until they stop.
        // The reason that Raycast goes before the Spherecast are the steps of the stairs. I couldn't figure a way to handle steps with Spherecast.
        private void SlopeCheck()
        {
            if (_controller.isGrounded)
            {
                // The cast origin will be on character's feet
                var castOrigin = transform.position - new Vector3(0, _controller.height / 2 - _controller.radius, 0);
                // If didn't slide using Spherecast and gets a hit using Raycast, then invoke TryApplySlopeForce
                if (!_didSphereCast && Physics.Raycast(transform.position, Vector3.down, out var hit, 1.5f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
                {
                    TryApplySlopeForce(hit);
                }
                // Else use a Spherecast to find a hit
                else if (Physics.SphereCast(castOrigin, _controller.radius - 0.01f, Vector3.down, out hit, 0.05f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
                {
                    TryApplySlopeForce(hit);
                    if (_isSliding)
                        _didSphereCast = true;
                }
            }
        }

        // This function will find the angle between the hit and the normal, and if it's a steep slope will start sliding
        private void TryApplySlopeForce(RaycastHit hit)
        {
            var angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > _controller.slopeLimit)
            {
                Velocity.x += (1f - hit.normal.y) * hit.normal.x * (1f - _slopeSlideMultiplier);
                Velocity.z += (1f - hit.normal.y) * hit.normal.z * (1f - _slopeSlideMultiplier);
                _isOnSlope = true;
                _isSliding = true;
            }
            else
            {
                _isOnSlope = false;
                _isSliding = false;
                _didSphereCast = false;
            }
        }
        #endregion
    }
}
