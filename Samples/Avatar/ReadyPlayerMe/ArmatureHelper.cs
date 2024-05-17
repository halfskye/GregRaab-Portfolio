using System.Collections.Generic;
using System.Linq;
using Emerge.SDK.Core.Tracking;
using Oculus.Avatar2;
using UnityEngine;

namespace Emerge.Connect.Avatar.ReadyPlayerMe
{
    public class ArmatureHelper : MonoBehaviour
    {
        public class ArmatureArmData
        {
            private readonly ArmatureHelper _armatureHelper = null;
            private readonly Chirality _chirality;
            public ArmatureArmData(ArmatureHelper armatureHelper, Chirality chirality)
            {
                _armatureHelper = armatureHelper;
                _chirality = chirality;
            }

            private bool IsFullBodySkeleton => _armatureHelper._isFullBodySkeleton;
            
            private string GetChiralityName()
            {
                return _chirality == Chirality.Left ? "Left" : "Right";
            }

            private Transform _armBone = null;
            private Transform _foreArmBone = null;
            private Transform _handBone = null;

            private string GetArmBoneName()
            {
                var chirality = GetChiralityName();
                return IsFullBodySkeleton ? $"Armature/Hips/Spine/Spine1/Spine2/{chirality}Shoulder/{chirality}Arm" : "???";
            }
            public Transform GetArmBone()
            {
                if (_armBone.IsNullOrDestroyed())
                {
                    _armBone = _armatureHelper.GetAvatarRoot().Find(GetArmBoneName());

                }
                return _armBone;
            }

            public Transform GetForeArmBone()
            {
                if (_foreArmBone.IsNullOrDestroyed())
                {
                    var chirality = GetChiralityName();
                    _foreArmBone = GetArmBone().Find($"{chirality}ForeArm");
                }
                return _foreArmBone;
            }

            public Transform GetHandBone()
            {
                if (_handBone.IsNullOrDestroyed())
                {
                    var chirality = GetChiralityName();
                    _handBone = GetForeArmBone().Find($"{chirality}Hand");
                }
                return _handBone;
            }
        }
        
        private const string ARMATURE_NODE_NAME = "Armature";

        // Head
        private const string HALF_BODY_HEAD_BONE_NAME = "Armature/Hips/Spine/Neck/Head";
        private const string FULL_BODY_HEAD_BONE_NAME = "Armature/Hips/Spine/Spine1/Spine2/Neck/Head";

        private bool _isInitialized = false;
        private bool _isFullBodySkeleton;

        public ArmatureArmData LeftArmData { get; private set; } = null;
        public ArmatureArmData RightArmData { get; private set; } = null;

        private List<Transform> AllBones { get; set; } = null;
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            _isFullBodySkeleton = AvatarBoneHelper.IsFullBodySkeleton(GetAvatarRoot());
            
            _armatureRoot = GetAvatarRoot().Find(ARMATURE_NODE_NAME);

            LeftArmData = new ArmatureArmData(this, Chirality.Left);
            RightArmData = new ArmatureArmData(this, Chirality.Right);

            AllBones = _armatureRoot.GetComponentsInChildren<Transform>().ToList();
            AllBones.Add(GetAvatarRoot());
            
            _isInitialized = true;
        }
        
        private Transform _avatarRoot = null;
        private Transform GetAvatarRoot()
        {
            if (_avatarRoot.IsNullOrDestroyed())
            {
                _avatarRoot = this.transform;
            }

            return _avatarRoot;
        }

        private Transform _armatureRoot = null;
        private Transform GetArmatureRoot()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _armatureRoot;
        }

        public List<Transform> GetAllBones()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            
            return AllBones;
        }

        public bool HasBones()
        {
            return GetAllBones().Count > 0;
        }

        private bool IsFullBodySkeleton()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _isFullBodySkeleton;
        }

        private Transform _headBone = null;

        public Transform GetHeadBone()
        {
            if (_headBone.IsNullOrDestroyed())
            {
                _headBone = this.transform.Find(
                    IsFullBodySkeleton()
                        ? FULL_BODY_HEAD_BONE_NAME
                        : HALF_BODY_HEAD_BONE_NAME
                );
            }

            return _headBone;
        }

        public void ScaleLegBones(float scale)
        {
            var hipBone = _armatureRoot.Find("Hips");
            hipBone.Find("LeftUpLeg").localScale = Vector3.one * scale;
            hipBone.Find("RightUpLeg").localScale = Vector3.one * scale;
        }

        public void SetVisibility(bool isVisible)
        {
            _armatureRoot.localScale = Vector3.one * (isVisible ? 1f : 0f);
        }

        //@TODO: Probably should move this to a common place like AvatarManager or a utility there to get mapping both ways and to not have internal dependency no OVR Avatar
        public Transform GetSkeletonTransformByOvrJointType(CAPI.ovrAvatar2JointType jointType)
        {
            switch (jointType)
            {
                case CAPI.ovrAvatar2JointType.Root:
                    return _armatureRoot;
                case CAPI.ovrAvatar2JointType.Hips:
                    return _armatureRoot.Find("Hips");
                case CAPI.ovrAvatar2JointType.LeftLegUpper:
                    return _armatureRoot.Find("Hips/LeftUpLeg");
                case CAPI.ovrAvatar2JointType.LeftLegLower:
                    return _armatureRoot.Find("Hips/LeftUpLeg/LeftLeg");
                case CAPI.ovrAvatar2JointType.LeftFootAnkle:
                    return _armatureRoot.Find("Hips/LeftUpLeg/LeftLeg/LeftFoot");
                case CAPI.ovrAvatar2JointType.LeftFootBall:
                    return _armatureRoot.Find("Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase");
                case CAPI.ovrAvatar2JointType.RightLegUpper:
                    return _armatureRoot.Find("Hips/RightUpLeg");
                case CAPI.ovrAvatar2JointType.RightLegLower:
                    return _armatureRoot.Find("Hips/RightUpLeg/RightLeg");
                case CAPI.ovrAvatar2JointType.RightFootAnkle:
                    return _armatureRoot.Find("Hips/RightUpLeg/RightLeg/RightFoot");
                case CAPI.ovrAvatar2JointType.RightFootBall:
                    return _armatureRoot.Find("Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase");
                case CAPI.ovrAvatar2JointType.SpineLower:
                    return _armatureRoot.Find("Hips/Spine");
                case CAPI.ovrAvatar2JointType.SpineMiddle:
                    return _armatureRoot.Find("Hips/Spine/Spine1");
                case CAPI.ovrAvatar2JointType.SpineUpper:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2"); // Same as Chest
                case CAPI.ovrAvatar2JointType.Chest:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2"); // Same as SpineUpper
                case CAPI.ovrAvatar2JointType.Neck:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/Neck");
                case CAPI.ovrAvatar2JointType.Head:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/Neck/Head");
                case CAPI.ovrAvatar2JointType.LeftShoulder:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder");
                case CAPI.ovrAvatar2JointType.LeftArmUpper:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm");
                case CAPI.ovrAvatar2JointType.LeftArmLower:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm");
                case CAPI.ovrAvatar2JointType.LeftHandWrist:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
                case CAPI.ovrAvatar2JointType.RightShoulder:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder");
                case CAPI.ovrAvatar2JointType.RightArmUpper:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm");
                case CAPI.ovrAvatar2JointType.RightArmLower:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm");
                case CAPI.ovrAvatar2JointType.RightHandWrist:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand");
                case CAPI.ovrAvatar2JointType.LeftHandThumbTrapezium:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandThumb1");
                case CAPI.ovrAvatar2JointType.LeftHandThumbMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandThumb1/LeftHandThumb2");
                case CAPI.ovrAvatar2JointType.LeftHandThumbProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandThumb1/LeftHandThumb2/LeftHandThumb3");
                case CAPI.ovrAvatar2JointType.LeftHandThumbDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandThumb1/LeftHandThumb2/LeftHandThumb3/LeftHandThumb4");
                case CAPI.ovrAvatar2JointType.LeftHandIndexMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandIndex1");
                case CAPI.ovrAvatar2JointType.LeftHandIndexProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandIndex1/LeftHandIndex2");
                case CAPI.ovrAvatar2JointType.LeftHandIndexIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandIndex1/LeftHandIndex2/LeftHandIndex3");
                case CAPI.ovrAvatar2JointType.LeftHandIndexDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandIndex1/LeftHandIndex2/LeftHandIndex3/LeftHandIndex4");
                case CAPI.ovrAvatar2JointType.LeftHandMiddleMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandMiddle1");
                case CAPI.ovrAvatar2JointType.LeftHandMiddleProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandMiddle1/LeftHandMiddle2");
                case CAPI.ovrAvatar2JointType.LeftHandMiddleIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandMiddle1/LeftHandMiddle2/LeftHandMiddle3");
                case CAPI.ovrAvatar2JointType.LeftHandMiddleDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandMiddle1/LeftHandMiddle2/LeftHandMiddle3/LeftHandMiddle4");
                case CAPI.ovrAvatar2JointType.LeftHandRingMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandRing1");
                case CAPI.ovrAvatar2JointType.LeftHandRingProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandRing1/LeftHandRing2");
                case CAPI.ovrAvatar2JointType.LeftHandRingIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandRing1/LeftHandRing2/LeftHandRing3");
                case CAPI.ovrAvatar2JointType.LeftHandRingDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandRing1/LeftHandRing2/LeftHandRing3/LeftHandRing4");
                case CAPI.ovrAvatar2JointType.LeftHandPinkyMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandPinky1");
                case CAPI.ovrAvatar2JointType.LeftHandPinkyProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandPinky1/LeftHandPinky2");
                case CAPI.ovrAvatar2JointType.LeftHandPinkyIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandPinky1/LeftHandPinky2/LeftHandPinky3");
                case CAPI.ovrAvatar2JointType.LeftHandPinkyDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandPinky1/LeftHandPinky2/LeftHandPinky3/LeftHandPinky4");
                case CAPI.ovrAvatar2JointType.RightHandThumbTrapezium:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandThumb1");
                case CAPI.ovrAvatar2JointType.RightHandThumbMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandThumb1/RightHandThumb2");
                case CAPI.ovrAvatar2JointType.RightHandThumbProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandThumb1/RightHandThumb2/RightHandThumb3");
                case CAPI.ovrAvatar2JointType.RightHandThumbDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandThumb1/RightHandThumb2/RightHandThumb3/RightHandThumb4");
                case CAPI.ovrAvatar2JointType.RightHandIndexMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandIndex1");
                case CAPI.ovrAvatar2JointType.RightHandIndexProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandIndex1/RightHandIndex2");
                case CAPI.ovrAvatar2JointType.RightHandIndexIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandIndex1/RightHandIndex2/RightHandIndex3");
                case CAPI.ovrAvatar2JointType.RightHandIndexDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandIndex1/RightHandIndex2/RightHandIndex3/RightHandIndex4");
                case CAPI.ovrAvatar2JointType.RightHandMiddleMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandMiddle1");
                case CAPI.ovrAvatar2JointType.RightHandMiddleProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandMiddle1/RightHandMiddle2");
                case CAPI.ovrAvatar2JointType.RightHandMiddleIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandMiddle1/RightHandMiddle2/RightHandMiddle3");
                case CAPI.ovrAvatar2JointType.RightHandMiddleDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandMiddle1/RightHandMiddle2/RightHandMiddle3/RightHandMiddle4");
                case CAPI.ovrAvatar2JointType.RightHandRingMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandRing1");
                case CAPI.ovrAvatar2JointType.RightHandRingProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandRing1/RightHandRing2");
                case CAPI.ovrAvatar2JointType.RightHandRingIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandRing1/RightHandRing2/RightHandRing3");
                case CAPI.ovrAvatar2JointType.RightHandRingDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandRing1/RightHandRing2/RightHandRing3/RightHandRing4");
                case CAPI.ovrAvatar2JointType.RightHandPinkyMeta:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandPinky1");
                case CAPI.ovrAvatar2JointType.RightHandPinkyProximal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandPinky1/RightHandPinky2");
                case CAPI.ovrAvatar2JointType.RightHandPinkyIntermediate:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandPinky1/RightHandPinky2/RightHandPinky3");
                case CAPI.ovrAvatar2JointType.RightHandPinkyDistal:
                    return _armatureRoot.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHandPinky1/RightHandPinky2/RightHandPinky3/RightHandPinky4");
                default:
                    return null;
            }
        }

        private void PrintAllBones()
        {
            Debug.LogError("AVATAR: ALL BONES >>>>>");
            for (int i = 0; i < AllBones.Count; i++)
            {
                var bone = AllBones[i];
                Debug.LogError($"BONE {i}: {bone.name}");
            }
        }
    }
}
