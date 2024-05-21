using System;
using System.Collections.Generic;
using Draw3D.Palettes;
using UnityEngine;

namespace Draw3D
{
    //@TODO: Candidate for deletion... in favor of newer StrokeData variants
    public class Draw3D_Stroke
    {
        // public Draw3D_Stroke(Draw3D_IDrawingDataManager drawingDataManager)
        // {
        //     _drawingDataManager = drawingDataManager;
        // }

        // private Draw3D_IDrawingDataManager _drawingDataManager = null;

        // public Color Color => Draw3D_PaletteManager.Instance.GetColor(PaletteIndex, PaletteColorIndex);
        // public Color Color => _drawingDataManager.GetStrokeColor(this);

        // private int PaletteIndex { get; set; } = Draw3D_PaletteManager.INVALID_PALETTE_INDEX;
        // private int PaletteColorIndex { get; set; } = Draw3D_PaletteManager.INVALID_COLOR_INDEX;

        // As the stroke is active and samples of its position are taken, the raw points are stored here.
        // private List<Vector3> _rawPoints = new List<Vector3>();

        //@TODO: Update to use optimized points?
        // public List<Vector3> RenderPoints => _drawingDataManager.GetStrokeRenderPoints(this);

        // public Vector3 LastPoint => _drawingDataManager.LastDrawnPoint;

        // public event Action<int> OnPaletteChange;

        // public void StartDraw(int paletteColorIndex, int brushIndex, Vector3 startingPoint)
        // {
        //     // PaletteIndex = paletteIndex;
        //     // PaletteColorIndex = paletteColorIndex;
        //
        //     // _rawPoints.Clear();
        //     // _rawPoints.Add(startingPoint);
        //
        //     _drawingDataManager.StartStroke(this, paletteColorIndex, brushIndex, startingPoint);
        // }

        // public void UpdateDraw(Vector3 newSamplePoint)
        // {
        //     // _rawPoints.Add(newSamplePoint);
        //
        //     _drawingDataManager.UpdateStroke(newSamplePoint);
        // }

        // public void EndDraw(Vector3 finalPoint)
        // {
        //     // _rawPoints.Add(finalPoint);
        //     //
        //     // OptimizeRawPoints();
        //
        //     _drawingDataManager.EndStroke(finalPoint);
        // }

        // public void ChangePalette(int paletteIndex)
        // {
        //     // PaletteIndex = paletteIndex;
        //     //
        //     // OnPaletteChange?.Invoke(PaletteIndex);
        // }

        private void OptimizeRawPoints()
        {
            //@TODO: Reduce the amount of points based on various metrics:
            //              - distance from one another
            //              - check if points are in relative straight line
        }
    }
}
