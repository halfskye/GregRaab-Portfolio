using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Emerge.Connect.Context;
using Emerge.Connect.Lifecycle;
using Emerge.Connect.Locomotion;
using Emerge.Connect.Normcore;
using Emerge.Connect.Scripts.Utils;
using Emerge.Connect.States;
using Normal.Realtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using static Emerge.Connect.Scripts.Utils.DebugLogUtilities;
using Object = UnityEngine.Object;

namespace Emerge.Connect.Avatar
{
    public class AvatarManager : MonoBehaviour
    {
        public enum AvatarType
        {
            Meta = 0,
            ReadyPlayerMe = 1,
        }
        [SerializeField]
        private AvatarType _avatarType = AvatarType.Meta;

        public enum AvatarBodyType
        {
            HalfBody = 0,
            FullBody = 1,
        }
        [SerializeField, ShowIf("_avatarType", AvatarType.ReadyPlayerMe)]
        private AvatarBodyType _avatarBodyType = AvatarBodyType.HalfBody;
        public AvatarBodyType avatarBodyType => _avatarBodyType;

        [FormerlySerializedAs("localAvatarPrefab"), SerializeField]
        private GameObject localMetaAvatarPrefab;

        [SerializeField]
        private GameObject localReadyPlayerMeAvatarPrefab;

        public RealtimeAvatarBase LocalAvatar { get; private set; }

        private Dictionary<int, RealtimeAvatarBase> Avatars { get; set; } = new Dictionary<int, RealtimeAvatarBase>();

        private OVRCameraRig _hardwareRig;

        private Realtime _realtime;

        public delegate void AvatarCreatedDestroyed(int clientId, RealtimeAvatarBase avatar, bool isLocalAvatar);
        public static event AvatarCreatedDestroyed OnAvatarCreated;
        public static event AvatarCreatedDestroyed OnAvatarDestroyed;

        // Visibility
        public bool IsAvatarVisibiltyEnabled { get; private set; } = true;
        public static Action<bool> OnIsAvatarVisibilityToggled;

        public bool CanCaptureBoneData => IsAvatarVisibiltyEnabled ||
                                          ApplicationContext.CurrentState == ApplicationState.Calibration;

        public static AvatarManager Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null)
            {
                DebugLog("Instance already exists.", DebugLogType.ERROR);
                DestroyImmediate(this.gameObject);
            }
            Instance = this;

            _realtime = GetComponentInParent<Realtime>();
            _hardwareRig = FindObjectOfType<OVRCameraRig>();

            SubscribeToEvents(true);
        }

        private void OnDestroy()
        {
            SubscribeToEvents(false);
        }

        #region Event Subscriptions

        private void SubscribeToEvents(bool subscribe)
        {
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ConnectionManager.OnDidConnectToRoom += OnConnectionManagerDidConnectToRoom,
                handler => ConnectionManager.OnDidConnectToRoom -= OnConnectionManagerDidConnectToRoom
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => Teleporter.OnTeleport += OnLocalPlayerTeleport,
                handler => Teleporter.OnTeleport -= OnLocalPlayerTeleport
            );

            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationContext.OnDidEnterState += OnApplicationDidEnterState,
                handler => ApplicationContext.OnDidEnterState -= OnApplicationDidEnterState
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationContext.OnDidExitState += OnApplicationDidExitState,
                handler => ApplicationContext.OnDidExitState -= OnApplicationDidExitState
            );

            #region ApplicationLifecycleManager
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnHMDAcquired += OnHMDAcquired,
                handler => ApplicationLifecycleManager.OnHMDAcquired -= OnHMDAcquired
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnHMDMounted += OnHMDMounted,
                handler => ApplicationLifecycleManager.OnHMDMounted -= OnHMDMounted
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnHMDUnmounted += OnHMDUnmounted,
                handler => ApplicationLifecycleManager.OnHMDUnmounted -= OnHMDUnmounted
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnVrFocusAcquired += OnVrFocusAcquired,
                handler => ApplicationLifecycleManager.OnVrFocusAcquired -= OnVrFocusAcquired
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnVrFocusLost += OnVrFocusLost,
                handler => ApplicationLifecycleManager.OnVrFocusLost -= OnVrFocusLost
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnTrackingLost += OnTrackingLost,
                handler => ApplicationLifecycleManager.OnTrackingLost -= OnTrackingLost
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnTrackingAcquired += OnTrackingAcquired,
                handler => ApplicationLifecycleManager.OnTrackingAcquired -= OnTrackingAcquired
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnApplicationFocusEvent += OnApplicationFocusEvent,
                handler => ApplicationLifecycleManager.OnApplicationFocusEvent -= OnApplicationFocusEvent
            );
            EventUtilities.ConditionalSubscribeOrUnsubscribe(subscribe,
                handler => ApplicationLifecycleManager.OnApplicationPauseEvent += OnApplicationPauseEvent,
                handler => ApplicationLifecycleManager.OnApplicationPauseEvent -= OnApplicationPauseEvent
            );
            #endregion ApplicationLifecycleManager
        }

        #endregion Event Subscriptions

        #region Event Handlers

        private void OnConnectionManagerDidConnectToRoom(Realtime realtime)
        {
            if (!gameObject.activeInHierarchy || !enabled)
            {
                return;
            }

            CreateAvatarIfNeeded();
        }

        private void OnLocalPlayerTeleport(Teleporter teleporter, Vector3 position, Quaternion rotation)
        {
            SyncAvatarToHardwareRig();
        }

        private void OnApplicationDidEnterState(ApplicationState oldsState, ApplicationState newState)
        {
            if (newState == ApplicationState.Calibration)
            {
                SetGlobalAvatarVisibility(false);
            }
        }

        private void OnApplicationDidExitState(ApplicationState oldState, ApplicationState newState)
        {
            if (oldState == ApplicationState.Calibration)
            {
                SetGlobalAvatarVisibility(true);
            }
        }

        #region ApplicationLifecycleManager
        private void OnHMDAcquired(bool isFocused)
        {
            //DebugLog($"OnHMDAcquired - isFocused: {isFocused}");

            if (isFocused)
            {
                OnFocusAcquired();
            }
        }

        private void OnHMDMounted(bool isFocused)
        {
            //DebugLog($"OnHMDMounted - isFocused: {isFocused}");

            if (isFocused)
            {
                OnFocusAcquired();
            }
        }

        private void OnHMDUnmounted()
        {
            //DebugLog("OnHMDUnmounted");

            OnFocusLost();
        }

        private void OnVrFocusAcquired(bool isFocused)
        {
            //DebugLog($"OnVrFocusAcquired - isFocused: {isFocused}");

            if (isFocused)
            {
                OnFocusAcquired();
            }
        }

        private void OnVrFocusLost()
        {
            //DebugLog("OnVrFocusLost");

            OnFocusLost();
        }

        private void OnTrackingLost()
        {
            //DebugLog("OnTrackingLost");

            OnFocusLost();
        }

        private void OnTrackingAcquired(bool isFocused)
        {
            //DebugLog($"OnTrackingAcquired - isFocused: {isFocused}");

            if (isFocused)
            {
                OnFocusAcquired();
            }
        }

        private void OnApplicationFocusEvent(bool isFocused)
        {
            //DebugLog($"OnApplicationFocusEvent - isFocused: {isFocused}");

            OnFocusChanged(isFocused);
        }

        private void OnApplicationPauseEvent(bool hasApplicationFocus, bool isPaused)
        {
            //DebugLog($"OnApplicationPauseEvent - isPaused: {isPaused}, isFocused: {hasApplicationFocus}");

            if (!isPaused && hasApplicationFocus)
            {
                OnFocusAcquired();
            }
            else
            {
                OnFocusLost();
            }
        }
        #endregion ApplicationLifecycleManager

        #endregion Event Handlers

        private void OnDisable()
        {
            DestroyAvatarIfNeeded();
        }

        #region Avatar Registration

        public void RegisterAvatar(int clientID, RealtimeAvatarBase avatar)
        {
            if (Avatars.ContainsKey(clientID))
            {
                DebugLog($"Avatar registered more than once for the same clientID ({clientID}). This is a bug!", DebugLogType.ERROR);
            }
            Avatars[clientID] = avatar;

            //DebugLog($"Registered new Avatar (Client ID: {clientID})");

            var isLocal = clientID == _realtime.clientID;
            if (isLocal)
            {
                ApplicationContext.ShowOculusHands(!IsAvatarVisibiltyEnabled);
            }

            OnAvatarCreated?.Invoke(clientID, avatar, isLocal);
        }

        public void UnregisterAvatar(RealtimeAvatarBase avatar)
        {
            var matchingAvatars = Avatars.Where(keyValuePair => keyValuePair.Value == avatar).ToList();
            foreach (var (avatarClientID, avatarObject) in matchingAvatars)
            {
                Avatars.Remove(avatarClientID);

                //DebugLog($"Unregistered Avatar (Client ID: {avatarClientID})");

                var isLocal = avatarClientID == _realtime.clientID;
                if (isLocal || !_realtime.connected)
                {
                    ApplicationContext.ShowOculusHands(true);
                }

                OnAvatarDestroyed?.Invoke(avatarClientID, avatarObject, isLocal);
            }
        }

        #endregion Avatar Registration

        private void CreateAvatarIfNeeded()
        {
            if (!ConnectionManager.Instance.IsConnected)
            {
                DebugLog("Unable to create avatar. Realtime is not connected to a room.", DebugLogType.ERROR);
                return;
            }

            if (LocalAvatar != null)
            {
                return;
            }

            var localAvatarPrefab = GetLocalAvatarPrefab();
            if (localAvatarPrefab == null)
            {
                DebugLog("Avatars local avatar prefab is null. No avatar prefab will be instantiated for the local player.", DebugLogType.WARNING);
                return;
            }

            GameObject avatarGameObject = Realtime.Instantiate(localAvatarPrefab.name, new Realtime.InstantiateOptions
            {
                ownedByClient = true,
                preventOwnershipTakeover = true,
                destroyWhenOwnerLeaves = true,
                destroyWhenLastClientLeaves = true,
                useInstance = _realtime,
            });

            if (avatarGameObject == null)
            {
                DebugLog("Failed to instantiate Avatar prefab for the local player.", DebugLogType.ERROR);
                return;
            }
            avatarGameObject.name = $"Local Avatar (Client ID: {_realtime.clientID})";

            LocalAvatar = avatarGameObject.GetComponent<RealtimeAvatarBase>();
            if (LocalAvatar == null)
            {
                DebugLog("Avatar doesn't derive from RealtimeAvatarBase as expected.", DebugLogType.ERROR);
            }

            SyncAvatarToHardwareRig();
        }

        private GameObject GetLocalAvatarPrefab()
        {
            return _avatarType == AvatarType.Meta ? localMetaAvatarPrefab : localReadyPlayerMeAvatarPrefab;
        }

        private void DestroyAvatarIfNeeded()
        {
            if (LocalAvatar == null)
            {
                return;
            }

            Realtime.Destroy(LocalAvatar.gameObject);

            LocalAvatar = null;
        }

        private void SyncAvatarToHardwareRig()
        {
            if (LocalAvatar != null && _hardwareRig != null)
            {
                LocalAvatar.GetComponent<RealtimeTransform>().RequestOwnership();
                var rigTransform = _hardwareRig.transform;
                LocalAvatar.transform.position = rigTransform.position;
                LocalAvatar.transform.rotation = rigTransform.rotation;
                //LocalAvatar.transform.parent = rigTransform;
                var localAvatarParentConstraint = LocalAvatar.GetComponent<ParentConstraint>();
                localAvatarParentConstraint.constraintActive = true;
                localAvatarParentConstraint.SetSource(0, new ConstraintSource
                {
                    sourceTransform = rigTransform,
                    weight = 1,
                });
            }
        }

        #region Focus-related

        private void OnFocusAcquired()
        {
            OnFocusChanged(isFocused: true);
        }

        private void OnFocusLost()
        {
            OnFocusChanged(isFocused: false);
        }

        private void OnFocusChanged(bool isFocused)
        {
            //DebugLog($">>>>>>>>>>>>>>> OnFocusChanged - {isFocused}");

            var isLocalAvatarValid = LocalAvatar != null;

            ApplicationContext.ShowOculusHands(!isLocalAvatarValid || !isFocused || !IsAvatarVisibiltyEnabled);

            if (isLocalAvatarValid)
            {
                LocalAvatar.UserStateSync.SetIsPlayerActive(isFocused);
            }
        }

        #endregion Focus-related

        public void ToggleGlobalAvatarVisibility()
        {
            SetGlobalAvatarVisibility(!IsAvatarVisibiltyEnabled);
        }

        private void SetGlobalAvatarVisibility(bool isVisible)
        {
            IsAvatarVisibiltyEnabled = isVisible;

            ApplicationContext.ShowOculusHands(!IsAvatarVisibiltyEnabled);

            OnIsAvatarVisibilityToggled?.Invoke(IsAvatarVisibiltyEnabled);
        }

        [Conditional(DefaultDebugDefine)]
        private void DebugLog(string message, DebugLogType logType = DebugLogType.LOG, Object context = null)
        {
            Log(DebugInfoType.AVATAR_MANAGER, $"[AvatarManager] {message}", logType, context != null ? context : this);
        }
    }
}