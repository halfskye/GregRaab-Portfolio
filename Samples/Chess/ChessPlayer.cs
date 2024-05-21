using System;
using System.Collections.Generic;
using System.Linq;
using Analytics;
using Environments;
using Fusion;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// The ChessPlayer class represent the player's understanding of the ChessBoard & ChessPieces state.
    /// </summary>
    public class ChessPlayer : NetworkBehaviour
    {
        [Networked] public NetworkObject ChessBoardNetworkObject { get; set; } = null;

        private ChessBoard _chessBoard = null;
        private ChessBoard ChessBoard
        {
            get
            {
                if (_chessBoard == null)
                {
                    _chessBoard = ChessBoardNetworkObject.GetComponent<ChessBoard>();
                }

                return _chessBoard;
            }
        }

        private bool IsChessBoardValid()
        {
            return isActiveAndEnabled &&
                   !Runner.IsShutdown &&
                   ChessBoardNetworkObject != null &&
                   ChessBoardNetworkObject.IsValid &&
                   _chessBoard != null;
        }

        // [Networked] public ChessBoard NetworkedChessBoard { get; set; } = null;

        [Serializable]
        public struct ChessPiecePositionData : INetworkStruct
        {
            //@NOTE: Bools apparently aren't supported since they are different sizes on different systems.
            //		So we are using flags, which are more robust anyway for future additions.
            public const uint BROADCAST = 1 << 0;

            public Vector3 Position;
            public uint Flags;

            private bool IsFlagSet(uint flag)
            {
                return (Flags & flag) == flag;
            }

            public bool IsBroadcast()
            {
                return IsFlagSet(BROADCAST);
            }
        }

        // Networked ChessPiece position data with networked OnChanged event handler.
        [Networked(OnChanged = nameof(OnChessPiecePositionDataChanged)), Capacity(ChessBoard.ALL_PIECES_COUNT)]
        private NetworkArray<ChessPiecePositionData> ChessPieceNetworkedPositions { get; }

        private readonly HashSet<PlayerRef> _otherPlayers = new HashSet<PlayerRef>();

        // Timestamps for last local edits by ChessPiece Index.
        private readonly float[] _localEditTimestamps = Enumerable.Repeat(float.MinValue, ChessBoard.ALL_PIECES_COUNT).ToArray();
        private float GetLastEditTimestamp() { return _localEditTimestamps.Max(); }

        private const float INVALID_TIMESTAMP = float.MaxValue;
        private float _startPlayTimestamp = INVALID_TIMESTAMP;
        private bool IsStartPlayTimestampValid() { return !Mathf.Approximately(_startPlayTimestamp, INVALID_TIMESTAMP); }

        private const string ANALYTICS_ACTIVITY_CHESS_EVENT = "activity_chess";
        private const string ANALYTICS_ACTIVITY_CHESS_PLAY_EVENT = "activity_chess_play";

        // Queued ChessPiece final destinations (for ChessPieces in Transition/Lerping)
        private readonly Dictionary<int, Vector3> _queuedPieceDestinations = new ();
        private const float LERP_SPEED = 4f;
        private const float LERP_DISTANCE = 0.0001f;
        private const float LERP_DISTANCE_SQ = LERP_DISTANCE * LERP_DISTANCE;

        private void Awake()
        {
            TableManager.OnFirstSeatAssigned += TableManager_OnFirstSeatAssigned;
            TableManager.OnSeatChanged += TableManager_OnSeatChanged;
        }

        private void OnDestroy()
        {
            TableManager.OnFirstSeatAssigned -= TableManager_OnFirstSeatAssigned;
            TableManager.OnSeatChanged -= TableManager_OnSeatChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            SetAllQueuedPiecesToDestinations();

            HandlePossibleAnalyticsActivityChessPlay();

            base.Despawned(runner, hasState);
        }

        private void Update()
        {
            if (IsChessBoardValid())
            {
                UpdateQueuedPiecesInTransition();
            }
        }

        #region Chess Board Reference

        public void HandleNewChessBoard(ChessBoard chessBoard)
        {
            if (ChessBoardManager.Instance.IsLocalPlayerChessBoard(chessBoard))
            {
                HandleAnalyticsActivityChess();
            }
        }

        public void HandleRemovedChessBoard()
        {
            HandlePossibleAnalyticsActivityChessPlay();
        }

        #endregion Chess Board Reference

        #region Set Chess Piece Position

        public void SetChessPiecePosition(int index, Vector3 position, bool updateTimestamp = true)
        {
            ChessPieceNetworkedPositions.Set(index, new ChessPiecePositionData() { Position = position, Flags = updateTimestamp ? ChessPiecePositionData.BROADCAST : 0 });

            ChessBoard.SyncFromPlayerData(index, ChessPieceNetworkedPositions[index]);

            if (updateTimestamp)
            {
                RecordLocalEditTimestampAtIndex(index, Time.time);
            }
        }

        public void LerpChessPieceToPosition(int index, Vector3 position, bool updateTimestamp = true)
        {
            AddQueuedPieceDestination(index, position);

            if (updateTimestamp)
            {
                RecordLocalEditTimestampAtIndex(index, Time.time);
            }
        }

        private void RecordLocalEditTimestampAtIndex(int chessPieceIndex, float timestamp)
        {
            _localEditTimestamps[chessPieceIndex] = timestamp;

            //@NOTE: Backup in case start time didn't get set somehow before. Might be unnecessary.
            if (!IsStartPlayTimestampValid())
            {
                _startPlayTimestamp = timestamp;
            }
        }

        #endregion Set Chess Piece Position

        #region Queued Chess Piece Destinations

        private void AddQueuedPieceDestination(int chessPieceIndex, Vector3 destination)
        {
            if (!_queuedPieceDestinations.TryAdd(chessPieceIndex, destination))
            {
                _queuedPieceDestinations[chessPieceIndex] = destination;
            }
        }

        private void UpdateQueuedPiecesInTransition()
        {
            var piecesAtDestination = new List<int>();

            // Go through Queued ChessPiece Final Destinations and update Lerp
            foreach (var (index, destination) in _queuedPieceDestinations)
            {
                var position = ChessBoard.ActiveChessPieces[index].transform.position;
                var newPosition = Vector3.Lerp(position, destination, Time.deltaTime * LERP_SPEED);

                if ((newPosition - destination).sqrMagnitude <= LERP_DISTANCE_SQ)
                {
                    newPosition = destination;
                    piecesAtDestination.Add(index);
                }

                SetChessPiecePosition(index, newPosition, updateTimestamp: true);
            }

            piecesAtDestination.ForEach(RemoveQueuedPieceDestinationByIndex);
        }

        private void SetAllQueuedPiecesToDestinations()
        {
            if (IsChessBoardValid())
            {
                foreach (var (index, destination) in _queuedPieceDestinations)
                {
                    SetChessPiecePosition(index, destination, updateTimestamp: true);
                }
            }

            ClearQueuedPieceDestinations();
        }

        private void ClearQueuedPieceDestinations()
        {
            _queuedPieceDestinations.Clear();
        }

        private void RemoveQueuedPieceDestinationByIndex(int chessPieceIndex)
        {
            _queuedPieceDestinations.Remove(chessPieceIndex);
        }

        #endregion Queued Chess Piece Destinations

        #region Receive Chess Piece Data

        public void SyncDataFromBoard(ChessPiece[] boardChessPieces)
        {
            ChessBoard.DebugLog("SyncBoardData", Object.InputAuthority);
            ChessBoard.DebugLog("SyncBoardData - Local PlayerRef");

            if (Object.HasInputAuthority)
            {
                // Stop any lerping
                ClearQueuedPieceDestinations();

                for (var i = 0; i < ChessPieceNetworkedPositions.Length; ++i)
                {
                    var before = ChessPieceNetworkedPositions[i].Position;
                    var after = boardChessPieces[i].transform.position;
                    var text = $"BEFORE : {before} [SyncBoardData]\n" +
                               $"                    AFTER  : {after}";

                    if (before != after)
                    {
                        ChessBoard.DebugLog(text, Object.InputAuthority);

                        //@TODO: Revisit: Should we Broadcast here?
                        ChessPieceNetworkedPositions.Set(i, new ChessPiecePositionData() { Position = after, Flags = ChessPiecePositionData.BROADCAST });
                    }
                    else
                    {
                        ChessBoard.DebugLog(text, Object.InputAuthority, useError: false);
                    }
                }
            }
        }

        public static void OnChessPiecePositionDataChanged(Changed<ChessPlayer> changed)
        {
            var changedPlayer = changed.Behaviour;
            var changedPlayerNetworkObject = changedPlayer.Object;
            var inputAuthority = changedPlayerNetworkObject.InputAuthority;
            ChessBoard.DebugLog("[ChessPlayer] OnChessPiecePositionDataChanged", inputAuthority);

            // Only receive data from Players that aren't us:
            if (!changedPlayerNetworkObject.HasInputAuthority)
            {
                // Load Old and New states to compare exact relevant delta data:
                changed.LoadOld();
                var oldValue = changedPlayer.ChessPieceNetworkedPositions;
                changed.LoadNew();
                var newValue = changedPlayer.ChessPieceNetworkedPositions;

                ChessBoard.DebugLog("DELTAS!", inputAuthority);
                var chessPieceMoveDeltas = new Dictionary<int, Vector3>();
                for (var i = 0; i < newValue.Length; i++)
                {
                    // Only consume deltas if they're set to Broadcast:
                    if (newValue[i].IsBroadcast() &&
                        newValue[i].Position != oldValue[i].Position)
                    {
                        chessPieceMoveDeltas.Add(i, newValue[i].Position);

                        ChessBoard.DebugLog($"Delta ADDED - Before: {oldValue[i].Position}, After: {newValue[i].Position}", inputAuthority);
                    }
                }

                changedPlayer.ReceiveChangedPositionData(chessPieceMoveDeltas);
            }
        }

        private void ReceiveChangedPositionData(Dictionary<int, Vector3> chessPieceMoveDeltas)
        {
            var objectInputAuthority = Object.InputAuthority;
            ChessBoard.DebugLog("[ChessPlayer] SendDataToBoard", objectInputAuthority);
            ChessBoard.DebugLog("[ChessPlayer] SendDataToBoard - Local PlayerRef");

            _otherPlayers.Add(objectInputAuthority);

            // var isHigherPriority = IsPlayerPriorityHigherThanOrEqualToLocal(objectInputAuthority);
            // ChessBoard.DebugLog($"Is Player [{objectInputAuthority}] Higher Priority? {isHigherPriority}");

            foreach (var (chessPieceIndex, theirs) in chessPieceMoveDeltas)
            {
                var localPlayer = ChessPlayerManager.Instance.GetLocalPlayerByBoard(ChessBoard);
                var ours = localPlayer.ChessPieceNetworkedPositions[chessPieceIndex].Position;

                if (!ours.Approximately(theirs))
                {
                    var text = $"OURS : {ours} [SyncBoardData]\n" +
                               $"                    THEIRS  : {theirs}";
                    ChessBoard.DebugLog($"[ChessPlayer] SendDataToBoard - Add Delta: {text}");
                    // ChessBoard.DebugLog($"Is Move Ready? {local.IsMoveReady(chessPieceIndex)}");

                    // Always accept higher priority data
                    // OR
                    // Data for an Index we haven't moved recently AND hasn't been moved recently by this other Player (@TODO: experiment w/ this)
                    // if (isHigherPriority ||
                    //     (CompareNowToLastMoveTimestamp(local, chessPieceIndex, 0.5f)))// && CompareNowToLastMoveTimestamp(this, chessPieceIndex, 0.1f)))
                    {
                        RemoveQueuedPieceDestinationByIndex(chessPieceIndex);

                        localPlayer.SetChessPiecePosition(chessPieceIndex, theirs, updateTimestamp: false); //!isHigherPriority); //isHigherPriority);
                    }
                }
            }
        }

        #endregion Receive Chess Piece Data

        #region Seat Events

        private void OnSitDown()
        {
            if (ChessBoardManager.Instance.IsLocalPlayerAtChessBoardByChessPlayer(this))
            {
                HandleAnalyticsActivityChess();
                _startPlayTimestamp = Time.time;
                _otherPlayers.Clear();
            }
        }

        private void TableManager_OnFirstSeatAssigned(int tableIndex,int seatIndex)
        {
            OnSitDown();
        }

        private void TableManager_OnSeatChanged(int tableIndex,int seatIndex)
        {
            // Handle possible previous play
            HandlePossibleAnalyticsActivityChessPlay();

            OnSitDown();
        }

        #endregion Seat Events

        #region Analytics

        private static void HandleAnalyticsActivityChess()
        {
            AmplitudeAnalytics.SendEvent(ANALYTICS_ACTIVITY_CHESS_EVENT);
        }

        private void HandlePossibleAnalyticsActivityChessPlay()
        {
            if (!IsStartPlayTimestampValid())
            {
                return;
            }

            var lastEditTimeStamp = GetLastEditTimestamp();
            if (lastEditTimeStamp <= _startPlayTimestamp)
            {
                return;
            }

            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                {"duration", lastEditTimeStamp - _startPlayTimestamp},
                {"peakPlayers", _otherPlayers.Count + 1} //@NOTE: Records other players who have interacted with the ChessBoard plus us.
            };

            AmplitudeAnalytics.SendEventWithData(ANALYTICS_ACTIVITY_CHESS_PLAY_EVENT, dict);

            // Reset, in case we play here again.
            _startPlayTimestamp = INVALID_TIMESTAMP;
            _otherPlayers.Clear();
        }

        #endregion Analytics

        //@TODO: Need to be pared down after experimentation:
        #region Move Validation Utilities

        // private bool IsLastLocalMoveTimestampPastThreshold(int index, float threshold)
        // {
        // 	return (Time.time - localEditTimestamps[index]) > threshold;
        // }

        // private bool IsMoveReady(int index)
        // {
        // 	// float lastLocalEditThreshold = Runner.DeltaTime * 15f;
        // 	const float LAST_LOCAL_EDIT_THRESHOLD = 0.5f;
        // 	return IsLastLocalMoveTimestampPastThreshold(index, LAST_LOCAL_EDIT_THRESHOLD);
        // }

        // private bool IsPlayerPriorityHigherThanOrEqualToLocal(PlayerRef playerRef)
        // {
        // 	return (playerRef <= Runner.LocalPlayer);
        // }

        #endregion Move Validation Utilities
    }
}
