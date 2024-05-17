using Emerge.SDK.Core.Tracking;
using UnityEngine;

namespace Emerge.Connect.Avatar.ReadyPlayerMe
{
    public class VRAnimationController : MonoBehaviour
    {
        public bool IsMoving { get; private set; }

        [SerializeField] private float _movingSpeedMultiplier = 90f;
        [SerializeField] private float _isMovingThreshold = 0.0005f;
        [SerializeField] private float _initialMovementThreshold = 0.005f;

        [SerializeField] private Animator _animator;
        public Animator Animator
        {
            set => _animator = value;
        }

        private readonly int _forwardAnimatorKey = Animator.StringToHash("ForwardMomentum");
        private readonly int _sideStepMomentumAnimatorKey = Animator.StringToHash("SideStepMomentum");
        private readonly int _isMovingAnimatorKey = Animator.StringToHash("IsMoving");
        private readonly int _isRotatingAnimatorKey = Animator.StringToHash("IsRotating");
        private readonly int _walkingSpeedAnimatorKey = Animator.StringToHash("WalkingSpeed");
        private readonly int _mirrorRotationAnimatorKey = Animator.StringToHash("MirrorRotation");

        private const float MomentumMultiplier = 125f;
        private const float MomentumLerpMultiplier = 1.5f;
        private const float SlerpPowerValue = 1.15f;

        private Camera _camera;
        private Vector3 _previousPosition;
        private float _forwardMomentum;
        private float _sideStepMomentum;

        private void Start()
        {
            if (_animator.IsNullOrDestroyed())
            {
                _animator = GetComponent<Animator>();
            }
            
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("No camera found");
                enabled = false;
                return;
            }

            _previousPosition = _camera.transform.position;
        }

        private void Update()
        {
            // Get the movement vector in the world space
            var movementVector = _camera.transform.position - _previousPosition;
            movementVector.y = 0;

            // Check if the camera is moving
            var wasMoving = IsMoving;
            IsMoving = wasMoving ? movementVector.magnitude > _isMovingThreshold : movementVector.magnitude > _initialMovementThreshold;
            _animator.SetBool(_isMovingAnimatorKey, IsMoving);

            // Calculate the forward momentum and side step momentum
            var forwardMomentum = Vector3.Dot(movementVector, _camera.transform.forward);
            var sideStepMomentum = Vector3.Dot(movementVector, _camera.transform.right);

            // Lerp the momentum values
            var slerpTime = Mathf.Pow(Time.deltaTime * MomentumLerpMultiplier, SlerpPowerValue);
            _forwardMomentum = Mathf.Lerp(_forwardMomentum, forwardMomentum, slerpTime);
            _sideStepMomentum = Mathf.Lerp(_sideStepMomentum, sideStepMomentum, slerpTime);

            // Update the animator parameters
            _animator.SetFloat(_forwardAnimatorKey, _forwardMomentum * MomentumMultiplier);
            _animator.SetFloat(_sideStepMomentumAnimatorKey, _sideStepMomentum * MomentumMultiplier);
            _animator.SetFloat(_walkingSpeedAnimatorKey, movementVector.magnitude * _movingSpeedMultiplier);

            // Store the previous position
            _previousPosition = _camera.transform.position;
        }

        public void ToggleRotationAnimation(bool isRotating, bool isRotatingRight)
        {
            if (_animator == null)
                return;

            // If the player is rotating left, the rotation animation should be mirrored
            var mirrorRotation = isRotatingRight;
            _animator.SetBool(_isRotatingAnimatorKey, isRotating);
            _animator.SetBool(_mirrorRotationAnimatorKey, mirrorRotation);
        }
    }
}