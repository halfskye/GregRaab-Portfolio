using System;
using System.Linq;
using Emerge.SDK.Core.Tracking;
using UnityEngine;

namespace Emerge.Connect.Avatar.ReadyPlayerMe
{
    [Serializable]
    public class RigMapping
    {
        public Transform VRTarget;
        public Transform IKTarget;

        public Vector3 MovementOffset;
        public Vector3 RotationOffset;

        public void Initialize()
        {
            IKTarget.position = VRTarget.TransformPoint(MovementOffset);
            IKTarget.rotation = VRTarget.rotation * Quaternion.Euler(RotationOffset);
        }
    }

    public class VRRigController : MonoBehaviour
    {
        public static bool IsUsingHandTracking => OVRInput.IsControllerConnected(OVRInput.Controller.LHand) ||
                                                 OVRInput.IsControllerConnected(OVRInput.Controller.RHand);
        [SerializeField] private VRAnimationController _vrAnimationController;

        [SerializeField] private float _turnThreshold = 60;
        [SerializeField] private float _turnSmoothness = 5;
        [SerializeField] private Transform _ikHead;
        [SerializeField] private Vector3 _headBodyOffset;

        //@TODO: Need to pull VR Targets from Rig at Runtime?
        [SerializeField] private RigMapping _head;
        [SerializeField] private RigMapping _leftHandController;
        [SerializeField] private RigMapping _rightHandController;
        [SerializeField] private RigMapping _leftHandTracking;
        [SerializeField] private RigMapping _rightHandTracking;

        private Vector3 _currentHeadForward;
        private bool _isRotatingRight;
        private bool _wasRotating;

        private OVRCameraRig _hardwareRig;

        private Transform _avatarRoot = null;
        public void SetAvatarRoot(Transform avatarRoot)
        {
            _avatarRoot = avatarRoot;
        }
        private Transform AvatarRoot => _avatarRoot.IsNullOrDestroyed() ? this.transform : _avatarRoot;

        private void Start()
        {
            ValidateVrTargets();
        }

        private void ValidateVrTargets()
        {
            if (_hardwareRig.IsNullOrDestroyed())
            {
                _hardwareRig = FindObjectOfType<OVRCameraRig>();
            }
            
            if (_head.VRTarget.IsNullOrDestroyed())
            {
                _head.VRTarget = _hardwareRig.GetComponentsInChildren<Camera>()
                    .FirstOrDefault(x => x.name == "CenterEyeAnchor")
                    ?.transform;
            }

            if (_leftHandController.VRTarget.IsNullOrDestroyed() || _leftHandTracking.VRTarget.IsNullOrDestroyed())
            {
                var leftHandAnchor = _hardwareRig.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(x => x.name == "LeftHandAnchor")
                    ?.transform;

                _leftHandController.VRTarget = leftHandAnchor;
                _leftHandTracking.VRTarget = leftHandAnchor;
            }

            if (_rightHandController.VRTarget.IsNullOrDestroyed() || _rightHandTracking.VRTarget.IsNullOrDestroyed())
            {
                var rightHandAnchor = _hardwareRig.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(x => x.name == "RightHandAnchor")
                    ?.transform;

                _rightHandController.VRTarget = rightHandAnchor;
                _rightHandTracking.VRTarget = rightHandAnchor;
            }
        }

        private void LateUpdate()
        {
            var potentialHeadForward = Vector3.ProjectOnPlane(_ikHead.forward, Vector3.up).normalized;
            var headAngleDifference = Vector3.Angle(AvatarRoot.forward, potentialHeadForward);
            var isRotating = headAngleDifference >= _turnThreshold;

            // If the angle between the head and the body is greater than the threshold or player is moving, rotate the body
            if (_vrAnimationController.IsMoving || isRotating || _currentHeadForward == Vector3.zero)
                _currentHeadForward = potentialHeadForward;

            var cachedTransform = AvatarRoot;
            cachedTransform.position = _ikHead.position + _headBodyOffset;

            AvatarRoot.forward = Vector3.Lerp(cachedTransform.forward, _currentHeadForward
                , Time.deltaTime * _turnSmoothness);

            _head.Initialize();
            // Check if the player is using the controllers or the tracking on OVRCameraRig
            if (IsUsingHandTracking)
            {
                _leftHandTracking.Initialize();
                _rightHandTracking.Initialize();
            }
            else
            {
                _leftHandController.Initialize();
                _rightHandController.Initialize();
            }

            // Check if the player is rotating right with, Only update the first frame of rotation. We don't want to change the direction while rotating
            if (isRotating && !_wasRotating)
                _isRotatingRight = Vector3.Angle(cachedTransform.right, _currentHeadForward) <= 90;
            // Update the animator with the new value
            _vrAnimationController.ToggleRotationAnimation(isRotating, _isRotatingRight);

            _wasRotating = isRotating;
        }
    }
}
