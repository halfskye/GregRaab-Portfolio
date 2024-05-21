using Tracking;
using UnityEngine;

namespace Avatar.ReadyPlayerMe
{
    public class VRLowerBodyIK : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        public Animator Animator
        {
            set => _animator = value;
        }

        [HideInInspector, SerializeField] private float _rightAnimatorFootPositionOffset;
        [HideInInspector, SerializeField] private float _leftAnimatorFootPositionOffset;

        [SerializeField, Range(0, 1)] private float _leftFootPositionWeight;
        [SerializeField, Range(0, 1)] private float _rightFootPositionWeight;

        [SerializeField, Range(0, 1)] private float _leftFootRotationWeight;
        [SerializeField, Range(0, 1)] private float _rightFootRotationWeight;

        [SerializeField] private float _footPositionOffset = 0.135f;
        [SerializeField] private float _animationFootOffsetMultiplier = 1;

        [SerializeField] private Vector3 _raycastLeftOffset;
        [SerializeField] private Vector3 _raycastRightOffset;

        private void Start()
        {
            if (_animator.IsNullOrDestroyed())
            {
                _animator = GetComponent<Animator>();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            var leftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            var rightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot);

            var leftFootRaycast = Physics.Raycast(leftFootPosition + _raycastLeftOffset, Vector3.down, out var leftFootHit);
            var rightFootRaycast = Physics.Raycast(rightFootPosition + _raycastRightOffset, Vector3.down, out var rightFootHit);

            CalculateLeftFootIK(leftFootRaycast, leftFootHit);

            CalculateRightFootIK(rightFootRaycast, rightFootHit);
        }

        private void CalculateLeftFootIK(bool leftFootRaycast, RaycastHit leftFootHit)
        {
            const AvatarIKGoal ikGoal = AvatarIKGoal.LeftFoot;

            if (!leftFootRaycast)
            {
                _animator.SetIKPositionWeight(ikGoal, 0);
                _animator.SetIKRotationWeight(ikGoal, 0);
                return;
            }

            _animator.SetIKPositionWeight(ikGoal, _leftFootPositionWeight);
            _animator.SetIKPosition(ikGoal, leftFootHit.point + Vector3.up * (_footPositionOffset + (_leftAnimatorFootPositionOffset * _animationFootOffsetMultiplier)));

            var leftFootRotation =
                Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, leftFootHit.normal),
                    leftFootHit.normal);

            _animator.SetIKRotationWeight(ikGoal, _leftFootRotationWeight);
            _animator.SetIKRotation(ikGoal, leftFootRotation);
        }

        private void CalculateRightFootIK(bool rightFootRaycast, RaycastHit rightFootHit)
        {
            const AvatarIKGoal ikGoal = AvatarIKGoal.RightFoot;

            if (!rightFootRaycast)
            {
                _animator.SetIKPositionWeight(ikGoal, 0);
                _animator.SetIKRotationWeight(ikGoal, 0);
                return;
            }

            _animator.SetIKPositionWeight(ikGoal, _rightFootPositionWeight);
            _animator.SetIKPosition(ikGoal, rightFootHit.point + Vector3.up * (_footPositionOffset + (_rightAnimatorFootPositionOffset * _animationFootOffsetMultiplier)));

            var rightFootRotation =
                Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, rightFootHit.normal),
                    rightFootHit.normal);

            _animator.SetIKRotationWeight(ikGoal, _rightFootRotationWeight);
            _animator.SetIKRotation(ikGoal, rightFootRotation);
        }
    }
}