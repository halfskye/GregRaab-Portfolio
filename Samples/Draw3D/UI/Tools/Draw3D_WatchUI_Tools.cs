using System;
using Emerge.Home.Experiments.Draw3D.Brushes;
using UnityEngine;
using UnityEngine.UI;

namespace Emerge.Home.Experiments.Draw3D.UI
{
    public class Draw3D_WatchUI_Tools : MonoBehaviour
    {
        [SerializeField] private Image eraserHighlight = null;

        private void Awake()
        {
            Draw3D_BrushManager.OnEraserActive += OnEraserActive;
            Draw3D_BrushManager.OnEraserInactive += OnEraserInactive;
        }

        private void OnDestroy()
        {
            Draw3D_BrushManager.OnEraserActive -= OnEraserActive;
            Draw3D_BrushManager.OnEraserInactive -= OnEraserInactive;
        }

        public void ClearActiveDrawing()
        {
            Draw3D_Manager.Instance.DestroyCurrentDrawing();
        }

        public void ToggleEraser()
        {
            Draw3D_BrushManager.Instance.ToggleEraser();
        }

        private void OnEraserActive()
        {
            SetEraserHighlightActive(true);
        }

        private void OnEraserInactive()
        {
            SetEraserHighlightActive(false);
        }

        private void SetEraserHighlightActive(bool isActive)
        {
            eraserHighlight.gameObject.SetActive(isActive);
        }
    }
}
