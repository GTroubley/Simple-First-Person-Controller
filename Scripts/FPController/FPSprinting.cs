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

        // This method is invoked at OnBeforeMove in UpdateMovement() of the FPController.cs 
        private void PrepareForSprint()
        {
            // If the sprint key is pressed and player is not jumping, checks if player is moving forward and will apply the sprinting multiplier 
            // only if player moves forward not backwards or side walking
            if (!_input.SprintInput || _player.IsJumping) return;

            var forwardMovementFactor = Mathf.Clamp01(Vector3.Dot(_player.transform.forward, _player.Velocity.normalized));
            var multiplier = Mathf.Lerp(1f, _speedMultiplier, forwardMovementFactor);
            _player.MovementSpeedMultiplier *= multiplier;
        }
    }
}
