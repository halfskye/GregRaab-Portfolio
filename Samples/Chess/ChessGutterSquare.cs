using UnityEngine;

namespace Emerge.Chess
{
    public class ChessGutterSquare : MonoBehaviour
    {
        public bool isOccupied { get;  set; }
        public Vector3 GetPosition()
        {
            return this.transform.position;
        }
    }
}
