using Utilities;
using UnityEngine;

namespace Chess
{
    public class ChessPlayerManager : MonoBehaviour
    {
        public static ChessPlayerManager Instance { get; private set; } = null;

        [SerializeField] private ChessPlayer chessPlayerPrefab = null;

        private readonly BidirectionalDictionaryUnique<ChessBoard, ChessPlayer> _localChessPlayerMap = new BidirectionalDictionaryUnique<ChessBoard, ChessPlayer>();

        private ChessPlayer this[ChessBoard chessBoard] => _localChessPlayerMap[chessBoard];
        private ChessBoard this[ChessPlayer chessPlayer] => _localChessPlayerMap[chessPlayer];
        public ChessPlayer GetLocalPlayerByBoard(ChessBoard chessBoard) => this[chessBoard];
        public ChessBoard GetBoardByLocalPlayer(ChessPlayer chessPlayer) => this[chessPlayer];

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("There should only be one ChessPlayerManager.");
                Destroy(this.gameObject);

                return;
            }

            Instance = this;
        }

        public void HandleNewChessBoard(ChessBoard chessBoard)
        {
            var runner = ApplicationManager.Instance.Runner;

            // Spawn new ChessPlayer
            var mgrTransform = transform;
            var chessPlayer = runner.Spawn(
                chessPlayerPrefab,
                mgrTransform.position,
                mgrTransform.rotation,
                runner.LocalPlayer);

            chessPlayer.ChessBoardNetworkObject = chessBoard.Object;

            // Map new ChessPlayer to new ChessBoard
            _localChessPlayerMap.Add(chessBoard, chessPlayer);
        }

        public void HandleRemovedChessBoard(ChessBoard chessBoard)
        {
            if (_localChessPlayerMap.ContainsKey(chessBoard))
            {
                var player = _localChessPlayerMap[chessBoard];

                player.HandleRemovedChessBoard();

                // Despawn ChessPlayer
                var runner = ApplicationManager.Instance.Runner;
                runner.Despawn(player.Object);
            }
        }
    }
}
