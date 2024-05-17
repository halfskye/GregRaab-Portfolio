using System.Collections.Generic;
using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Palettes
{
    public class PaletteDisplay : MonoBehaviour
    {
        [SerializeField] private Vector3 offsetBetweenDisplayAndRoot = new Vector3(0f, 1f, 0f);
        private Transform PaletteHand => Draw3D_PaletteManager.PaletteHand;
        private Vector3 PaletteHandPosition => PaletteHand ? PaletteHand.position : Vector3.zero;
        private Vector3 DisplayRoot => PaletteHandPosition + offsetBetweenDisplayAndRoot;

        [SerializeField] private Color highlightColor = Color.yellow;

        #region Palette

        [SerializeField] private float spaceBetweenPalettes = 0.1f;

        // Palette Highlighter
        [SerializeField] private Vector3 offsetBetweenPaletteAndHighlight = new Vector3(-0.1f, 0f, 0f);
        [SerializeField] private float paletteHighlightHeight = 0.3f;
        [SerializeField] private float paletteHighlightWidth = 0.1f;

        #endregion Palette

        #region Colors

        [SerializeField] private float colorHeight = 0.25f;
        [SerializeField] private float colorWidth = 0.25f;

        [SerializeField] private float spaceBetweenColors = 0.1f;

        // Color Highlighter
        [SerializeField] private Vector3 offsetBetweenColorAndHighlight = new Vector3(0f, -0.01f, 0f);
        [SerializeField] private float colorHighlightHeight = 0.1f;
        [SerializeField] private float colorHighlightWidth = 0.3f;

        #endregion Colors

        private List<List<LineRenderer>> paletteColorVisuals = new List<List<LineRenderer>>();

        private LineRenderer paletteHighlight = null;
        private LineRenderer colorHighlight = null;

        private Draw3D_PaletteManager PaletteManager => Draw3D_PaletteManager.Instance;

        private void Start()
        {
            //@NOTE: Might need to go to Start or elsewhere when loading actual available palettes (subset of total)
            SetupVisualizations();
        }

        private void Update()
        {
            //@TODO: Update Current Palette and Palette Color selection highlights

            //@TODO: Maybe make this event based instead...

            UpdateVisuals(generate: false);
        }

        private void SetupVisualizations()
        {
            UpdateVisuals(generate: true);
        }

        private void UpdateVisuals(bool generate)
        {
            var selectedPaletteIndex = PaletteManager.SelectedPaletteIndex;
            var selectedPaletteColorIndex = PaletteManager.SelectedPaletteColorIndex;

            if (generate)
            {
                GenerateHighlights();
            }

            var paletteCount = PaletteManager.TotalPaletteCount;

            var colorsCount = Draw3D_Palette.PALETTE_COLORS_COUNT;

            var paletteWidth = colorsCount * colorWidth + (colorsCount - 2) * spaceBetweenColors;
            var paletteHeight = colorHeight; // Maybe add highlight info?

            var paletteHalfWidth = paletteWidth / 2f;
            var colorHalfWidth = colorWidth / 2f;

            var xDir = Vector3.right;
            var yDir = Vector3.up;

            var startXOffset = -xDir * paletteHalfWidth;
            // var startY = DisplayRoot + yDir;

            var xy = DisplayRoot + startXOffset;

            for (int paletteIndex = 0; paletteIndex < paletteCount; ++paletteIndex)
            {
                if (paletteIndex == selectedPaletteIndex)
                {
                    var paletteHighlightWidthHalf = xDir * paletteHighlightWidth / 2f;
                    var paletteHighlightXY = xy + offsetBetweenPaletteAndHighlight;
                    paletteHighlight.SetPosition(0, paletteHighlightXY - paletteHighlightWidthHalf);
                    paletteHighlight.SetPosition(1, paletteHighlightXY + paletteHighlightWidthHalf);
                }

                List<LineRenderer> paletteColors = generate ? new List<LineRenderer>() : paletteColorVisuals[paletteIndex];

                for (int colorIndex = 0; colorIndex < colorsCount; ++colorIndex)
                {
                    if (paletteIndex == selectedPaletteIndex &&
                        colorIndex == selectedPaletteColorIndex)
                    {
                        var colorHighlightWidthHalf = xDir * colorHighlightWidth / 2f;
                        var colorHighlightXY = xy + offsetBetweenColorAndHighlight + xDir * colorHalfWidth;
                        colorHighlight.SetPosition(0, colorHighlightXY - colorHighlightWidthHalf);
                        // colorHighlight.SetPosition(0, colorHighlightXY /*- xDir * colorHighlightWidth*/);
                        colorHighlight.SetPosition(1, colorHighlightXY + colorHighlightWidthHalf);
                        // colorHighlight.SetPosition(1, colorHighlightXY + xDir * colorHighlightWidth);
                    }

                    LineRenderer colorVisual = null;
                    var color = PaletteManager.GetPaletteByIndex(paletteIndex).GetColorSafe(colorIndex);
                    if (generate)
                    {
                        colorVisual = LineRendererUtilities.CreateBasicDebugLineRenderer(color, colorHeight, this.transform);
                        colorVisual.positionCount = 2;

                        paletteColors.Add(colorVisual);
                    }
                    else
                    {
                        colorVisual = paletteColors[colorIndex];
                    }

                    colorVisual.startColor = color;
                    colorVisual.endColor = color;

                    colorVisual.SetPosition(0, xy);
                    xy += (xDir * colorWidth);
                    colorVisual.SetPosition(1, xy);
                    if (colorIndex != colorsCount - 1)
                    {
                        xy += (xDir * spaceBetweenColors);
                    }
                }
                if (generate)
                {
                    paletteColorVisuals.Add(paletteColors);
                }

                xy += (yDir * (colorHeight + spaceBetweenPalettes));
                // xy += startXOffset;
                xy += paletteWidth * -xDir;
            }
        }

        private void GenerateHighlights()
        {
            paletteHighlight = LineRendererUtilities.CreateBasicDebugLineRenderer(highlightColor, paletteHighlightHeight, this.transform);
            paletteHighlight.positionCount = 2;

            colorHighlight = LineRendererUtilities.CreateBasicDebugLineRenderer(highlightColor, colorHighlightHeight, this.transform);
            colorHighlight.positionCount = 2;
        }
    }
}
