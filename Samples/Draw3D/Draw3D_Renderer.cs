using System.Collections.Generic;
using System.Linq;
using Draw3D.Brushes;
using Tracking;
using UnityEngine;

namespace Draw3D
{
    public class Draw3D_Renderer : MonoBehaviour
    {
        private Dictionary<Draw3D_Drawing, Dictionary<Draw3D_BaseStrokeData, LineRenderer>> _drawingRenderers =
            new Dictionary<Draw3D_Drawing, Dictionary<Draw3D_BaseStrokeData, LineRenderer>>();

        public void RenderDrawing(Draw3D_Drawing drawing)
        {
            AddDrawingRenderer(drawing);
        }

        private void AddDrawingRenderer(Draw3D_Drawing drawing)
        {
            if (!_drawingRenderers.ContainsKey(drawing))
            {
                var strokeRenderers = new Dictionary<Draw3D_BaseStrokeData, LineRenderer>();
                drawing.Strokes.Values.ToList().ForEach(stroke =>
                {
                    if (Draw3D_BrushManager.Instance.IsBrushIndexValid(stroke.BrushIndex))
                    {
                        strokeRenderers.Add(stroke, CreateLineRendererFromStroke(drawing, stroke, drawing.transform));
                    }
                });
                _drawingRenderers.Add(drawing, strokeRenderers);

                drawing.OnPaletteChange += DrawingOnPaletteChange;
                //@TODO: Unsubscribe
            }
        }

        private void DrawingOnPaletteChange(Draw3D_Drawing drawing)
        {
            if (_drawingRenderers.TryGetValue(drawing, out var strokeRenderers))
            {
                foreach (var (stroke, lineRenderer) in strokeRenderers)
                {
                    if (!lineRenderer.material.IsNullOrDestroyed())
                    {
                        Destroy(lineRenderer.material);
                    }

                    lineRenderer.material = drawing.Palette.Material;

                    var color = drawing.GetStrokeColor(stroke);
                    lineRenderer.startColor = lineRenderer.endColor = color;
                    lineRenderer.material.SetColor("_BaseColor", color);
                }
            }
        }

        private void AddStrokeRenderer(Draw3D_Drawing drawing, Draw3D_BaseStrokeData stroke)
        {
            if (!drawing.IsNullOrDestroyed())
            {
                if (!_drawingRenderers[drawing].ContainsKey(stroke))
                {
                    _drawingRenderers[drawing].Add(stroke,
                        CreateLineRendererFromStroke(drawing, stroke, drawing.transform));
                }
            }
        }

        public void UpdateStrokeRenderer(Draw3D_Drawing drawing, Draw3D_Brush brush, Draw3D_BaseStrokeData stroke)
        {
            if (drawing.IsNullOrDestroyed())
            {
                return;
            }

            AddDrawingRenderer(drawing);
            AddStrokeRenderer(drawing, stroke);

            var renderPoints = stroke.RenderPoints;
            var pointCount = renderPoints.Count;
            var lineRenderer = _drawingRenderers[drawing][stroke];
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPositions(renderPoints.ToArray());

            if (brush.UseWidthCurve)
            {
                UpdateStrokeWidthCurve(brush, stroke, ref lineRenderer);
            }
        }

        private static void UpdateStrokeWidthCurve(Draw3D_Brush brush, Draw3D_BaseStrokeData stroke, ref LineRenderer lineRenderer)
        {
            var renderPoints = stroke.RenderPoints;
            var pointCount = renderPoints.Count;

            //@TODO: Try using existing AnimationCurve and going from there... as is, the lines can be a little wavy which is a cool effect, but something to try differently
            var widthCurve = new AnimationCurve();

            var sampleDistanceSqrThreshold = brush.SampleDistancePerSecondThreshold * brush.SampleDistancePerSecondThreshold;
            var distanceSqrThreshold = sampleDistanceSqrThreshold * brush.SampleMinTime;

            for (var i = 0; i < pointCount; ++i)
            {
                var pointTime = (float) i / pointCount;

                if (i == 0)
                {
                    widthCurve.AddKey(0f, brush.Width);
                }
                else
                {
                    var distSq = (renderPoints[i] - renderPoints[i - 1]).sqrMagnitude;
                    var distSqRatio = Mathf.Clamp01(distSq / distanceSqrThreshold);

                    var width = Mathf.Lerp(brush.MaxWidth, brush.MinWidth, distSqRatio);

                    widthCurve.AddKey(pointTime, width);
                }
            }

            lineRenderer.widthCurve = widthCurve;
        }

        private static LineRenderer CreateLineRendererFromStroke(Draw3D_Drawing drawing, Draw3D_BaseStrokeData stroke, Transform parent)
        {
            var newGameObject = new GameObject("StrokeLineRenderer");
            newGameObject.transform.parent = parent;
            var lineRenderer = newGameObject.AddComponent<LineRenderer>();

            var color = drawing.GetStrokeColor(stroke);

            lineRenderer.material = drawing.Palette.Material;
            lineRenderer.startWidth = lineRenderer.endWidth = stroke.Brush.Width;
            lineRenderer.material.SetColor("_BaseColor", color);
            lineRenderer.startColor = lineRenderer.endColor = color;
            lineRenderer.useWorldSpace = false;
            lineRenderer.enabled = true;

            var renderPoints = stroke.RenderPoints;
            lineRenderer.positionCount = renderPoints.Count;
            lineRenderer.SetPositions(renderPoints.ToArray());

            return lineRenderer;
        }

        public void EraseStroke(Draw3D_Drawing drawing, Draw3D_BaseStrokeData stroke)
        {
            if (_drawingRenderers[drawing].TryGetValue(stroke, out var lineRenderer))
            {
                _drawingRenderers[drawing].Remove(stroke);

                Destroy(lineRenderer.gameObject);
            }
        }
    }
}
