using UnityEngine;

namespace FPController
{
    [RequireComponent(typeof(FPController))]
    public class FPSprinting : MonoBehaviour
    {
        [Header("Sprinting")]
        [SerializeField] private float _speedMultiplier = 1.5f;

        FPController _player;
        FPInputManager _input;

        private void Awake()
        {
            _player = GetComponent<FPController>();
            _input = GetComponent<FPInputManager>();
        }

        private void OnEnable() => _player.OnBeforeMove += PrepareForSprint;

        private void OnDisable() => _player.OnBeforeMove -= PrepareForSprint;

        /// <summary>
        /// Responsible for checking if player is moving forward, if so then will apply the sprinting multiplier.
        /// Sprinting does not work when moving backwards!
        /// This method is invoked at OnBeforeMove in UpdateMovement() of the FPController.cs 
        /// </summary>
        private void PrepareForSprint()
        {
            if (!_input.SprintInput || _player.IsJumping) return;
            // Compute how much the player is moving in the forward direction (1 = forward, 0 = sideways, -1 = backward)
            float forwardMovementFactor = Mathf.Clamp01(Vector3.Dot(_player.transform.forward, _player.Velocity.normalized));
            float multiplier = Mathf.Lerp(1f, _speedMultiplier, forwardMovementFactor);
            _player.MovementSpeedMultiplier *= multiplier;
        }
    }
}
