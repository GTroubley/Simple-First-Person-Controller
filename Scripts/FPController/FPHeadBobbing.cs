using UnityEngine;

namespace FPController
{
    public class FPHeadBobbing : MonoBehaviour
    {
        [SerializeField, Range(0, 20f)] float _frequency = 15f;
        [SerializeField, Range(0, 0.1f)] float _amplitude = 0.04f;
        [SerializeField, Range(1, 2f)] float _sprintMultiplier = 1.3f;

        private FPController _player;
        private Transform _cameraHolderTransform;
        private float _defaultPosY = 0;
        private float _timer = 0;

        private void Awake()
        {
            _player = GetComponent<FPController>();
            _cameraHolderTransform = GetComponentInChildren<Camera>().transform.parent;
        }

        // On Start caches the camera position on the Y-Axis
        private void Start() => _defaultPosY = _cameraHolderTransform.localPosition.y;

        private void Update()
        {
            // Depending wether player is sprinting or not to determine the frequency and amplitude of the bob
            float frequency = _player.IsSprinting ? _frequency * _sprintMultiplier : _frequency;
            float amplitude = _player.IsSprinting ? _amplitude * _sprintMultiplier : _amplitude;

            // If player is moving
            if (IsPlayerMoving())
            {
                // Start the timer and move the camera on the Y-Axis for the bob
                _timer += Time.deltaTime * frequency;
                _cameraHolderTransform.localPosition = new Vector3(_cameraHolderTransform.localPosition.x, _defaultPosY + Mathf.Sin(_timer) * amplitude, _cameraHolderTransform.localPosition.z);
            }
            else
            {
                // Resets the timer and gradually reset the camera position on the Y-Axis
                _timer = 0;
                _cameraHolderTransform.localPosition = new Vector3(_cameraHolderTransform.localPosition.x, Mathf.Lerp(_cameraHolderTransform.localPosition.y, _defaultPosY, Time.deltaTime * frequency), _cameraHolderTransform.localPosition.z);
            }
        }

        private bool IsPlayerMoving()
        {
            return _player.IsGrounded && (Mathf.Abs(_player.Velocity.x) > 0.1f || Mathf.Abs(_player.Velocity.z) > 0.1f);
        }
    }
}
