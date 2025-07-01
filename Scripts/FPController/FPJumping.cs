using UnityEngine;

namespace FPController
{
    [RequireComponent(typeof(FPController))]
    public class FPJumping : MonoBehaviour
    {
        [Header("Jumping")]

        [Tooltip("Desired jump height")]
        [SerializeField] float _jumpHeight = 1f;

        [Tooltip("Jump buffer time to forgive bad timing jump attempts")]
        [SerializeField][Range(0, 0.2f)] float _jumpBufferTime = 0.05f;

        [Tooltip("Jump grace time to allow jumping even after not being grounded")]
        [SerializeField] float _jumpGroundGraceTime = 0.2f;

        private FPController _player;
        private FPInputManager _input;
        private bool _tryingToJump;
        private float _lastJumpPressTime;
        private float _lastGroundedTime;

        private void Awake()
        {
            _player = GetComponent<FPController>();
            _input = GetComponent<FPInputManager>();
        }

        private void OnEnable()
        {
            _player.OnBeforeMove += PrepareForJump;
            _input.OnJumpPressed += OnJump;
            _player.OnGroundStateChange += OnGroundStateChange;
        }

        private void OnDisable()
        {
            _player.OnBeforeMove -= PrepareForJump;
            _input.OnJumpPressed -= OnJump;
            _player.OnGroundStateChange -= OnGroundStateChange;
        }

        /// <summary>
        /// Resets _lastGroundedTime
        /// This method is invoked at OnGroundStateChange in UpdateGround() of the FPController.cs
        /// </summary>
        /// <param name="isGrounded"></param>
        private void OnGroundStateChange(bool isGrounded)
        {
            if (!isGrounded) _lastGroundedTime = Time.time;
        }

        /// <summary>
        /// This method is invoked at FPInputManager in OnJump() of the FPInputManager.cs
        /// </summary>
        private void OnJump()
        {
            _tryingToJump = true;
            _lastJumpPressTime = Time.time;
        }

        /// <summary>
        /// Handles jump logic
        /// This method is invoked at OnBeforeMove in UpdateMovement() of the FPController.cs
        /// </summary>
        private void PrepareForJump()
        {
            // If player is not pressing jump and not jumping already or is sliding
            // then don't proceed with jump preparation calculations
            if ((!_input.JumpInput && !_player.IsJumping) || _player.IsSliding) return;

            // Resets IsJumping if player is grounded
            if (_player.IsGrounded) _player.IsJumping = false;

            // Checks if player was trying to jump during the jump buffer time
            bool wasTryingToJump = Time.time - _lastJumpPressTime < _jumpBufferTime;
            // Checks if player was grounded the last _jumpGroundGraceTime 
            // (allows the player to jump even for a short time after not being grounded)
            bool wasGrounded = Time.time - _lastGroundedTime < _jumpGroundGraceTime;

            bool isOrWasTryingToJump = _tryingToJump || (wasTryingToJump && _player.IsGrounded);
            bool isOrWasGrounded = _player.IsGrounded || wasGrounded;

            // If player isn't on a slope and not already jumping and is or was grounded and trying to jump
            // Adds to his vertical velocity to perform the jump
            if (!_player.IsOnSlope && !_player.IsJumping && isOrWasTryingToJump && isOrWasGrounded)
            {
                _player.Velocity.y += Mathf.Sqrt(-2 * Physics.gravity.y * _player.PlayerMass * _player.GravityMultiplier * _jumpHeight);
                _player.IsJumping = true;
                _player.MovementSpeedMultiplier = 0f;
            }
            _tryingToJump = false;
        }
    }
}
