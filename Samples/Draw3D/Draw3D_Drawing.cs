using System;
using System.Collections.Generic;
using Draw3D.Palettes;
using UnityEngine;

namespace Draw3D
{
    // Finished Drawing
    //@TODO: Maybe doesn't need to be a MonoBehavior?
    public class Draw3D_Drawing : MonoBehaviour
    {
        public Draw3D_IDrawingDataManager DrawingDataManager { get; private set; } = null;
        public void SetDrawingDataManager(Draw3D_IDrawingDataManager drawingDataManager) { DrawingDataManager = drawingDataManager; }

        public Dictionary<int, Draw3D_BaseStrokeData> Strokes => DrawingDataManager.StrokeData;
        private int StrokeCount => DrawingDataManager.StrokeData.Count;
        // private Draw3D_Stroke LastStroke => Strokes.Count > 0 ? Strokes[^1] : null;

        public Draw3D_BaseStrokeData CurrentStroke => DrawingDataManager.CurrentStroke;

        public Vector3 LastDrawnPoint => DrawingDataManager.LastDrawnPoint;

        public Draw3D_Palette Palette => Draw3D_PaletteManager.Instance.GetPaletteByIndex(DrawingDataManager.PaletteIndex);

        public event Action<Draw3D_Drawing> OnPaletteChange;

        public Color GetStrokeColor(Draw3D_BaseStrokeData strokeData)
        {
            return DrawingDataManager.GetStrokeColor(strokeData);
        }

        public void StartDrawing(int paletteIndex)
        {
            DrawingDataManager.StartDrawing(paletteIndex);
        }

        public void StartStroke(int paletteColorIndex, int brushIndex, Vector3 startingPoint)
        {
            var strokeIndex = StrokeCount;
            DrawingDataManager.StartStroke(strokeIndex, paletteColorIndex, brushIndex, startingPoint);
        }

        public void AddNewDrawnPoint(Vector3 newDrawnPoint)
        {
            DrawingDataManager.UpdateStroke(newDrawnPoint);
        }

        public void EndDrawing(Vector3 finalDrawnPoint)
        {
            DrawingDataManager.EndStroke(finalDrawnPoint);
        }

        // public void UndoLastStroke()
        // {
        //     if (Strokes.Count > 0)
        //     {
        //         Strokes.Remove(LastStroke);
        //
        //         //@TODO: Maybe add "possible redo strokes" and RedoStroke function
        //     }
        // }

        public override string ToString()
        {
            return $"Palette: \"{Palette.DisplayName}\", Stroke Count: {DrawingDataManager.StrokeCount.ToString()}";
        }

        public void ChangePalette(int paletteIndex)
        {
            DrawingDataManager.ChangePalette(paletteIndex);

            OnPaletteChange?.Invoke(this);
        }
    }
}
