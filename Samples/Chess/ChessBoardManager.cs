using System.Collections.Generic;
using System.Threading.Tasks;
using Emerge.Home;
using EmergeHome.Code.Core;
using EmergeHome.Code.Environments;
using Fusion;
using UnityEngine;

namespace Emerge.Chess
{
    public class ChessBoardManager : MonoBehaviour
    {
        public static ChessBoardManager Instance { get; private set; } = null;

        [SerializeField] private NetworkObject chessBoardPrefab = null;

        private readonly BidirectionalDictionaryUnique<ChessBoard, int> _chessBoardMapBySpawnIndex = new BidirectionalDictionaryUnique<ChessBoard, int>();

        private readonly HashSet<int> _pendingGameSpawnPoints = new HashSet<int>();

        #region Unity Methods

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("There should only be one ChessBoardManager.");
                Destroy(this.gameObject);

                return;
            }

            Instance = this;

            RegisterEvents(subscribe: true);
        }

        private void OnDestroy()
        {
            RegisterEvents(subscribe: false);
        }

        #endregion Unity Methods

        #region Events

        private void RegisterEvents(bool subscribe)
        {
            if (subscribe)
            {
                TableManager.OnGameSpawnPointHidden += TableManagerOnGameSpawnPointHidden;
            }
            else
            {
                TableManager.OnGameSpawnPointHidden -= TableManagerOnGameSpawnPointHidden;
            }
        }

        private async void TableManagerOnGameSpawnPointHidden(int gameSpawnPointIndex)
        {
            var chessBoard = GetPossibleChessBoardByGameSpawnPointIndex(gameSpawnPointIndex);
            if (chessBoard != null)
            {
                await DespawnChessBoard(chessBoard);
            }
        }

        #endregion Events

        #region Chess Board Spawn/Despawn

        public void RequestSpawnChessBoard()
        {
            var gameSpawnPoint = GetPossibleGameSpawnPoint();
            if (gameSpawnPoint != null)
            {
                ChessBoard.RPC_RequestSpawnChessBoard(GetNetworkRunner(), gameSpawnPoint.Index);
            }
        }

        internal void SpawnChessBoardAtGameSpawnPoint(GameSpawningPoint gameSpawnPoint)
        {
            _pendingGameSpawnPoints.Add(gameSpawnPoint.Index);

            var gameSpawnPointTransform = gameSpawnPoint.GetSpawnPoint();
            var runner = GetNetworkRunner();
            NetworkObject spawnedObject = runner.Spawn(
                chessBoardPrefab,
                gameSpawnPointTransform.position,
                gameSpawnPointTransform.rotation * chessBoardPrefab.transform.rotation,
                runner.LocalPlayer
            );

            ChessBoard.RPC_SpawnedChessBoard(runner, spawnedObject, gameSpawnPoint.Index);
        }

        public async Task DespawnChessBoard(ChessBoard chessBoard)
        {
            if (chessBoard == null || chessBoard.Object == null)
            {
                await Task.CompletedTask;
            }
            else
            {
                ChessBoard.RPC_DespawnObject(GetNetworkRunner(), chessBoard.Object.StateAuthority, chessBoard.Object);
                while (chessBoard.Object != null)
                {
                    await Task.Yield();
                }

                // RPC_UpdateIsSpawnedState(false);
                await Task.CompletedTask;
            }
        }

        #region Chess Board Reference Handlers

        internal void HandleNewChessBoard(ChessBoard chessBoard, int gameSpawnPointIndex)
        {
            _pendingGameSpawnPoints.Remove(gameSpawnPointIndex);
            _chessBoardMapBySpawnIndex.Add(chessBoard, gameSpawnPointIndex);

            var player = ChessPlayerManager.Instance.GetLocalPlayerByBoard(chessBoard);
            player.HandleNewChessBoard(chessBoard);
        }

        public void HandleRemovedChessBoardObject(ChessBoard chessBoard)
        {
            _chessBoardMapBySpawnIndex.Remove(chessBoard);
        }

        #endregion Chess Board Reference

        #endregion Chess Board Spawn/Despawn

        #region Utilities

        private static GameSpawningPoint GetPossibleGameSpawnPoint()
        {
            return TableManager.Instance.GetCurrentPlayersGameSpawningPoint(out var gameSpawnPoint) ? gameSpawnPoint : null;
        }

        public static GameSpawningPoint GetPossibleGameSpawnPointByIndex(int gameSpawnPointIndex)
        {
            return TableManager.Instance.GetGameSpawningPointByIndex(gameSpawnPointIndex);
        }

        private ChessBoard GetPossibleChessBoardByGameSpawnPointIndex(int gameSpawnPointIndex)
        {
            return _chessBoardMapBySpawnIndex.ContainsKey(gameSpawnPointIndex)
                ? _chessBoardMapBySpawnIndex[gameSpawnPointIndex]
                : null;
        }

        public bool DoesGameSpawnPointHaveChessBoardOrPending(int gameSpawnPointIndex)
        {
            return GetPossibleChessBoardByGameSpawnPointIndex(gameSpawnPointIndex) != null ||
                   _pendingGameSpawnPoints.Contains(gameSpawnPointIndex);
        }

        public bool IsLocalPlayerChessBoard(ChessBoard chessBoard)
        {
            if (_chessBoardMapBySpawnIndex.ContainsKey(chessBoard))
            {
                var gameSpawnPointIndex = _chessBoardMapBySpawnIndex[chessBoard];
                var localPlayerGameSpawnPoint = GetPossibleGameSpawnPoint();

                return localPlayerGameSpawnPoint != null && localPlayerGameSpawnPoint.Index == gameSpawnPointIndex;
            }

            return false;
        }

        public bool IsLocalPlayerAtChessBoardByChessPlayer(ChessPlayer chessPlayer)
        {
            var chessBoard = ChessPlayerManager.Instance.GetBoardByLocalPlayer(chessPlayer);
            return chessBoard != null && IsLocalPlayerChessBoard(chessBoard);
        }

        private NetworkRunner GetNetworkRunner()
        {
            return ApplicationManager.Instance.Runner;
            // return NetworkRunner.GetRunnerForGameObject(TableManager.Instance.gameObject);
        }

        #endregion Utilities
    }
}
