using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emerge.Home;
using Fusion;
using UnityEngine;

namespace Emerge.Chess
{
    public class ChessBoard : NetworkBehaviour
    {
        private const int PLAYER_PIECES_COUNT = ChessPieceSO.GRID_SIZE * 2;
        public const int ALL_PIECES_COUNT = PLAYER_PIECES_COUNT * 2;

        public enum PlayerColors
        {
            Black = 0,
            White = 1,
        }

        //@NOTE: Piece values might not be used directly in code, but indicates index order elsewhere (Editor, etc):
        public enum PiecesEnum
        {
            King,
            Queen,
            Rook,
            Bishop,
            Knight,
            Pawn,
            Count
        }

        [Header("General Settings")]
        [SerializeField] private ChessBoardSettingsSO chessBoardSettings = null;
        public ChessBoardSettingsSO Settings => chessBoardSettings;

        [Header("Pieces")]
        [SerializeField] private List<ChessPiece> chessPiecePrefabs = null;

        [Header("Board")]
        [SerializeField] private BoardSizeSettings boardSizeSettings;

        [Header("Gutters")]
        [SerializeField] private ChessGutter[] gutters = null;
        public ChessGutter GetGutterByColor(PlayerColors color) { return gutters[(int)color]; }

        [Header("Gizmo Setting")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField, Range(0.01f, 1.0f)] private float gizmoScale = 0.6f;

        public ChessPiece[] ActiveChessPieces { get; } = Enumerable.Repeat((ChessPiece)null, ALL_PIECES_COUNT).ToArray();

        [Serializable]
        private struct BoardSizeSettings
        {
            [SerializeField] private float heightOffset;
            public float HeightOffset => heightOffset;

            [SerializeField, Range(0.01f, 0.2f)] private float scale;
            public float Scale => scale;

            public float HorizontalOffset => (-Scale * ChessPieceSO.GRID_SIZE / 2) + (Scale / 2);

            public BoardSizeSettings(float heightOffset, float scale)
            {
                this.heightOffset = heightOffset;
                this.scale = scale;
            }
        }

        #region Spawning - Board & Pieces

        public override void Spawned()
        {
            base.Spawned();

            CreateLocalChessPlayer();

            SpawnChessPieces();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ChessBoardManager.Instance.HandleRemovedChessBoardObject(this);
            ChessPlayerManager.Instance.HandleRemovedChessBoard(this);

            base.Despawned(runner, hasState);
        }

        private void CreateLocalChessPlayer()
        {
            ChessPlayerManager.Instance.HandleNewChessBoard(this);
        }

        private void SpawnChessPieces()
        {
            SpawnBlackChessPieces();

            SpawnWhiteChessPieces();

            // DebugLog("All Chess Pieces Spawned (in order):\n");
            // foreach (var activeChessPiece in activeChessPieces)
            // {
            //     DebugLog($"{GetChessPieceDebugText(activeChessPiece)}\n");
            // }

            SyncDataToPlayer();
        }

        private void SpawnBlackChessPieces()
        {
            int numberOfPieceTypes = (int)PiecesEnum.Count;
            SpawnChessPiecesInRange(firstIndex: 0, lastIndex: numberOfPieceTypes - 1);
        }

        private void SpawnWhiteChessPieces()
        {
            int numberOfPieceTypes = (int)PiecesEnum.Count;
            SpawnChessPiecesInRange(firstIndex: numberOfPieceTypes, lastIndex: numberOfPieceTypes + numberOfPieceTypes - 1);
        }

        private void SpawnChessPiecesInRange(int firstIndex, int lastIndex)
        {
            for (int i = firstIndex; i <= lastIndex; ++i)
            {
                SpawnChessPieceType(chessPiecePrefabs[i]);
            }
        }

        private void SpawnChessPieceType(ChessPiece chessPiecePrefab)
        {
            var pieceData = chessPiecePrefab.PieceData;
            for (int i = 0; i < pieceData.SpawnPositions.Length; ++i)
            {
                var boardPosition = pieceData.SpawnPositions[i];
                Vector3 spawnPos = GetWorldSpawnPosition(boardPosition);
                Quaternion spawnRot = transform.rotation * Quaternion.Euler(0, pieceData.YRotation, 0);

                ChessPiece chessPiece = Instantiate(chessPiecePrefab, spawnPos, spawnRot, this.transform);
                chessPiece.Initialize(this, boardPosition, id: i);

                // Check for previous existence?
                ActiveChessPieces[chessPiece.RelativeIndex] = chessPiece;

                // DebugLog($"{GetChessPieceDebugText(chessPiece)}\n");
            }
        }

        private Vector3 GetWorldSpawnPosition(ChessPieceSO.BoardPosition boardPosition)
        {
            Vector2Int gridPos = boardPosition.GetBoardPositionXY();
            return GridPosToWorldPos(gridPos.x, gridPos.y);
        }

        public async Task Despawn()
        {
            await ChessBoardManager.Instance.DespawnChessBoard(this);
        }

        #endregion // Spawning - Board & Pieces

        public void ResetBoard()
        {
            RPC_ResetChessBoard(this.Id);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_ResetChessBoard(NetworkBehaviourId boardNetworkID)
        {
            if (boardNetworkID != this.Id)
            {
                return;
            }
            ResetToInitialState();
        }
        public void CloseBoardButtonPanels()
        {
            var boardButtonUI = gameObject.GetComponentsInChildren<ChessBoardButtonsUI>();
            for (int i = 0; i < boardButtonUI.Length; i++)
            {
                boardButtonUI[i].HideButtonPanel();
                boardButtonUI[i].isButtonPanelOn = false;
            }
        }
        public void ResetToInitialState()
        {
            foreach (var chessPiece in ActiveChessPieces)
            {
                var grabbableObjectComponent = chessPiece.GetComponent<GrabbableObject>();

                for (int i = 0; i < gutters.Length; i++)
                {
                    gutters[i].ResetGutterSquares();
                }
                if(grabbableObjectComponent.GrabState == GrabbableObject.GrabStates.Grabbed)
                {
                    grabbableObjectComponent.ReleaseActivate();
                }

                grabbableObjectComponent.isResettingChessBoard = true;
                grabbableObjectComponent.ResettingChessBoard();
                chessPiece.releaseDestinationVisual.gameObject.SetActive(false);
                chessPiece.transform.position = GetWorldSpawnPosition(chessPiece.InitialPosition);
            }

            SyncDataToPlayer();
        }

        #region Sync Data

        private void SyncDataToPlayer()
        {
            DebugLog("SyncDataToPlayer");
            var player = ChessPlayerManager.Instance.GetLocalPlayerByBoard(this);
            player.SyncDataFromBoard(ActiveChessPieces);

            // var chessPlayers = FindObjectsOfType<ChessPlayer>();
            // foreach (var chessPlayer in chessPlayers)
            // {
            //     chessPlayer.SyncDataFromBoard(activeChessPieces);
            // }
        }

        public void SyncFromPlayerData(int index, ChessPlayer.ChessPiecePositionData chessPiecePosition)
        {
            var chessPieceTransform = ActiveChessPieces[index].transform;
            var before = chessPieceTransform.position;
            var after = chessPiecePosition.Position;
            var text = $"BEFORE : {before} [SyncPlayerData] [{GetChessPieceDebugText(ActiveChessPieces[index])}]\n" +
                       $"                    AFTER  : {after}";

            if (!before.Approximately(after))
            {
                DebugLog(text);
            }
            else
            {
                DebugLogWarning(text);
            }

            chessPieceTransform.position = chessPiecePosition.Position;
        }

        #endregion Sync Data

        #region Board Grid and Gizmos

        private Vector3 GridPosToWorldPos(int x, int y)
        {
            Vector3 localPos = new Vector3(
                x * boardSizeSettings.Scale + boardSizeSettings.HorizontalOffset,
                boardSizeSettings.HeightOffset,
                y * boardSizeSettings.Scale + boardSizeSettings.HorizontalOffset);

            return transform.TransformPoint(localPos);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos)
            {
                return;
            }

            bool isColorBlack = true;

            // Make Gizmo matrix local so gizmo rotations look correct
            Gizmos.matrix = transform.localToWorldMatrix;

            for (int i = 0; i < ChessPieceSO.GRID_SIZE; i++)
            {
                for (int j = 0; j < ChessPieceSO.GRID_SIZE; j++)
                {
                    // Convert world pos to local pos due to local gizmo matrix
                    Vector3 pos = transform.InverseTransformPoint(GridPosToWorldPos(i, j));

                    // Adjustments for gizmo placement
                    Vector3 scale = Vector3.one * boardSizeSettings.Scale;
                    pos.y += boardSizeSettings.Scale / 2;
                    scale.x *= gizmoScale;
                    scale.z *= gizmoScale;

                    // Swap colors unless we are starting a new row
                    if (j % ChessPieceSO.GRID_SIZE != 0 || i == 0)
                    {
                        Gizmos.color = isColorBlack ? Color.black : Color.white;
                        isColorBlack = !isColorBlack;
                    }

                    Gizmos.DrawWireCube(pos, scale);
                }
            }
        }

        #endregion

        #region RPCs

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        internal static void RPC_RequestSpawnChessBoard(NetworkRunner runner, int gameSpawningPointIndex)
        {
            //@NOTE RpcTargets.StateAuthority doesn't seem to be working? Maybe not for static RPCs? So having to do this manual check.
            if (runner.IsSharedModeMasterClient)
            {
                var gameSpawningPoint = ChessBoardManager.GetPossibleGameSpawnPointByIndex(gameSpawningPointIndex);
                if (gameSpawningPoint != null)
                {
                    if (!ChessBoardManager.Instance
                            .DoesGameSpawnPointHaveChessBoardOrPending(gameSpawningPointIndex))
                    {
                        ChessBoardManager.Instance.SpawnChessBoardAtGameSpawnPoint(gameSpawningPoint);
                    }
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        internal static void RPC_SpawnedChessBoard(NetworkRunner runner, NetworkObject spawnedChessBoard, int gameSpawningPointIndex)
        {
            ChessBoardManager.Instance.HandleNewChessBoard(spawnedChessBoard.GetComponent<ChessBoard>(), gameSpawningPointIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        internal static void RPC_DespawnObject(NetworkRunner runner, PlayerRef targetPlayer, NetworkObject objectToDespawn)
        {
            if (targetPlayer != runner.LocalPlayer)
            {
                return;
            }

            runner.Despawn(objectToDespawn);
        }

        #endregion RPCs

        #region Log Utilities

        // Utility for logging with PlayerRef included and utilizing LogError/LogWarning to bubble up debug messages easier:
        public static void DebugLog(string text, PlayerRef playerRef, bool useError = true)
        {
            //@NOTE: Disabled for now:
            return;

            //var updatedText = $"{text} [PlayerRef: {playerRef}]";
            //if (useError)
            //{
            //    Debug.LogError(updatedText);
            //}
            //else
            //{
            //    Debug.LogWarning(updatedText);
            //}
        }

        public void DebugLog(string text)
        {
            DebugLog(text, Runner.LocalPlayer);
        }

        public void DebugLogWarning(string text)
        {
            DebugLog(text, Runner.LocalPlayer, useError: false);
        }

        private static string GetChessPieceDebugText(ChessPiece chessPiece)
        {
            var color = chessPiece.IsBlack ? "Black" : "White";
            return $"[{chessPiece.RelativeIndex:00}] [{color}] [{chessPiece.ID:00}] [{chessPiece.PieceType}]";
        }
        #endregion
    }
}
