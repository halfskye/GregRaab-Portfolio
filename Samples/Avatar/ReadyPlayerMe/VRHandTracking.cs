using System.Collections.Generic;
using System.Linq;
using Emerge.SDK.Core;
using Emerge.SDK.Core.Tracking;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Sirenix.OdinInspector;
using UnityEngine;
using Hand = Oculus.Interaction.Input.Hand;

namespace Emerge.Connect.Avatar.ReadyPlayerMe
{
    // TODO: Hand Rotation offset
    // TODO: Finger Rotation offset

    // This script needs to grab the hand tracking data from the Oculus SDK and apply it to the ReadyPlayerMe hand model
    public class VRHandTracking : MonoBehaviour
    {
        private class HandTrackingData
        {
            public Transform Wrist;
            public Transform Thumb0;
            public Transform Thumb1;
            public Transform Thumb2;
            public Transform Thumb3;
            public Transform Index0;
            public Transform Index1;
            public Transform Index2;
            public Transform Middle0;
            public Transform Middle1;
            public Transform Middle2;
            public Transform Ring0;
            public Transform Ring1;
            public Transform Ring2;
            public Transform Pinky0;
            public Transform Pinky1;
            public Transform Pinky2;
            public Transform Pinky3;
        }

        [Title("Meta - Coordinate Conversion", "Negate Local Joints' Quaternion Components")]
        [SerializeField] private bool negateX;
        [SerializeField] private bool negateY;
        [SerializeField] private bool negateZ;
        [SerializeField] private bool negateW;

        [Title("Meta - Joint Rotational Offsets", "Rotational Offsets per Bone/Joint (after above conversion/inversion)")]
        [SerializeField] private Quaternion wristRotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion thumb0RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion thumb1RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion thumb2RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion index0RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion index1RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion index2RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion middle0RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion middle1RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion middle2RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion ring0RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion ring1RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion ring2RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion pinky0RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion pinky1RotationOffset = Quaternion.Euler(0, 0, 0);
        [SerializeField] private Quaternion pinky2RotationOffset = Quaternion.Euler(0, 0, 0);

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        [SerializeField]
        private HandType _handType;

        private IHand Hand { get; set; }

        private Transform _wristRoot = null;
        private Transform WristRoot => _wristRoot.IsNullOrDestroyed() ? this.transform : _wristRoot;
        public void SetWristRoot(Transform wristRoot)
        {
            _wristRoot = wristRoot;
        }

        private readonly HandTrackingData _handTrackingData = new HandTrackingData();

        private OVRCameraRig _hardwareRig;

        private void Start()
        {
            ValidateHand();

            // Find the hand tracking data. Oculus SDK starts at 0, ReadyPlayerMe starts at 1. So Oculus SDK Thumb0 is ReadyPlayerMe Thumb1
            _handTrackingData.Wrist = WristRoot;
            _handTrackingData.Thumb0 = WristRoot.FindChildContainingName("Thumb1");
            _handTrackingData.Thumb1 = _handTrackingData.Thumb0.GetChild(0);
            _handTrackingData.Thumb2 = _handTrackingData.Thumb1.GetChild(0);
            _handTrackingData.Thumb3 = _handTrackingData.Thumb2.GetChild(0);
            _handTrackingData.Index0 = WristRoot.FindChildContainingName("Index1");
            _handTrackingData.Index1 = _handTrackingData.Index0.GetChild(0);
            _handTrackingData.Index2 = _handTrackingData.Index1.GetChild(0);
            _handTrackingData.Middle0 = WristRoot.FindChildContainingName("Middle1");
            _handTrackingData.Middle1 = _handTrackingData.Middle0.GetChild(0);
            _handTrackingData.Middle2 = _handTrackingData.Middle1.GetChild(0);
            _handTrackingData.Ring0 = WristRoot.FindChildContainingName("Ring1");
            _handTrackingData.Ring1 = _handTrackingData.Ring0.GetChild(0);
            _handTrackingData.Ring2 = _handTrackingData.Ring1.GetChild(0);
            _handTrackingData.Pinky0 = WristRoot.FindChildContainingName("Pinky1");
            _handTrackingData.Pinky1 = _handTrackingData.Pinky0.GetChild(0);
            _handTrackingData.Pinky2 = _handTrackingData.Pinky1.GetChild(0);
            _handTrackingData.Pinky3 = _handTrackingData.Pinky2.GetChild(0);
        }

        private void ValidateHand()
        {
            if (_hardwareRig.IsNullOrDestroyed())
            {
                _hardwareRig = FindObjectOfType<OVRCameraRig>();
            }

            if (_hand.IsNullOrDestroyed())
            {
                _hand = _hardwareRig.GetComponentsInChildren<Hand>()
                    .FirstOrDefault(x => x.name == GetHandTypeName(_handType));
            }

            Hand = _hand as IHand;
        }

        private string GetHandTypeName(HandType handType)
        {
            return handType == HandType.Left ? "LeftHand" : "RightHand";
        }

        private void LateUpdate()
        {
            // If the user is using Controllers, return
            if (!VRRigController.IsUsingHandTracking)
                return;

            if (!Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
                return;

            // Create a list of tuples that each hold a Transform and a Quaternion
            var _jointTransforms = new List<(Transform, Quaternion)>();
            _jointTransforms.Add((null, Quaternion.identity)); // Wrist
            _jointTransforms.Add((null, Quaternion.identity)); // forearm
            _jointTransforms.Add((null, Quaternion.identity)); // Thumb0
            _jointTransforms.Add((_handTrackingData.Thumb0, thumb0RotationOffset));
            _jointTransforms.Add((_handTrackingData.Thumb1, thumb1RotationOffset));
            _jointTransforms.Add((_handTrackingData.Thumb2, thumb2RotationOffset));
            _jointTransforms.Add((_handTrackingData.Index0, index0RotationOffset));
            _jointTransforms.Add((_handTrackingData.Index1, index1RotationOffset));
            _jointTransforms.Add((_handTrackingData.Index2, index2RotationOffset));
            _jointTransforms.Add((_handTrackingData.Middle0, middle0RotationOffset));
            _jointTransforms.Add((_handTrackingData.Middle1, middle1RotationOffset));
            _jointTransforms.Add((_handTrackingData.Middle2, middle2RotationOffset));
            _jointTransforms.Add((_handTrackingData.Ring0, ring0RotationOffset));
            _jointTransforms.Add((_handTrackingData.Ring1, ring1RotationOffset));
            _jointTransforms.Add((_handTrackingData.Ring2, ring2RotationOffset));
            _jointTransforms.Add((null, Quaternion.identity)); // pinky0
            _jointTransforms.Add((_handTrackingData.Pinky0, pinky0RotationOffset));
            _jointTransforms.Add((_handTrackingData.Pinky1, pinky1RotationOffset));
            _jointTransforms.Add((_handTrackingData.Pinky2, pinky2RotationOffset));

            // Apply the wrist rotation offset as the wrist is not tracked by the Oculus SDK
            _handTrackingData.Wrist.localRotation *= wristRotationOffset;

            // Apply Local Rotation to each valid joint
            for (var i = 0; i < _jointTransforms.Count; ++i)
            {
                // Access the joint transform and rotation offset from the tuple
                var (jointTransform, rotationOffset) = _jointTransforms[i];
                if (jointTransform == null)
                    continue;

                jointTransform.localRotation = ConvertRotation(localJoints[i].rotation);
                WristRoot.localScale = Hand.Scale * Vector3.one;

                // Apply the finger rotation offset
                if (rotationOffset != Quaternion.identity)
                    jointTransform.localRotation *= rotationOffset;
            }
        }

        private Quaternion ConvertRotation(Quaternion rotation)
        {
            var newQuaternion = new Quaternion(rotation.z, rotation.y, rotation.x, rotation.w);
            newQuaternion.x *= negateX ? -1 : 1;
            newQuaternion.y *= negateY ? -1 : 1;
            newQuaternion.z *= negateZ ? -1 : 1;
            newQuaternion.w *= negateW ? -1 : 1;

            return newQuaternion;
        }
    }
}