using UnityEngine;

namespace FPController
{
    [RequireComponent(typeof(FPController))]
    public class FPFootsteps : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private bool _playFootsteps = true;
        [SerializeField] private float _minRequiredSpeed = 3f;
        [SerializeField] private float _walkStepInterval = 0.8f;
        [SerializeField] private float _sprintStepInterval = 0.5f;
        [SerializeField] private AudioClip[] _sounds;

        private FPController _player;
        private float _lastStepTimer;

        private void Awake() => _player = GetComponent<FPController>();

        private void Update()
        {
            // Play footsteps only if _playFootsteps is enabled and has an AudioSource
            if (!_playFootsteps || _audioSource == null || _player == null) return;
            // If audioSource is not already playing a sound and player is grounded and moves using input with a speed greater than the _minRequiredSpeed
            if (!_audioSource.isPlaying && _player.IsGrounded && _player.IsInputMoving && _player.Velocity.magnitude > _minRequiredSpeed)
            {
                float interval = _player.IsSprinting ? _sprintStepInterval : _walkStepInterval;
                if (IsTimeToSprint(interval) && _sounds.Length > 0)
                {
                    _audioSource.PlayOneShot(_sounds[Random.Range(0, _sounds.Length)]);
                    _lastStepTimer = Time.time;
                }
            }
        }

        private bool IsTimeToSprint(float interval) => Time.time - _lastStepTimer > interval;
    }
}
