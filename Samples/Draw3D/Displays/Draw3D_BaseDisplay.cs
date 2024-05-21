using System;
using UnityEngine;

namespace Draw3D.Displays
{
    public class Draw3D_BaseDisplay : MonoBehaviour
    {
        [SerializeField] private Transform _displayAnchor = null;
        public Transform DisplayAnchor => _displayAnchor;

        public Draw3D_Drawing AnchoredDrawing { get; private set; } = null;

        public bool IsOccupied => AnchoredDrawing != null;

        private void Start()
        {
            Draw3D_DisplayManager.Instance.RegisterDisplay(this);
        }

        private void OnDestroy()
        {
            Draw3D_DisplayManager.Instance.UnregisterDisplay(this);
        }

        public bool TryAnchorDrawing(Draw3D_Drawing drawing)
        {
            if (!IsOccupied)
            {
                AnchorDrawing(drawing);

                return true;
            }

            return false;
        }

        private void AnchorDrawing(Draw3D_Drawing drawing)
        {
            // Re-Parent Drawing to Anchor
            var drawingTransform = drawing.transform;
            drawingTransform.parent = DisplayAnchor;

            //@TODO: Find Drawing dims and center drawing w/ offset from anchor
            drawingTransform.position = DisplayAnchor.position;
            drawingTransform.rotation = DisplayAnchor.rotation;

            AnchoredDrawing = drawing;
        }
    }
}
