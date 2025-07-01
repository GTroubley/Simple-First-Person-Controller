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
        
        [Tooltip("Minimum slide speed while sliding on slopes")]
        [SerializeField] private float _minSlideSpeed = 0.01f;

        [Tooltip("Maximum slide speed while sliding on slopes")]
        [SerializeField] private float _maxSlideSpeed = 4f;


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
        private RaycastHit _slopeHit;
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
            SlopeCheck();           // Slope Check and logic
            UpdateGround();         // Ground Check and logic
            UpdateGravity();        // Gravity calculations
            UpdateMovement();       // Movement calculations
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Updates player's look direction
        /// - Applies horizontal rotation to the player.
        /// - Applies vertical rotation to the camera holder and clamps it's Y-axis rotation
        /// </summary>
        private void UpdateCamera()
        {
            _lookDirection += new Vector2(_input.LookInput.x * _mouseSensitivity, _input.LookInput.y * _mouseSensitivity);  // Get the mouse delta input
            _lookDirection.y = Mathf.Clamp(_lookDirection.y, -_maxCameraY, _maxCameraY);                                    // Clamps vertical rotation to prevent excesive up/down rotation
            _cameraHolderTransform.localRotation = Quaternion.Euler(-_lookDirection.y, 0, 0);                               // Applies vertical(Y) rotation to camera holder 
            transform.localRotation = Quaternion.Euler(0, _lookDirection.x, 0);                                             // Applies horizontal(X) rotation to player
        }

        /// <summary>
        /// Updates the player's movement
        /// - First invokes OnBeforeMove for pre-movement logic (Sprinting & Jumping)
        /// </summary>
        private void UpdateMovement()
        {
            _movementSpeedMultiplier = 1f;
            OnBeforeMove?.Invoke();             // Invoke methods before the actual movement calculations if there are any


            if (_controller.isGrounded)         // When player is grounded, calculate & normalize the move direction from the input
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

            // If player is not sliding, then smooth the movement speed before applying it
            if (!_isSliding)
            {
                float smoothMovementFactor = _acceleration * Time.deltaTime;
                Velocity.x = Mathf.Lerp(Velocity.x, _moveDirection.x, smoothMovementFactor);
                Velocity.z = Mathf.Lerp(Velocity.z, _moveDirection.z, smoothMovementFactor);
            }

            //Apply the move direction
            _controller.Move(Velocity * Time.deltaTime);
        }

        /// <summary>
        /// Applies vertical gravity force to the player based on current state:
        /// - Player is on slope (also applies slope force along with gravity)
        /// - Player is just grounded
        /// - player is mid-air
        /// </summary>
        private void UpdateGravity()
        {
            float gravityForce = Physics.gravity.y * _gravityMultiplier * _mass;
            if (_isOnSlope && IsGrounded)
            {
                Vector3 slopeForce = Vector3.Cross(Vector3.Cross(Vector3.up, _slopeHit.normal), _slopeHit.normal);
                Velocity += slopeForce * Mathf.Abs(_slopeSlideMultiplier);
                Velocity.y += gravityForce * Time.deltaTime;
                if (_isSliding)
                    ClampSlopeSlidingSpeed();
            }
            else if (IsGrounded)
                Velocity.y = -1f;
            else
                Velocity.y += gravityForce * Time.deltaTime;
        }

        /// <summary>
        /// Clamps horizontal velocity when player is sliding on a slope
        /// </summary>
        private void ClampSlopeSlidingSpeed()
        {
            Vector3 horizontalVelocity = new Vector3(Velocity.x, 0, Velocity.z);
            float speed = horizontalVelocity.magnitude;
            if (speed > _minSlideSpeed)
            {
                float clampedSpeed = Mathf.Clamp(speed, _minSlideSpeed, _maxSlideSpeed);
                horizontalVelocity = horizontalVelocity.normalized * clampedSpeed;
                Velocity.x = horizontalVelocity.x;
                Velocity.z = horizontalVelocity.z;
            }
        }

        /// <summary>
        /// Responsible for checking if player's grounded state has changed since last frame.
        /// If it has, then invokes OnGroundStateChange event and updates _wasGrounded to reflect the current grounded state.
        /// </summary>
        private void UpdateGround()
        {
            if (_wasGrounded != IsGrounded)
            {
                OnGroundStateChange?.Invoke(IsGrounded);
                _wasGrounded = IsGrounded;
            }
        }

        /// <summary>
        /// Checks if player is standing on a slope.
        /// First uses a Raycast directly downward to detect ground. But if player is near an edge and Raycast fails, then uses a SphereCast.
        /// SphereCast manages to detect that edge (stairs commonly). If player starts sliding and SphereCast was used that frame, it will not go back to Raycast until they stop
        /// 
        /// !!! Need to check this later !!!
        /// There might be an issue with stairs
        /// </summary>
        private void SlopeCheck()
        {
            if (_controller.isGrounded)
            {
                // The cast origin will be on character's feet
                var castOrigin = transform.position - new Vector3(0, _controller.height / 2 - _controller.radius, 0);
                // If didn't slide using SphereCast and Raycast finds a hit then, invoke TryApplySlopeForce
                if (!_didSphereCast && Physics.Raycast(castOrigin, Vector3.down, out var hit, 1.5f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
                    SlopeHitPointCheck(hit);
                // Else use a Spherecast to find a hit
                else if (Physics.SphereCast(castOrigin, _controller.radius - 0.01f, Vector3.down, out hit, 0.05f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
                {
                    _didSphereCast = true;
                    SlopeHitPointCheck(hit);
                }
                else if (IsGrounded && IsSliding) {
                    _didSphereCast = false;
                    SlopeHitPointCheck(_slopeHit);
                }
            }
        }

        /// <summary>
        /// Responsible for checking the angle of the hit point and adjust logic
        /// </summary>
        private void SlopeHitPointCheck(RaycastHit hit)
        {
            _slopeHit = hit;
            var angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > _controller.slopeLimit)     // If angle exceeds the limit (hit is a steep slope)
            {
                _isOnSlope = true;
                _isSliding = true;
            }
            else
            {
                _isOnSlope = false;         // is no longer on slope
                _isSliding = false;         // is not sliding
                _didSphereCast = false;     // do not use SphereCast on next frame
            }
        }
        #endregion
    }
}
