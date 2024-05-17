using System.Collections.Generic;
using Emerge.Connect.Avatar.Models;
using Emerge.Connect.Scripts.Utils;
using Emerge.SDK.Core.Tracking;
using Oculus.Avatar2;
using ReadyPlayerMe.Core;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Emerge.Connect.Avatar.ReadyPlayerMe
{
    public class RealtimeReadyPlayerMeAvatar : RealtimeAvatarBase
    {
        private AvatarObjectLoader _avatarObjectLoader;
        private GameObject _avatar;

        private readonly Vector3 _avatarPositionOffset = new Vector3(0f, 0f, 0f);

        [SerializeField]
        [Tooltip("RPM avatar ID or shortcode to load")]
        private string avatarId;

        [SerializeField] private GameObject vrRigSetup;

        private ArmatureHelper _armatureHelper = null;

        protected override void Awake()
        {
            base.Awake();

            _avatarObjectLoader = new AvatarObjectLoader();
            _avatarObjectLoader.OnCompleted += OnLoadCompleted;
            _avatarObjectLoader.OnFailed += OnLoadFailed;
        }

        protected override void ConfigureAsLocalAvatar()
        {
            model.userId = avatarId;

            LoadAvatar();
        }

        protected override bool ConfigureAsRemoteAvatar()
        {
            if (!base.ConfigureAsRemoteAvatar()) return false;

            DebugLog($"ConfigureAsRemoteAvatar - {model.userId}");

            LoadAvatar();

            return true;
        }

        private void OnLoadFailed(object sender, FailureEventArgs args)
        {
            DebugLog($"Avatar Load Failed: {args.Message} ({args.Url})", DebugLogUtilities.DebugLogType.ERROR);
        }

        private void OnLoadCompleted(object sender, CompletionEventArgs args)
        {
            SetupAvatar(args.Avatar);

            IsAvatarConfigured = true;
        }

        private void SetupAvatar(GameObject targetAvatar)
        {
            if (_avatar != null)
            {
                Destroy(_avatar);
            }

            _avatar = targetAvatar;

            // Re-parent and reset transforms
            _avatar.transform.parent = transform;
            _avatar.transform.localPosition = _avatarPositionOffset;
            _avatar.transform.localRotation = Quaternion.Euler(0, 0, 0);

            _armatureHelper = _avatar.AddComponent<ArmatureHelper>();

            if (realtimeView.isOwnedLocallySelf)
            {
                var rigBuilder = _avatar.AddComponent<RigBuilder>();

                var vrRig = Instantiate(vrRigSetup, _avatar.transform).GetComponent<Rig>();
                vrRig.transform.localPosition = Vector3.zero;
                vrRig.transform.localRotation = Quaternion.identity;

                vrRig.GetComponent<VRRigController>().SetAvatarRoot(_avatar.transform);

                var vrRigHelper = vrRig.GetComponent<VRRigHelper>();

                var avatarAnimator = _avatar.GetComponent<Animator>();
                avatarAnimator.runtimeAnimatorController = vrRigHelper.AnimatorController;

                var headBone = _armatureHelper.GetHeadBone();
                vrRigHelper.Head.data.constrainedObject = headBone;

                vrRigHelper.LeftArm.data.root = _armatureHelper.LeftArmData.GetArmBone();
                vrRigHelper.LeftArm.data.mid = _armatureHelper.LeftArmData.GetForeArmBone();
                var leftHandBone = _armatureHelper.LeftArmData.GetHandBone();
                vrRigHelper.LeftArm.data.tip = leftHandBone;

                vrRigHelper.RightArm.data.root = _armatureHelper.RightArmData.GetArmBone();
                vrRigHelper.RightArm.data.mid = _armatureHelper.RightArmData.GetForeArmBone();
                var rightHandBone = _armatureHelper.RightArmData.GetHandBone();
                vrRigHelper.RightArm.data.tip = rightHandBone;

                vrRig.GetComponent<VRAnimationController>().Animator = avatarAnimator;
                vrRig.GetComponent<VRLowerBodyIK>().Animator = avatarAnimator;

                vrRigHelper.VRHandTrackingLeft.SetWristRoot(leftHandBone);
                vrRigHelper.VRHandTrackingRight.SetWristRoot(rightHandBone);

                if (AvatarManager.Instance.avatarBodyType == AvatarManager.AvatarBodyType.HalfBody)
                {
                    _armatureHelper.ScaleLegBones(0.0f);
                }

#if UNITY_EDITOR
                vrRig.GetComponent<BoneRenderer>().transforms = _armatureHelper.GetAllBones().ToArray();
#endif //UNITY_EDITOR

                rigBuilder.layers = new List<RigLayer>()
                {
                    new(vrRig)
                };
                rigBuilder.Build();
            }
            else
            {
#if UNITY_EDITOR
                _avatar.AddComponent<BoneRenderer>().transforms = _armatureHelper.GetAllBones().ToArray();
#endif //UNITY_EDITOR
            }

            _avatar.AddComponent<EyeAnimationHandler>();
        }

        private void LoadAvatar(string urlOrId)
        {
            //remove any leading or trailing spaces
            avatarId = urlOrId.Trim(' ');
            _avatarObjectLoader.LoadAvatar(avatarId);
        }

        private void LoadAvatar()
        {
            LoadAvatar(model.userId);
        }

        protected override void ModelOnUserIdDidChange(AvatarBaseModel avatarBaseModel, string value)
        {
            base.ModelOnUserIdDidChange(avatarBaseModel, value);

            //DebugLog("ModelOnUserIdDidChange");

            ChangeAvatarId();
        }

        protected override void ModelOnAvatarDataDidChange(AvatarBaseModel avatarBaseModel, byte[] value)
        {
            base.ModelOnAvatarDataDidChange(avatarBaseModel, value);

            //DebugLog("ModelOnAvatarDataDidChange");

            ApplyAvatarData();
        }

        private void ChangeAvatarId()
        {
            //@TODO: Do we want to handle avatar ID changing?

            // _avatarEntity.SetMetaUserId(model.metaUserId);
            // if (!_isAvatarConfigured && !realtimeView.isOwnedLocallySelf)
            // {
            //     ConfigureAsRemoteAvatar();
            // }
        }

        protected override bool CaptureAvatarData()
        {
            if (!base.CaptureAvatarData()) return false;

            var allBones = _armatureHelper.GetAllBones();
            const int numDataPerBone = 3 + 4 + 3; // Position (Vec3) + Rotation (Quaternion) + Scale (Vec3)
            const int dataSize = sizeof(float);
            const int dataSizePerBone = numDataPerBone * dataSize;
            var avatarData = new byte[allBones.Count * dataSizePerBone];

            for (var boneIndex = 0; boneIndex < allBones.Count; ++boneIndex)
            {
                var indexOffset = boneIndex * dataSizePerBone;

                Transform bone = allBones[boneIndex];
                Vector3 bonePosition = bone.localPosition;
                ProperBitConverter.GetBytes(avatarData, indexOffset + 0 * dataSize, bonePosition.x);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 1 * dataSize, bonePosition.y);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 2 * dataSize, bonePosition.z);

                Quaternion boneRotation = bone.localRotation;
                ProperBitConverter.GetBytes(avatarData, indexOffset + 3 * dataSize, boneRotation.x);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 4 * dataSize, boneRotation.y);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 5 * dataSize, boneRotation.z);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 6 * dataSize, boneRotation.w);

                Vector3 boneScale = bone.localScale;
                ProperBitConverter.GetBytes(avatarData, indexOffset + 7 * dataSize, boneScale.x);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 8 * dataSize, boneScale.y);
                ProperBitConverter.GetBytes(avatarData, indexOffset + 9 * dataSize, boneScale.z);
            }

            model.avatarData = avatarData;

            //DebugLog($"CaptureAvatarData - model.avatarData Length : {model.avatarData.Length}");

            return true;
        }

        protected override bool ApplyAvatarData()
        {
            if (!base.ApplyAvatarData()) return false;

            //DebugLog($"ApplyAvatarData - avatarData Length : {model.avatarData.Length}");

            var allBones = _armatureHelper.GetAllBones();
            const int numDataPerBone = 3 + 4 + 3; // Position (Vec3) + Rotation (Quaternion) + Scale (Vec3)
            const int dataSize = sizeof(float);
            const int dataSizePerBone = numDataPerBone * dataSize;

            for (var boneIndex = 0; boneIndex < allBones.Count; ++boneIndex)
            {
                var indexOffset = boneIndex * dataSizePerBone;

                var bonePosition = new Vector3(
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 0 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 1 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 2 * dataSize)
                );
                allBones[boneIndex].localPosition = bonePosition;

                var boneRotation = new Quaternion(
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 3 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 4 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 5 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 6 * dataSize)
                );
                allBones[boneIndex].localRotation = boneRotation;

                var boneScale = new Vector3(
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 7 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 8 * dataSize),
                    ProperBitConverter.ToSingle(model.avatarData, indexOffset + 9 * dataSize)
                );
                allBones[boneIndex].localScale = boneScale;
            }

            return true;
        }

        protected override void SetAvatarVisibility(bool isVisible)
        {
            _armatureHelper.SetVisibility(isVisible);

            base.SetAvatarVisibility(isVisible);
        }

        public override bool HasJoints()
        {
            return !_armatureHelper.IsNullOrDestroyed() && _armatureHelper.HasBones();
        }

        public override Transform GetSkeletonTransform(CAPI.ovrAvatar2JointType ovrAvatar2JointType)
        {
            return _armatureHelper.GetSkeletonTransformByOvrJointType(ovrAvatar2JointType);
        }

        private void DebugLog(string message, DebugLogUtilities.DebugLogType debugLogType = DebugLogUtilities.DebugLogType.LOG)
        {
            DebugLogUtilities.Log(DebugLogUtilities.DebugInfoType.AVATAR_MANAGER, $"[RealtimeReadyPlayerMeAvatar] {message}", debugLogType, this);
        }
    }
}