using Avatar.AvatarAudioProcessor;
using Avatar.Models;
using Scripts.Utils;
using Oculus.Avatar2;
using Oculus.Platform;
using Unity.Collections;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarEntity;
using CAPI = Oculus.Avatar2.CAPI;

namespace Avatar.Meta
{
    public class RealtimeMetaAvatar : RealtimeAvatarBase
    {
        private NetworkedAvatarEntity _avatarEntity;
        private OVRCameraRig _hardwareRig;

        public enum LipSyncMode
        {
            AnalyseOnSpeakerClient,
            AnalyseOnReceiverClient,
            AnalyseOnBoth
        }
        public LipSyncMode lipSyncMode = LipSyncMode.AnalyseOnReceiverClient;

        protected override void Awake()
        {
            base.Awake();

            _avatarEntity = GetComponentInChildren<NetworkedAvatarEntity>();
            _hardwareRig = FindObjectOfType<OVRCameraRig>();
        }

        protected override void ConfigureAsLocalAvatar()
        {
            LogIntoMetaAndConfigureAsLocalAvatar();
        }

        private void LogIntoMetaAndConfigureAsLocalAvatar()
        {
            Users.GetLoggedInUser().OnComplete(GetLoggedInLocalUserCallback);
        }

        private void GetLoggedInLocalUserCallback(Message<Oculus.Platform.Models.User> message)
        {
            if (message.IsError) return;

            model.userId = message.Data.ID.ToString();
            _avatarEntity.SetMetaUserId(model.userId);
            DebugLog($"Meta Avatar ID: {model.userId}");

            var inputManager = _hardwareRig.GetComponentInChildren<SampleInputManager>();
            _avatarEntity.SetBodyTracking(inputManager);
            if (lipSyncMode == LipSyncMode.AnalyseOnSpeakerClient || lipSyncMode == LipSyncMode.AnalyseOnBoth)
            {
                var lipSyncContext = _hardwareRig.GetComponentInChildren<OvrAvatarLipSyncContext>();
                _avatarEntity.SetLipSync(lipSyncContext);

                var realtimeAvatarVoice = this.GetComponent<RealtimeAvatarVoice>();
                if (realtimeAvatarVoice != null)
                {
                    var ovrAvatarAudioProcessor = realtimeAvatarVoice.GetComponent<OvrAvatarAudioProcessor>();
                    if (ovrAvatarAudioProcessor != null)
                    {
                        ovrAvatarAudioProcessor.lipSyncContext = lipSyncContext;
                    }
                }
            }
            _avatarEntity.ConfigureAsLocal();

            // TODO: Figure out a way to disable the hand visuals without disabling the colliders.
            // Disabling this function for now as it is also disabling the Oculus's HandCapsuleColliders.
            // HideSyntheticHandVisuals();

            IsAvatarConfigured = true;
        }

        protected override bool ConfigureAsRemoteAvatar()
        {
            if (!base.ConfigureAsRemoteAvatar()) return false;

            DebugLog($"ConfigureAsRemoteAvatar - {model.userId}");

            _avatarEntity.SetMetaUserId(model.userId);
            _avatarEntity.ConfigureAsRemote();
            if (lipSyncMode == LipSyncMode.AnalyseOnReceiverClient || lipSyncMode == LipSyncMode.AnalyseOnBoth)
            {
                var lipSyncContext = this.GetComponentInChildren<OvrAvatarLipSyncContext>();
                _avatarEntity.SetLipSync(lipSyncContext);
            }

            IsAvatarConfigured = true;

            return true;
        }

        protected override void ModelOnUserIdDidChange(AvatarBaseModel avatarBaseModel, string value)
        {
            //DebugLog("ModelOnUserIdDidChange");

            base.ModelOnUserIdDidChange(avatarBaseModel, value);

            ChangeMetaUserId();
        }

        protected override void ModelOnAvatarDataDidChange(AvatarBaseModel avatarBaseModel, byte[] value)
        {
            //DebugLog("ModelOnAvatarDataDidChange");

            base.ModelOnAvatarDataDidChange(avatarBaseModel, value);

            ApplyAvatarData();
        }

        private void ChangeMetaUserId()
        {
            _avatarEntity.SetMetaUserId(model.userId);
            if (!IsAvatarConfigured && !realtimeView.isOwnedLocallySelf)
            {
                ConfigureAsRemoteAvatar();
            }
        }

        protected override bool CaptureAvatarData()
        {
            if (!base.CaptureAvatarData()) return false;
            if (!_avatarEntity.CanStreamJointData()) return false;

            //for (int streamLod = (int)StreamLOD.High; streamLod <= (int)StreamLOD.Low; ++streamLod) CaptureLODAvatar((StreamLOD) streamLod, AvatarDataFull);
            CaptureLODAvatar(StreamLOD.Medium);

            //DebugLog($"CaptureAvatarData - model.avatarData Length : {model.avatarData.Length}");

            return true;
        }

        private uint CaptureLODAvatar(StreamLOD streamLod)
        {
            NativeArray<byte> avatarData = new NativeArray<byte>(0, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory); ;
            uint avatarDataCount = _avatarEntity.RecordStreamData_AutoBuffer(streamLod, ref avatarData);

            //DebugLog($"CaptureLODAvatar - avatarDataCount : {avatarDataCount}");

            byte[] avatarDataArray = new byte[avatarData.Length];
            //DebugLog($"CaptureLODAvatar {streamLod}: {avatarData.Length} / {avatarDataCount}");
            avatarData.CopyTo(avatarDataArray);

            model.avatarData = avatarDataArray;

            avatarData.Dispose();
            return avatarDataCount;
        }

        protected override void SetAvatarVisibility(bool isVisible)
        {
            _avatarEntity.Hidden = !isVisible;

            base.SetAvatarVisibility(isVisible);
        }

        protected override bool ApplyAvatarData()
        {
            if (!base.ApplyAvatarData()) return false;

            //DebugLog($"ApplyAvatarData - avatarData Length : {model.avatarData.Length}");

            _avatarEntity.ApplyStreamData(model.avatarData);

            return true;
        }

        public override bool HasJoints()
        {
            return _avatarEntity.HasJoints;
        }

        public override Transform GetSkeletonTransform(CAPI.ovrAvatar2JointType ovrAvatar2JointType)
        {
            var skeletonTransform = _avatarEntity.GetSkeletonTransform(ovrAvatar2JointType);
            DebugLog($"Skeleton Transform: {ovrAvatar2JointType} | {skeletonTransform.name}", DebugLogUtilities.DebugLogType.ERROR);
            return _avatarEntity.GetSkeletonTransform(ovrAvatar2JointType);
        }

        private void DebugLog(string message, DebugLogUtilities.DebugLogType debugLogType = DebugLogUtilities.DebugLogType.LOG)
        {
            DebugLogUtilities.Log(DebugLogUtilities.DebugInfoType.AVATAR_MANAGER, $"[RealtimeMetaAvatar] {message}", debugLogType, this);
        }
    }
}
