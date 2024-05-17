using System.Collections.Generic;
using UnityEngine;

namespace Emerge.Chess
{
    public class ChessGutter : MonoBehaviour
    {
        [SerializeField] private List<ChessGutterSquare> squares = null;

        public delegate bool SelectOnCondition(Vector3 position);

        private void Start()
        {
            ResetGutterSquares();
        }
        public bool GetFirstAvailableSquarePosition(SelectOnCondition selectOnCondition, out Vector3 position)
        {
            position = Vector3.negativeInfinity;

            foreach (var square in squares)
            {
                Debug.Log("Occupied : "+square.isOccupied);
                position = square.GetPosition();
                if (selectOnCondition(position) && !square.isOccupied )
                {
                    square.isOccupied = true;
                    return true;
                }
            }

            return false;
        }

        public void ResetGutterSquares()
        {
            foreach (var square in squares)
            {
                square.isOccupied = false;
            }
        }
    }
}
