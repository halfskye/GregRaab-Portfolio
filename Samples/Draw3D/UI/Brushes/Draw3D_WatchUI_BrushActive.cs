using System.Collections.Generic;
using Draw3D.Brushes;
using TMPro;
using UnityEngine;

namespace Draw3D.UI
{
    public class Draw3D_WatchUI_BrushActive : MonoBehaviour
    {
        [SerializeField] private List<Draw3D_WatchUI_Brush> brushes = null;

        [SerializeField] private TextMeshProUGUI brushName = null;

        private void Awake()
        {
            Draw3D_BrushManager.OnBrushChange += OnBrushChange;

            Draw3D_BrushManager.OnEraserActive += OnEraserActive;
            Draw3D_BrushManager.OnEraserInactive += OnEraserInactive;
        }

        private void OnDestroy()
        {
            Draw3D_BrushManager.OnBrushChange -= OnBrushChange;

            Draw3D_BrushManager.OnEraserActive -= OnEraserActive;
            Draw3D_BrushManager.OnEraserInactive -= OnEraserInactive;
        }

        public void SelectNextBrush()
        {
            Draw3D_BrushManager.Instance.SelectNextAvailableBrush();
        }

        public void SelectPreviousBrush()
        {
            Draw3D_BrushManager.Instance.SelectPreviousAvailableBrush();
        }

        public void SelectBrush(int brushIndex)
        {
            Draw3D_BrushManager.Instance.SelectBrushByIndex(brushIndex);
        }

        private void OnBrushChange(int brushIndex)
        {
            var brush = Draw3D_BrushManager.Instance.SelectedBrush;
            brushName.text = brush.DisplayName;

            SetOnlySelectedBrushHighlightActive();
        }

        private void OnEraserActive()
        {
            SetAllHighlightsInactive();
        }

        private void OnEraserInactive()
        {
            SetOnlySelectedBrushHighlightActive();
        }

        private delegate bool HighlightCondition(int brushIndex);
        private void SetHighlightsActive(HighlightCondition condition)
        {
            for (var i = 0; i < brushes.Count; i++)
            {
                brushes[i].SetHighlightActive(condition(i));
            }
        }

        private void SetOnlySelectedBrushHighlightActive()
        {
            var brushIndex = Draw3D_BrushManager.Instance.SelectedBrushIndex;
            SetHighlightsActive((i => i == brushIndex));
        }

        private void SetAllHighlightsInactive()
        {
            SetHighlightsActive((_) => false);
        }
    }
}
