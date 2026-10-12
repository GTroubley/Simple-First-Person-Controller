using System;
using UnityEngine;

namespace FPController
{
    [RequireComponent(typeof(FPInputManager))]
    public class FPController : MonoBehaviour
    {
        #region Inspector Serialized Fields
        [Header("Movement")] // Movement Fields

        [Tooltip("Walking speed in meters per second")]
        [SerializeField][Range(0, 20)] private float _movementSpeed = 4f;

        [Tooltip("How quickly the player reaches walking speed and stops. Higher = snappier, lower = more slippery")]
        [SerializeField][Range(1, 50)] private float _acceleration = 15f;

        [Tooltip("How much the player can steer while in the air. 0 = no control, 1 = full control")]
        [SerializeField][Range(0, 1)] private float _midAirControlMultiplier = 0.4f;

        [Header("Slopes")] // Slope Fields, slopes steeper than the CharacterController's Slope Limit make the player slide

        [Tooltip("How fast sliding speeds up on steep slopes, in meters per second squared. Steeper slopes speed up faster")]
        [SerializeField][Range(0, 100)] private float _slideAcceleration = 30f;

        [Tooltip("Top speed while sliding down steep slopes, in meters per second")]
        [SerializeField][Range(0, 20)] private float _maxSlideSpeed = 4f;

        [Tooltip("How far down the player is pulled to stay on the ground when walking down slopes and steps, in meters. 0 = off")]
        [SerializeField][Range(0, 1)] private float _groundSnapDistance = 0.3f;

        [Header("Gravity")] // Gravity Fields

        [Tooltip("Player's mass. Multiplies gravity together with the Gravity Multiplier (heavier = falls faster, jumps keep the same height)")]
        [SerializeField][Range(0.1f, 5)] private float _mass = 1f;

        [Tooltip("1 = normal gravity (9.81 m/s²), 2 = twice as strong")]
        [SerializeField][Range(0, 5)] private float _gravityMultiplier = 1f;

        [Header("Camera")] // Camera fields

        [Tooltip("Reference to the camera holder game object")]
        [SerializeField] private Transform _cameraHolderTransform;

        [Tooltip("Mouse sensitivity, degrees turned per pixel of mouse movement")]
        [SerializeField][Range(0.01f, 0.5f)] private float _mouseSensitivity = 0.05f;

        [Tooltip("How far the camera can look up and down, in degrees")]
        [SerializeField][Range(0, 90)] private float _maxCameraY = 90f;
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
        public bool IsSprinting => _controller.isGrounded && _input.SprintInput && IsInputMoving && _input.MovementInput.y > 0;
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
            CeilingCheck();         // Ceiling Check and logic
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
            bool wasGroundedBeforeMove = _controller.isGrounded;
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
            // Exponential smoothing keeps the acceleration the same on any frame rate and the factor always within [0,1]
            if (!_isSliding)
            {
                float smoothMovementFactor = 1f - Mathf.Exp(-_acceleration * Time.deltaTime);
                Velocity.x = Mathf.Lerp(Velocity.x, _moveDirection.x, smoothMovementFactor);
                Velocity.z = Mathf.Lerp(Velocity.z, _moveDirection.z, smoothMovementFactor);
            }

            //Apply the move direction
            _controller.Move(Velocity * Time.deltaTime);

            // If player was grounded before the move but not anymore, try to keep him on the ground
            if (wasGroundedBeforeMove)
                SnapToGround();
        }

        /// <summary>
        /// Keeps the player on the ground when walking down slopes and steps.
        /// Without this, the grounded downward velocity (-1) is slower than walking down a slope, so the player keeps leaving the ground.
        /// If player just left the ground without jumping or sliding, and there is walkable ground within _groundSnapDistance below him, then moves him down onto it.
        /// </summary>
        private void SnapToGround()
        {
            if (_controller.isGrounded || _isJumping || _isSliding || Velocity.y > 0) return;

            // The cast origin will be on character's feet
            var castOrigin = transform.position - new Vector3(0, _controller.height / 2 - _controller.radius, 0);
            if (Physics.SphereCast(castOrigin, _controller.radius - 0.01f, Vector3.down, out var hit, _groundSnapDistance, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) <= _controller.slopeLimit)
                _controller.Move(Vector3.down * hit.distance);      // CharacterController stops on the ground, so it can't go through it
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
                RemoveUpSlopeVelocity(slopeForce);
                Velocity += slopeForce * _slideAcceleration * Time.deltaTime;
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
        /// Removes the horizontal velocity that is going up a steep slope.
        /// Player can't climb a steep slope, so without this the speed he had before stepping on it would keep pushing him into the slope
        /// </summary>
        private void RemoveUpSlopeVelocity(Vector3 slopeForce)
        {
            Vector3 slideDirection = new Vector3(slopeForce.x, 0, slopeForce.z).normalized;     // Horizontal direction going down the slope
            float slideDirectionSpeed = Vector3.Dot(new Vector3(Velocity.x, 0, Velocity.z), slideDirection);
            if (slideDirectionSpeed < 0)
            {
                Velocity.x -= slideDirection.x * slideDirectionSpeed;
                Velocity.z -= slideDirection.z * slideDirectionSpeed;
            }
        }

        /// <summary>
        /// Clamps velocity when player is sliding on a slope
        /// - Horizontal speed is clamped to _maxSlideSpeed
        /// - Vertical speed is clamped to the speed needed to follow the slope at _maxSlideSpeed,
        ///   so it doesn't keep building up the longer the player slides
        /// </summary>
        private void ClampSlopeSlidingSpeed()
        {
            Vector3 horizontalVelocity = Vector3.ClampMagnitude(new Vector3(Velocity.x, 0, Velocity.z), _maxSlideSpeed);
            Velocity.x = horizontalVelocity.x;
            Velocity.z = horizontalVelocity.z;

            float slopeAngle = Vector3.Angle(_slopeHit.normal, Vector3.up) * Mathf.Deg2Rad;
            float maxFallSpeed = _maxSlideSpeed * Mathf.Tan(slopeAngle) + 1f;      // +1 keeps the player pressed on the slope, same as the grounded -1
            Velocity.y = Mathf.Max(Velocity.y, -maxFallSpeed);
        }

        /// <summary>
        /// Checks if player hit a ceiling during the last move.
        /// If player is still moving upwards, then cancels the upward velocity so he starts falling immediately instead of sticking to the ceiling.
        /// </summary>
        private void CeilingCheck()
        {
            if ((_controller.collisionFlags & CollisionFlags.Above) != 0 && Velocity.y > 0)
                Velocity.y = 0;
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
        /// Steep edges low enough to step over are ignored, see IsStepEdge()
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
            if (angle > _controller.slopeLimit && !IsStepEdge(hit))     // If angle exceeds the limit and it's not a small edge (hit is a steep slope)
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

        /// <summary>
        /// Checks if a steep hit is only a small edge the player can step over (the edge of a step, a lip where a ramp meets a platform).
        /// Casts a ray down a bit further up the steep surface, starting _controller.stepOffset above the hit point.
        /// If it finds walkable ground, then the steep surface is lower than the step offset, so it's a step and not a slope.
        /// </summary>
        private bool IsStepEdge(RaycastHit hit)
        {
            Vector3 upSlopeDirection = -new Vector3(hit.normal.x, 0, hit.normal.z).normalized;                  // Horizontal direction going up the steep surface
            Vector3 probeOrigin = hit.point + upSlopeDirection * 0.1f + Vector3.up * _controller.stepOffset;    // 0.1m further up the steep surface, at step offset height
            return Physics.Raycast(probeOrigin, Vector3.down, out var stepHit, _controller.stepOffset, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore)
                && Vector3.Angle(stepHit.normal, Vector3.up) <= _controller.slopeLimit;
        }
        #endregion
    }
}
