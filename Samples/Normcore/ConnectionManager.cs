using System;
using System.Collections.Generic;
using System.Diagnostics;
using Analytics;
using Avatar;
using Context;
using Normal.Realtime;
using UnityEngine;
using static Scripts.Utils.DebugLogUtilities;
using Object = UnityEngine.Object;

namespace Normcore
{
    public class ConnectionManager : MonoBehaviour
    {
        private Realtime _realtime;

        public List<int> Clients { get; private set; } = new List<int>();

        public static event Action<Realtime> OnDidConnectToRoom;
        public static event Action<Realtime> OnDidDisconnectToRoom;

        public delegate void OnClientAddRemove(int clientId, bool isLocal);
        public static event OnClientAddRemove OnAddClient;
        public static event OnClientAddRemove OnRemoveClient;

        public bool IsConnected => _realtime != null && _realtime.connected;

        public static ConnectionManager Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null)
            {
                DebugLog("Instance already exists.", DebugLogType.ERROR);
                DestroyImmediate(this.gameObject);
            }
            Instance = this;

            _realtime = GetComponentInParent<Realtime>();
            _realtime.didConnectToRoom += RealtimeOnDidConnectToRoom;
            _realtime.didDisconnectFromRoom += RealtimeOnDidDisconnectToRoom;

            AvatarManager.OnAvatarCreated += OnAvatarManagerAvatarCreated;
            AvatarManager.OnAvatarDestroyed += OnAvatarManagerAvatarDestroyed;
        }

        private void OnDestroy()
        {
            _realtime.didConnectToRoom -= RealtimeOnDidConnectToRoom;
            _realtime.didDisconnectFromRoom -= RealtimeOnDidDisconnectToRoom;

            AvatarManager.OnAvatarCreated -= OnAvatarManagerAvatarCreated;
            AvatarManager.OnAvatarDestroyed -= OnAvatarManagerAvatarDestroyed;
        }

        private void RealtimeOnDidConnectToRoom(Realtime realtime)
        {
            OnDidConnectToRoom?.Invoke(realtime);

            DebugLog("Connected to Room.");
        }

        private void RealtimeOnDidDisconnectToRoom(Realtime realtime)
        {
            OnDidDisconnectToRoom?.Invoke(realtime);

            DebugLog("Disconnected from Room.");
        }

        private void OnAvatarManagerAvatarCreated(int clientId, RealtimeAvatarBase avatar, bool isLocalAvatar)
        {
            if (Clients.Contains(clientId))
            {
                DebugLog($"Client ID already exists (Client ID: {clientId}).", DebugLogType.ERROR);
                return;
            }

            Clients.Add(clientId);

            if (Clients.Count > Analytics.PeakPlayers)
            {
                // increase peak player count if needed
                // note: we never decrease this value, by design!
                Analytics.PeakPlayers = Clients.Count;
            }

            OnAddClient?.Invoke(clientId, isLocalAvatar);
            DebugLog($"Added Client ID: {clientId}");
            if (!isLocalAvatar)
            {
                var remoteAvatarComponent = avatar.UserStateSync;
                ApplicationContext.AddOrShowToastMessage(string.Format(ApplicationMessages.NewUserJoined, remoteAvatarComponent.AvatarName), remoteAvatarComponent.AvatarName, 5f);
                if (!remoteAvatarComponent.IsUserActive)
                {
                    avatar.SetAvatarActiveWithPauseTimer(false);
                }
            }
        }

        private void OnAvatarManagerAvatarDestroyed(int clientId, RealtimeAvatarBase avatar, bool isLocalAvatar)
        {
            Clients.Remove(clientId);

            OnRemoveClient?.Invoke(clientId, isLocalAvatar);
            DebugLog($"Removed Client ID: {clientId}");
            if (!isLocalAvatar && _realtime.room.connected)
            {
                var remoteAvatarComponent = avatar.UserStateSync;
                ApplicationContext.AddOrShowToastMessage(string.Format(ApplicationMessages.UserHasLeft, remoteAvatarComponent.AvatarName), remoteAvatarComponent.AvatarName, 3f);
            }
        }

        [Conditional(DefaultDebugDefine)]
        private void DebugLog(string message, DebugLogType logType = DebugLogType.LOG, Object context = null)
        {
            Log(DebugInfoType.CONNECTION_MANAGER, $"[ConnectionManager] {message}", logType, context != null ? context : this);
        }
    }
}