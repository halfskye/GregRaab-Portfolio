using Avatar.Models;
using Scripts.Utils;
using Cloud;
using Tracking;
using Normal.Realtime;
using Oculus.Avatar2;
using UnityEngine;

namespace Avatar
{
    public class RealtimeAvatarBase : RealtimeComponent<AvatarBaseModel>
    {
        protected bool IsAvatarConfigured { get; set; } = false;

        [SerializeField] private float streamSampleRate = 0.05f;
        private float _streamSampleTimer = 0f;

        [SerializeField] AvatarInactiveStateManager avatarInactiveStateManager;

        [SerializeField] private int pauseTime = 10;
        private float _pauseTimer = 0f;

        private RealtimeAvatarVoice _realtimeAvatarVoice;

        public UserStateSync UserStateSync { get; private set; }

        protected virtual void Awake()
        {
            UserStateSync = this.GetComponent<UserStateSync>();

            AvatarManager.OnIsAvatarVisibilityToggled += SetAvatarVisibility;
        }

        protected virtual void Start()
        {
            AvatarManager.Instance.RegisterAvatar(realtimeView.ownerIDSelf, this);
            avatarInactiveStateManager.InitUIManger();
            ConfigureAvatar();

            SetAvatarVisibility(AvatarManager.Instance.IsAvatarVisibiltyEnabled);
        }

        protected virtual void OnDestroy()
        {
            if (AvatarManager.Instance != null)
            {
                AvatarManager.Instance.UnregisterAvatar(this);
            }

            AvatarManager.OnIsAvatarVisibilityToggled -= SetAvatarVisibility;
        }

        private void Update()
        {
            UpdatePauseState();
        }

        protected override void OnRealtimeModelReplaced(AvatarBaseModel previousModel, AvatarBaseModel currentModel)
        {
            base.OnRealtimeModelReplaced(previousModel, currentModel);

            if (previousModel != null)
            {
                previousModel.avatarDataDidChange -= ModelOnAvatarDataDidChange;
                previousModel.userIdDidChange -= ModelOnUserIdDidChange;
            }
            if (currentModel != null)
            {
                currentModel.avatarDataDidChange += ModelOnAvatarDataDidChange;
                currentModel.userIdDidChange += ModelOnUserIdDidChange;
            }
        }

        private void ConfigureAvatar()
        {
            if (realtimeView.isOwnedLocallySelf)
            {
                // Local avatar
                ConfigureAsLocalAvatar();
            }
            else
            {
                if (!IsAvatarConfigured)
                {
                    ConfigureAsRemoteAvatar();
                }
            }
        }

        private void LateUpdate()
        {
            // Local avatar has fully updated this frame and can send data to the network
            if (!realtimeView.isOwnedLocallySelf) return;

            _streamSampleTimer -= Time.deltaTime;
            if (_streamSampleTimer <= 0f)
            {
                CaptureAvatarData();
                _streamSampleTimer = streamSampleRate;
            }
        }

        protected virtual void ConfigureAsLocalAvatar()
        {
            IsAvatarConfigured = true;
        }

        protected virtual bool ConfigureAsRemoteAvatar()
        {
            if (IsAvatarConfigured || model.userId == AvatarBaseModel.INVALID_USER_ID) return false;

            DebugLog($"ConfigureAsRemoteAvatar - {model.userId}");

            return true;
        }

        protected virtual void ModelOnUserIdDidChange(AvatarBaseModel avatarBaseModel, string value)
        {
            //DebugLog("ModelOnUserIdDidChange");
        }

        protected virtual void ModelOnAvatarDataDidChange(AvatarBaseModel avatarBaseModel, byte[] value)
        {
            //DebugLog("ModelOnAvatarDataDidChange");
            StartPauseTimer();
        }

        private void StartPauseTimer()
        {
            _pauseTimer = pauseTime;
        }

        private void UpdatePauseState()
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer <= 0f)
                {
                    SetAvatarActive(false);
                }
            }
        }

        protected virtual bool CaptureAvatarData()
        {
            return IsAvatarConfigured &&
                   AvatarManager.Instance.CanCaptureBoneData;
        }

        public void SetAvatarActiveWithPauseTimer(bool isActive)
        {
            StartPauseTimer();
            SetAvatarActive(isActive);
        }

        private void SetAvatarActive(bool isActive)
        {
            SetAvatarVisibility(isActive && AvatarManager.Instance.IsAvatarVisibiltyEnabled);
            avatarInactiveStateManager.SetAvatarInactiveUiActive(!isActive && !Cloud.IsGuestMode && AvatarManager.Instance.IsAvatarVisibiltyEnabled);
        }

        protected virtual void SetAvatarVisibility(bool isVisible)
        {
            if (isVisible)
            {
                ApplyAvatarData();
            }
        }

        protected virtual bool ApplyAvatarData()
        {
            if (realtimeView.isOwnedLocallySelf) return false;
            if (!IsAvatarConfigured) return false;
            if (!AvatarManager.Instance.IsAvatarVisibiltyEnabled) return false;

            DebugLog($"ApplyAvatarData - avatarData Length : {model.avatarData.Length}");

            return true;
        }

        public RealtimeAvatarVoice GetAvatarVoice()
        {
            if (!_realtimeAvatarVoice.IsNullOrDestroyed())
            {
                return _realtimeAvatarVoice;
            }

            _realtimeAvatarVoice = GetComponent<RealtimeAvatarVoice>();
            if (_realtimeAvatarVoice.IsNullOrDestroyed())
            {
                _realtimeAvatarVoice = GetComponentInChildren<RealtimeAvatarVoice>();
            }

            return _realtimeAvatarVoice;
        }

        public virtual bool HasJoints()
        {
            return false;
        }

        public bool IsLocal()
        {
            return realtimeView.isOwnedLocallySelf;
        }

        public virtual Transform GetSkeletonTransform(CAPI.ovrAvatar2JointType ovrAvatar2JointType)
        {
            return null;
        }

        private void DebugLog(string message, DebugLogUtilities.DebugLogType debugLogType = DebugLogUtilities.DebugLogType.LOG)
        {
            DebugLogUtilities.Log(DebugLogUtilities.DebugInfoType.AVATAR_MANAGER, $"[RealtimeAvatarBase] {message}", debugLogType, this);
        }
    }
}
