using System.Collections.Generic;
using Emerge.Home.Experiments.Draw3D.Brushes;
using Emerge.Home.Experiments.Draw3D.Palettes;
using UnityEngine;
using Color = UnityEngine.Color;

//@TODO: Separate all these types into separate/appropriate files.
namespace Emerge.Home.Experiments.Draw3D
{
    public interface Draw3D_IStrokeData
    {
        public int StrokeIndex { get; }

        public List<Vector3> DataPoints { get; }
        public void AddDrawnPoint(Vector3 drawnPoint);

        public int PaletteColorIndex { get; }
        public int BrushIndex { get; }
        public Draw3D_Brush Brush { get; }

        public Vector3 LastDrawnPoint { get; }

        public List<Vector3> RenderPoints { get; }
    }

    public abstract class Draw3D_BaseStrokeData : Draw3D_IStrokeData
    {
        protected Draw3D_BaseStrokeData(int strokeIndex, int paletteColorIndex, int brushIndex)
        {
            StrokeIndex = strokeIndex;
            PaletteColorIndex = paletteColorIndex;
            BrushIndex = brushIndex;
        }

        public int StrokeIndex { get; }

        public abstract List<Vector3> DataPoints { get; }
        public abstract void AddDrawnPoint(Vector3 drawnPoint);
        public abstract void EndStroke();
        public int PaletteColorIndex { get; private set; } = Draw3D_PaletteManager.INVALID_COLOR_INDEX;
        public int BrushIndex { get; private set; } = Draw3D_BrushManager.INVALID_BRUSH_INDEX;
        public Draw3D_Brush Brush => Draw3D_BrushManager.Instance.GetBrushByIndex(BrushIndex);

        public void UpdateData(int paletteColorIndex, int brushIndex)
        {
            PaletteColorIndex = paletteColorIndex;
            BrushIndex = brushIndex;
        }

        public Vector3 LastDrawnPoint => DataPoints[^1];

        public List<Vector3> RenderPoints => DataPoints;

        public void Erase() { BrushIndex = Draw3D_BrushManager.INVALID_BRUSH_INDEX; }
        public bool IsErased => BrushIndex == Draw3D_BrushManager.INVALID_BRUSH_INDEX;
    }

    public class Draw3D_StrokeData : Draw3D_BaseStrokeData
    {
        public Draw3D_StrokeData(int strokeIndex, int paletteColorIndex, int brushIndex)
            : base(strokeIndex, paletteColorIndex, brushIndex)
        {
        }

        public override List<Vector3> DataPoints { get; } = new List<Vector3>();

        public override void AddDrawnPoint(Vector3 drawnPoint)
        {
            DataPoints.Add(drawnPoint);
        }

        public override void EndStroke()
        {
        }
    }

    public class Draw3D_NetworkedStrokeData : Draw3D_BaseStrokeData
    {
        public Draw3D_NetworkedDrawing Drawing { get; } = null;

        public Draw3D_NetworkedStrokeData(Draw3D_NetworkedDrawing drawing, int strokeIndex, int paletteColorIndex, int brushIndex)
            : base(strokeIndex, paletteColorIndex, brushIndex)
        {
            Drawing = drawing;
        }

        public override List<Vector3> DataPoints => Drawing.GetStrokeDrawnPoints(this);

        public override void AddDrawnPoint(Vector3 drawnPoint)
        {
            // Debug.LogError("Draw3D_NetworkedStrokeData - AddDrawnPoint");

            Drawing.AddStrokeDrawnPoint(this, drawnPoint);
        }

        public override void EndStroke()
        {
            Drawing.EndStroke();
        }
    }

    public class Draw3D_DrawingData
    {
        public Dictionary<int, Draw3D_BaseStrokeData> StrokeData { get; internal set; } = null;
        public int StrokeCount => StrokeData.Count;
        public Draw3D_BaseStrokeData CurrentStroke { get; private set; } = null;

        public int PaletteIndex { get; private set; } = Draw3D_PaletteManager.INVALID_PALETTE_INDEX;

        public Draw3D_DrawingData(int paletteIndex)
        {
            PaletteIndex = paletteIndex;
        }

        public void StartStroke(Draw3D_BaseStrokeData strokeData)
        {
            StrokeData.Add(strokeData.StrokeIndex, strokeData);
            CurrentStroke = strokeData;
        }

        public void UpdateStroke(Vector3 newSamplePoint)
        {
            AddCurrentStrokePoint(newSamplePoint);
        }

        public void EndStroke(Vector3 finalSamplePoint)
        {
            AddCurrentStrokePoint(finalSamplePoint);
            CurrentStroke.EndStroke();
            CurrentStroke = null;
        }

        private void AddCurrentStrokePoint(Vector3 newSamplePoint)
        {
            CurrentStroke.AddDrawnPoint(newSamplePoint);
        }

        public void ChangePalette(int paletteIndex)
        {
            PaletteIndex = paletteIndex;
        }

        public Color GetStrokeColor(Draw3D_BaseStrokeData stroke)
        {
            return Draw3D_PaletteManager.Instance.GetColor(PaletteIndex, stroke.PaletteColorIndex);
        }

        public Vector3 LastDrawnPoint => CurrentStroke.LastDrawnPoint;
    }

    public interface Draw3D_IDrawingDataManager
    {
        public void StartDrawing(int paletteIndex);

        public int PaletteIndex { get; }

        public void StartStroke(int strokeIndex, int paletteColorIndex, int brushIndex, Vector3 startingPoint);
        public void StartStroke(Draw3D_BaseStrokeData strokeData);
        public void UpdateStroke(Vector3 newSamplePoint);
        public void EndStroke(Vector3 finalSamplePoint);

        public Dictionary<int, Draw3D_BaseStrokeData> StrokeData { get; }

        public Draw3D_BaseStrokeData CurrentStroke { get; }

        public int StrokeCount { get; }

        public void ChangePalette(int paletteIndex);

        public Vector3 LastDrawnPoint { get; }

        public Color GetStrokeColor(Draw3D_BaseStrokeData stroke);
    }

    public abstract class Draw3D_BaseDrawingDataManager : Draw3D_IDrawingDataManager
    {
        private Draw3D_DrawingData DrawingData { get; set; } = null;

        public void StartDrawing(int paletteIndex)
        {
            DrawingData = new Draw3D_DrawingData(paletteIndex);
            DrawingData.StrokeData = new Dictionary<int, Draw3D_BaseStrokeData>();
        }

        public int PaletteIndex => DrawingData.PaletteIndex;

        public abstract void StartStroke(int strokeIndex, int paletteColorIndex, int brushIndex, Vector3 startingPoint);

        public void StartStroke(Draw3D_BaseStrokeData strokeData)
        {
            DrawingData.StartStroke(strokeData);
        }

        public void UpdateStroke(Vector3 newSamplePoint)
        {
            DrawingData.UpdateStroke(newSamplePoint);
        }

        public void EndStroke(Vector3 finalSamplePoint)
        {
            DrawingData.EndStroke(finalSamplePoint);
        }

        public Dictionary<int, Draw3D_BaseStrokeData> StrokeData => DrawingData.StrokeData;

        public Draw3D_BaseStrokeData CurrentStroke => DrawingData.CurrentStroke;

        public int StrokeCount => DrawingData.StrokeCount;

        public virtual void ChangePalette(int paletteIndex)
        {
            DrawingData.ChangePalette(paletteIndex);
        }

        public Color GetStrokeColor(Draw3D_BaseStrokeData stroke)
        {
            return DrawingData.GetStrokeColor(stroke);
        }

        public Vector3 LastDrawnPoint => DrawingData.LastDrawnPoint;
    }

    public class Draw3D_DrawingDataManager : Draw3D_BaseDrawingDataManager
    {
        public override void StartStroke(int strokeIndex, int paletteColorIndex, int brushIndex, Vector3 startingPoint)
        {
            Debug.LogError("Draw3D_DrawingDataManager - StartStroke");

            var strokeData = new Draw3D_StrokeData(strokeIndex, paletteColorIndex, brushIndex);
            strokeData.AddDrawnPoint(startingPoint);

            StartStroke(strokeData);
        }
    }

    public class Draw3D_NetworkedDrawingDataManager : Draw3D_BaseDrawingDataManager
    {
        public Draw3D_NetworkedDrawing Drawing { get; } = null;

        public Draw3D_NetworkedDrawingDataManager(Draw3D_NetworkedDrawing drawing)
        {
            Drawing = drawing;
        }

        public override void StartStroke(int strokeIndex, int paletteColorIndex, int brushIndex, Vector3 startingPoint)
        {
            // Debug.LogError("Draw3D_NetworkedDrawingDataManager - StartStroke");

            var strokeData = new Draw3D_NetworkedStrokeData(Drawing, strokeIndex, paletteColorIndex, brushIndex);
            strokeData.AddDrawnPoint(startingPoint);

            StartStroke(strokeData);
        }

        public override void ChangePalette(int paletteIndex)
        {
            base.ChangePalette(paletteIndex);

            Drawing.ChangePalette(paletteIndex);
        }
    }
}
