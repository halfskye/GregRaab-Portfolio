using System;
using UnityEngine;

namespace Emerge.Chess
{
    [CreateAssetMenu(fileName = "NewChessPiece", menuName = "ChessPrototype/NewChessPiece")]
    public class ChessPieceSO : ScriptableObject
    {
        public const int GRID_SIZE = 8;
        
        // Uppercase char A- H as int value: 65 - 72
        // lowercase char a - h as int value: 97 - 104
        private const int UPPERCASE_CHAR_START = 65;
        private const int UPPERCASE_CHAR_END = 72;
        private const int LOWERCASE_CHAR_START = 97;
        private const int LOWERCASE_CHAR_END = 104;

        private const int START_ROW = 1;
        private const int END_ROW = GRID_SIZE;
        public const int START_ROW_FAR = START_ROW + 1;
        public const int END_ROW_NEAR = END_ROW - 1;
        
        [Serializable]
        public struct BoardPosition
        {
            [SerializeField] private char column;
            public char Column => column;

            [SerializeField] private int row;
            public int Row => row;

            public BoardPosition(int row, char column)
            {
                this.row = row;
                this.column = column;
            }

            // Convert chess column char to int value 0 to 7 to work with grid systems
            public int ColumnInt => Column >= UPPERCASE_CHAR_START && Column <= UPPERCASE_CHAR_END
                ? Column - UPPERCASE_CHAR_START
                : Column - LOWERCASE_CHAR_START;
            
            public Vector2Int GetBoardPositionXY()
            {
                // The -1 on Row converts 1- 8 chess position to 0 - 7 to work with grid systems
                return new Vector2Int(Row - 1, ColumnInt);
            }
        }

        [Serializable]
        public struct PieceData
        {
            [SerializeField] private Mesh meshToUse;
            public Mesh MeshToUse => meshToUse;

            [SerializeField] private float yRotation;
            public float YRotation => yRotation;

            [SerializeField] private BoardPosition[] spawnPositions;
            public BoardPosition[] SpawnPositions => spawnPositions;

            public void SetPosition(int posIndex, BoardPosition newValue)
            {
                spawnPositions[posIndex] = newValue;
            }
        }

        [SerializeField] private ChessBoard.PiecesEnum pieceType;
        public ChessBoard.PiecesEnum PieceType => pieceType;

        [SerializeField] private PieceData blackPieceData;
        public PieceData BlackPieceData => blackPieceData;

        [SerializeField] private PieceData whitePieceData;
        public PieceData WhitePieceData => whitePieceData;

        public PieceData GetPieceDataByPlayerColor(ChessBoard.PlayerColors playerColor)
        {
            return playerColor == ChessBoard.PlayerColors.Black ? BlackPieceData : WhitePieceData;
        }

        private void OnValidate()
        {
            ValidateBoardPositionArray(ref blackPieceData);
            ValidateBoardPositionArray(ref whitePieceData);
        }

        private void ValidateBoardPositionArray(ref PieceData pieceData)
        {
            for (int i = 0; i < pieceData.SpawnPositions.Length; i++)
            {
                if (IsRowOutOfBounds(pieceData.SpawnPositions[i].Row))
                {
                    pieceData.SetPosition(i, new BoardPosition(1, pieceData.SpawnPositions[i].Column));
                }

                if (IsColumnOutOfBounds(pieceData.SpawnPositions[i].Column))
                {
                    pieceData.SetPosition(i, new BoardPosition(pieceData.SpawnPositions[i].Row, 'a'));
                }
            }
        }

        private static bool IsRowOutOfBounds(int row)
        {
            return row < 1 || row > 8;
        }

        private static bool IsColumnOutOfBounds(char column)
        {
            return !(column >= UPPERCASE_CHAR_START && column <= UPPERCASE_CHAR_END ||
                     column >= LOWERCASE_CHAR_START && column <= LOWERCASE_CHAR_END);
        }
    }
}