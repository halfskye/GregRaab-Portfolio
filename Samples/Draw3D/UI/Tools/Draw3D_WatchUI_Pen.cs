using Draw3D.Brushes;
using Tracking;
using UnityEngine;

namespace Draw3D.UI
{
    public class Draw3D_WatchUI_Pen : MonoBehaviour
    {
        [SerializeField] private float moveIncrement = 0.1f;

        private Draw3D_Pen Pen
        {
            get
            {
                if (_pen.IsNullOrDestroyed())
                {
                    _pen = FindObjectOfType<Draw3D_Pen>();
                }

                return _pen;
            }
        }
        private Draw3D_Pen _pen = null;

        private void Move(Vector3 vector, float distance)
        {
            var penTransform = Pen.transform;

            penTransform.position -= (vector * distance);
        }

        public void MoveForward()
        {
            Pen.MoveForward();
        }

        public void MoveBack()
        {
            Pen.MoveBackward();
        }
    }
}
