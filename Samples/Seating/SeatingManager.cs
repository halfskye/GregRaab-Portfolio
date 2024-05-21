using System;
using System.Diagnostics;
using System.Linq;
using Context;
using Lifecycle;
using Locomotion;
using Normcore;
using States;
using Tracking;
using Normal.Realtime;
using Normal.Realtime.Serialization;
using UnityEngine;
using static Scripts.Utils.DebugLogUtilities;
using Object = UnityEngine.Object;

namespace Seating
{
    public class SeatingManager : RealtimeComponent<SeatingManagerModel>
    {
        private const int INVALID_CLIENT_ID = -1;

        [SerializeField] private Transform primarySeats = null;
        [SerializeField] private Transform spectatorSeats = null;

        [SerializeField] private Seat temporaryStartSeat = null;

        private Seat[] _primarySeats = null;
        private Seat[] _spectatorSeats = null;

        private Seat _localPlayerSeat = null;

        private static bool _hasHardwareCalibrated = false;
        
        public static bool ShouldAnchorToTactileHardware => _hasHardwareCalibrated;
        
        public static Action<Seat> OnLocalPlayerSeatAssigned = null;

        private Vector3 hardwarePosition;

        private void Awake()
        {
            _primarySeats = primarySeats.GetComponentsInChildren<Seat>();
            DebugPrintSeatNames(_primarySeats, "Primary");
            _spectatorSeats = spectatorSeats.GetComponentsInChildren<Seat>();
            DebugPrintSeatNames(_spectatorSeats, "Spectator");
            
            ConnectionManager.OnDidConnectToRoom += OnConnectionManagerDidConnectToRoom;
            ConnectionManager.OnRemoveClient += OnConnectionManagerRemoveClient;
            ApplicationContext.OnDidEnterState += OnApplicationDidEnterState;
            ApplicationContext.OnDidExitState += OnApplicationDidExitState;
            
            ApplicationLifecycleManager.OnHMDMounted += OnHMDMounted;
            ApplicationLifecycleManager.OnVrFocusAcquired += OnVrFocusAcquired;
            ApplicationLifecycleManager.OnHMDAcquired += OnHMDAcquired;
            ApplicationLifecycleManager.OnTrackingAcquired += OnTrackingAcquired;
        }

        private void OnDestroy()
        {
            ConnectionManager.OnDidConnectToRoom -= OnConnectionManagerDidConnectToRoom;
            ConnectionManager.OnRemoveClient -= OnConnectionManagerRemoveClient;
            ApplicationContext.OnDidEnterState -= OnApplicationDidEnterState;
            ApplicationContext.OnDidExitState -= OnApplicationDidExitState;
            
            ApplicationLifecycleManager.OnHMDMounted -= OnHMDMounted;
            ApplicationLifecycleManager.OnVrFocusAcquired -= OnVrFocusAcquired;
            ApplicationLifecycleManager.OnHMDAcquired -= OnHMDAcquired;
            ApplicationLifecycleManager.OnTrackingAcquired -= OnTrackingAcquired;
        }

        private void Start()
        {
            AssignLocalPlayerSeat(temporaryStartSeat);
        }

        private void OnApplicationDidEnterState(ApplicationState oldState, ApplicationState newState)
        {
            if (newState == ApplicationState.Calibration)
            {
                hardwarePosition = TactileCore.HardwareMarkerCenter.transform.position;
            }
        }

        private void OnApplicationDidExitState(ApplicationState oldState, ApplicationState newState)
        {
            if (oldState == ApplicationState.Calibration)
            {
                if (hardwarePosition.Equals(TactileCore.HardwareMarkerCenter.transform.position)) return;
                
                _hasHardwareCalibrated = true;
                TryReseatLocalPlayerSeat();
            }
        }

        private void TryReseatLocalPlayerSeat()
        {
            if (!_localPlayerSeat.IsNullOrDestroyed())
            {
                AssignLocalPlayerSeat(_localPlayerSeat);
            }
        }

        private void OnConnectionManagerDidConnectToRoom(Realtime rt)
        {
            AssignLocalPlayerSeat();
        }

        private void OnConnectionManagerRemoveClient(int clientId, bool isLocal)
        {
            ReleaseSeatsByClientID(clientId);
            // ReleaseStaleSeats();
        }

        private void OnHMDMounted(bool hasFocus)
        {
            TryReseatLocalPlayerSeat();
        }

        private void OnVrFocusAcquired(bool hasApplicationFocus)
        {
            TryReseatLocalPlayerSeat();
        }

        private void OnHMDAcquired(bool hasApplicationFocus)
        {
            TryReseatLocalPlayerSeat();
        }

        private void OnTrackingAcquired(bool hasApplicationFocus)
        {
            TryReseatLocalPlayerSeat();
        }

        private void OnDisable()
        {
            ReleaseLocalPlayerSeat();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CycleLocalPlayerToNextSeat();
            }
        }

        protected override void OnRealtimeModelReplaced(SeatingManagerModel previousModel, SeatingManagerModel currentModel)
        {
            base.OnRealtimeModelReplaced(previousModel, currentModel);

            if (currentModel != null)
            {
                // if (currentModel.isFreshModel)
                // {
                //     // Set initial values here
                //     // currentModel.primarySeatData = Enumerable.Repeat(INVALID_CLIENT_ID, SeatingManagerModel.MAX_PRIMARY_SEATS).ToArray();
                //     // currentModel.spectatorSeatData = Enumerable.Repeat(INVALID_CLIENT_ID, SeatingManagerModel.MAX_SPECTATOR_SEATS).ToArray();
                // }

                // Subscribe to events

                currentModel.primarySeatData.modelAdded += OnPrimarySeatDataAdded;
                currentModel.primarySeatData.modelRemoved += OnPrimarySeatDataRemoved;
                currentModel.primarySeatData.modelReplaced += OnPrimarySeatDataReplaced;

                currentModel.spectatorSeatData.modelAdded += OnSpectatorSeatDataAdded;
                currentModel.spectatorSeatData.modelRemoved += OnSpectatorSeatDataRemoved;
                currentModel.spectatorSeatData.modelReplaced += OnSpectatorSeatDataReplaced;
            }

            if (previousModel != null)
            {
                // Unsubscribe from events

                previousModel.primarySeatData.modelAdded -= OnPrimarySeatDataAdded;
                previousModel.primarySeatData.modelRemoved -= OnPrimarySeatDataRemoved;
                previousModel.primarySeatData.modelReplaced -= OnPrimarySeatDataReplaced;

                previousModel.spectatorSeatData.modelAdded -= OnSpectatorSeatDataAdded;
                previousModel.spectatorSeatData.modelRemoved -= OnSpectatorSeatDataRemoved;
                previousModel.spectatorSeatData.modelReplaced -= OnSpectatorSeatDataReplaced;
            }
        }

        #region Primary Seat Events

        private void OnPrimarySeatDataAdded(RealtimeDictionary<SeatModel> dictionary, uint key, SeatModel seatModel, bool remote)
        {
            if (seatModel.clientId == realtime.clientID)
            {
                AssignLocalPlayerSeat(_primarySeats[key]);
            }
        }

        private void OnPrimarySeatDataRemoved(RealtimeDictionary<SeatModel> dictionary, uint key, SeatModel seatModel, bool remote)
        {
        }

        private void OnPrimarySeatDataReplaced(RealtimeDictionary<SeatModel> dictionary, uint key, SeatModel oldmodel, SeatModel newmodel, bool remote)
        {
        }

        #endregion Primary Seat Events

        #region Spectator Seat Events

        private void OnSpectatorSeatDataAdded(RealtimeDictionary<SeatModel> dictionary, uint key, SeatModel seatModel, bool remote)
        {
            if (seatModel.clientId == realtime.clientID)
            {
                AssignLocalPlayerSeat(_spectatorSeats[key]);
            }
        }

        private void OnSpectatorSeatDataRemoved(RealtimeDictionary<SeatModel> dictionary, uint key, SeatModel seatModel, bool remote)
        {
        }

        private void OnSpectatorSeatDataReplaced(RealtimeDictionary<SeatModel> dictionary, uint key, SeatModel oldmodel, SeatModel newmodel, bool remote)
        {
        }

        #endregion Spectator Seat Events

        private void AssignLocalPlayerSeat(Seat seat)
        {
            _localPlayerSeat = seat;

            DebugLog($"Assigned Local Player Seat '{seat.name}' - Client ID: {realtime.clientID}", context: _localPlayerSeat);

            Teleporter.Instance.Teleport(_localPlayerSeat.ActiveSeatLocation, ShouldAnchorToTactileHardware);

            OnLocalPlayerSeatAssigned?.Invoke(_localPlayerSeat);
        }

        private void AssignLocalPlayerSeat()
        {
            if (!TryCycleLocalPlayerToNextPrimarySeat())
            {
                if (!TryCycleLocalPlayerToNextSpectatorSeat())
                {
                    DebugLog("Couldn't assign Seat to Local Player", DebugLogType.ERROR);
                }
            }
        }

        private delegate bool GetSeatIndex(out uint startingIndex);
        private delegate bool RemoveSeatByIndex(uint seatIndex);
        private delegate bool TryAssignLocalPlayerSeatByIndex(uint seatIndex);
        private bool TryCycleLocalPlayerToNextSeat(GetSeatIndex getSeatIndex, RemoveSeatByIndex removeSeatByIndex, uint seatCount, TryAssignLocalPlayerSeatByIndex tryAssignLocalPlayerSeatByIndex)
        {
            if (!getSeatIndex(out var startingIndex))
            {
                startingIndex = 0;
            }
            else
            {
                removeSeatByIndex(startingIndex);
                startingIndex = (startingIndex + 1) % seatCount;
            }

            if (model == null) return false;
            if (startingIndex >= seatCount) return false;
            
            // Starting Index to End
            for (uint seatIndex = startingIndex; seatIndex < seatCount; ++seatIndex)
            {
                var seatAssigned = tryAssignLocalPlayerSeatByIndex(seatIndex);
                if (seatAssigned)
                {
                    return true;
                }
            }
            
            // 0 to Starting Index
            for (uint seatIndex = 0; seatIndex < startingIndex; ++seatIndex)
            {
                var seatAssigned = tryAssignLocalPlayerSeatByIndex(seatIndex);
                if (seatAssigned)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryCycleLocalPlayerToNextPrimarySeat()
        {
            return TryCycleLocalPlayerToNextSeat(
                TryGetLocalPlayerPrimarySeatIndex,
                model.primarySeatData.Remove,
                (uint) _primarySeats.Length,
                TryAssignLocalPlayerPrimarySeatByIndex
            );
        }

        // private delegate bool TryGetSeatData(uint seatIndex, out SeatModel seatModel);
        // private delegate void AssignSeatDataByIndex(uint seatIndex, SeatModel seatModel);
        // private delegate void AddSeatDataByIndex(uint seatIndex, SeatModel seatModel);
        // private bool TryAssignLocalPlayerSeatDataByIndex(uint seatIndex, TryGetSeatData tryGetSeatData, AssignSeatDataByIndex assignSeatDataByIndex, AddSeatDataByIndex addSeatDataByIndex)
        // {
        //     if (tryGetSeatData(seatIndex, out SeatModel seatModel)
        //         && seatModel.clientId != realtime.clientID
        //         && seatModel.clientId != INVALID_CLIENT_ID) return false;
        //     
        //     var newSeatModel = new SeatModel() { clientId = realtime.clientID };
        //     if (seatModel != null)
        //     {
        //         assignSeatDataByIndex(seatIndex, newSeatModel);
        //     }
        //     else
        //     {
        //         addSeatDataByIndex(seatIndex, newSeatModel);
        //     }
        //
        //     return true;
        // }
        
        private bool TryAssignLocalPlayerPrimarySeatByIndex(uint seatIndex)
        {
            if (model.primarySeatData.TryGetValue(seatIndex, out SeatModel seatModel)
                && seatModel.clientId != realtime.clientID
                && seatModel.clientId != INVALID_CLIENT_ID) return false;
            
            var newSeatModel = new SeatModel() { clientId = realtime.clientID };
            if (seatModel != null)
            {
                model.primarySeatData[seatIndex] = newSeatModel;

                //@NOTE: Special case of reassigning Client ID.
                if (seatModel.clientId == realtime.clientID)
                {
                    AssignLocalPlayerSeat(_primarySeats[seatIndex]);
                }
            }
            else
            {
                model.primarySeatData.Add(seatIndex, newSeatModel);
            }

            DebugLog($"Primary Seat Assigned to Local Player : {seatIndex}");
            return true;
        }

        private bool TryCycleLocalPlayerToNextSpectatorSeat()
        {
            return TryCycleLocalPlayerToNextSeat(
                TryGetLocalPlayerSpectatorSeatIndex,
                model.spectatorSeatData.Remove,
                (uint) _spectatorSeats.Length,
                TryAssignLocalPlayerSpectatorSeatByIndex
            );
        }

        private bool TryAssignLocalPlayerSpectatorSeatByIndex(uint seatIndex)
        {
            if (model.spectatorSeatData.TryGetValue(seatIndex, out SeatModel seatModel)
                && seatModel.clientId != realtime.clientID
                && seatModel.clientId != INVALID_CLIENT_ID) return false;
            
            var newSeatModel = new SeatModel() { clientId = realtime.clientID };
            if (seatModel != null)
            {
                model.spectatorSeatData[seatIndex] = newSeatModel;

                //@NOTE: Special case of reassigning Client ID.
                if (seatModel.clientId == realtime.clientID)
                {
                    AssignLocalPlayerSeat(_spectatorSeats[seatIndex]);
                }
            }
            else
            {
                model.spectatorSeatData.Add(seatIndex, newSeatModel);
            }

            DebugLog($"Spectator Seat Assigned to Local Player : {seatIndex}");
            return true;
        }

        private bool CycleLocalPlayerToNextSeat()
        {
            var success = TryCycleLocalPlayerToNextPrimarySeat();
            if (!success)
            {
                success = TryCycleLocalPlayerToNextSpectatorSeat();
            }

            if (success)
            {
                DebugLog("Cycled Local Player to Next Seat", context: _localPlayerSeat);
            }
            else
            {
                DebugLog("Failed to cycle Local Player to next seat.", DebugLogType.WARNING);
            }

            return success;
        }

        private bool TryGetLocalPlayerPrimarySeatIndex(out uint seatIndex)
        {
            if (model != null && model.primarySeatData.Any(x => x.Value.clientId == realtime.clientID))
            {
                seatIndex = model.primarySeatData.First(x => x.Value.clientId == realtime.clientID).Key;
                return true;
            }

            seatIndex = uint.MaxValue;
            return false;
        }
        
        private bool TryGetLocalPlayerSpectatorSeatIndex(out uint seatIndex)
        {
            if (model != null && model.spectatorSeatData.Any(x => x.Value.clientId == realtime.clientID))
            {
                seatIndex = model.spectatorSeatData.First(x => x.Value.clientId == realtime.clientID).Key;
                return true;
            }

            seatIndex = uint.MaxValue;
            return false;
        }

        private void ReleaseLocalPlayerSeat()
        {
            ReleaseSeatsByClientID(realtime.clientID);
        }

        private void ReleaseSeatsByClientID(int clientId)
        {
            model.primarySeatData.Where(x => x.Value.clientId == clientId)
                .AsParallel().ForAll(x => model.primarySeatData.Remove(x.Key));

            model.spectatorSeatData.Where(x => x.Value.clientId == clientId)
                .AsParallel().ForAll(x => model.spectatorSeatData.Remove(x.Key));
            
            DebugLog($"Removed Seats assigned to Client ID: {clientId}");
        }
        
        private void ReleaseStaleSeats()
        {
            // Primary Seats
            (
                from seatData in model.primarySeatData
                where ConnectionManager.Instance.Clients.All(x => x != seatData.Value.clientId)
                select seatData.Key
            ).AsParallel().ForAll(x => model.primarySeatData.Remove(x));
            
            // Spectator Seats
            (
                from seatData in model.spectatorSeatData
                where ConnectionManager.Instance.Clients.All(x => x != seatData.Value.clientId)
                select seatData.Key
            ).AsParallel().ForAll(x => model.spectatorSeatData.Remove(x));
        }

        #region Debug
        
        [Conditional(DefaultDebugDefine)]
        private void DebugLog(string message, DebugLogType logType = DebugLogType.LOG, Object context = null)
        {
            Log(DebugInfoType.SEATING, $"[SeatingManager] {message}", logType, context != null ? context : this);
        }

        private void DebugPrintSeatNames(Seat[] seats, string label)
        {
            foreach (var seat in seats)
            {
                DebugLog($"{label} Seat name: '{seat.name}'", context: seat);
            }
        }

        #endregion Debug
    }
}
