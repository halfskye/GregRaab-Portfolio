using System.Collections.Generic;
using Draw3D.Brushes;
using Draw3D.Palettes;
using TMPro;
using UnityEngine;

namespace Draw3D.UI
{
    public class Draw3D_WatchUI_PaletteActive : MonoBehaviour
    {
        [SerializeField] private List<Draw3D_WatchUI_PaletteColor> colors = null;

        [SerializeField] private TextMeshProUGUI paletteName = null;

        private void Awake()
        {
            Draw3D_PaletteManager.OnPaletteChange += OnPaletteChange;
            Draw3D_PaletteManager.OnPaletteColorChange += OnPaletteColorChange;

            Draw3D_BrushManager.OnEraserActive += OnEraserActive;
            Draw3D_BrushManager.OnEraserInactive += OnEraserInactive;
        }

        private void OnDestroy()
        {
            Draw3D_PaletteManager.OnPaletteChange -= OnPaletteChange;
            Draw3D_PaletteManager.OnPaletteColorChange -= OnPaletteColorChange;

            Draw3D_BrushManager.OnEraserActive -= OnEraserActive;
            Draw3D_BrushManager.OnEraserInactive -= OnEraserInactive;
        }

        public void SelectNextPalette()
        {
            Draw3D_PaletteManager.Instance.SelectNextAvailablePalette();
        }

        public void SelectPreviousPalette()
        {
            Draw3D_PaletteManager.Instance.SelectPreviousAvailablePalette();
        }

        public void SelectPaletteColor(int index)
        {
            Draw3D_PaletteManager.Instance.SelectActivePaletteColorByIndex(index);
        }

        private void OnPaletteChange(int paletteIndex)
        {
            var palette = Draw3D_PaletteManager.Instance.SelectedPalette;
            paletteName.text = palette.DisplayName;
            for (var i = 0; i < colors.Count; i++)
            {
                colors[i].SetPaletteColor(palette, i);
            }

            SetOnlySelectedColorHighlightActive();
        }

        private void OnPaletteColorChange(int colorIndex)
        {
            SetOnlySelectedColorHighlightActive();
        }

        private void OnEraserActive()
        {
            SetAllHighlightsInactive();
        }

        private void OnEraserInactive()
        {
            SetOnlySelectedColorHighlightActive();
        }

        private delegate bool HighlightCondition(int colorIndex);
        private void SetHighlightsActive(HighlightCondition condition)
        {
            for (var i = 0; i < colors.Count; i++)
            {
                colors[i].SetHighlightActive(condition(i));
            }
        }

        private void SetOnlySelectedColorHighlightActive()
        {
            SetHighlightsActive((i => i == Draw3D_PaletteManager.Instance.SelectedPaletteColorIndex));
        }

        private void SetAllHighlightsInactive()
        {
            SetHighlightsActive((_) => false);
        }
    }
}
