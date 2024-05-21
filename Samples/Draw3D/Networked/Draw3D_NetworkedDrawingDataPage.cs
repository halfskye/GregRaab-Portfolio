using System;
using System.Collections.Generic;
using System.Linq;
using Draw3D.Brushes;
using Tracking;
using Fusion;
using Vector3 = UnityEngine.Vector3;

namespace Draw3D
{
    public class Draw3D_NetworkedDrawingDataPage : NetworkBehaviour
    {
        [Serializable]
        public struct NetworkedStrokePageData : INetworkStruct
        {
            public int PaletteColorIndex { get; }
            public int BrushIndex { get; private set; }

            public int StartIndex { get; }
            public int Count { get; set; }

            public NetworkedStrokePageData(int paletteColorIndex, int brushIndex, int startIndex)
            {
                PaletteColorIndex = paletteColorIndex;
                BrushIndex = brushIndex;
                StartIndex = startIndex;
                Count = 1;
            }

            public void EraseStroke()
            {
                BrushIndex = Draw3D_BrushManager.INVALID_BRUSH_INDEX;
            }

            #region Comparison overloads (might be unnecessary)

            public override bool Equals(System.Object obj)
            {
                return obj is NetworkedStrokePageData c && this == c;
            }
            public override int GetHashCode()
            {
                return PaletteColorIndex.GetHashCode() ^
                       BrushIndex.GetHashCode() ^
                       StartIndex.GetHashCode() ^
                       Count.GetHashCode();
            }
            public static bool operator ==(NetworkedStrokePageData x, NetworkedStrokePageData y)
            {
                return x.PaletteColorIndex == y.PaletteColorIndex &&
                       x.BrushIndex == y.BrushIndex &&
                       x.StartIndex == y.StartIndex &&
                       x.Count == y.Count;
            }
            public static bool operator !=(NetworkedStrokePageData x, NetworkedStrokePageData y)
            {
                return !(x == y);
            }

            #endregion Comparison overloads (might be unnecessary)
        }

        [Serializable]
        public struct NetworkedStrokeDrawnPointData : INetworkStruct
        {
            public Vector3 Position { get; }
            // public int BrushIndex { get; } //@TODO: Might be unnecessary due to NetworkedLinkedList usage

            public NetworkedStrokeDrawnPointData(Vector3 position)
            {
                Position = position;
            }
        }

        private const int INVALID_PAGE_INDEX = -1;

        [Networked(OnChanged = nameof(OnDataChanged))]
        public int PageIndex { get; private set; } = INVALID_PAGE_INDEX;

        private const int PAGE_SIZE = 512;

        [Networked(OnChanged = nameof(OnDataChanged)), Capacity(PAGE_SIZE)]
        public NetworkLinkedList<NetworkedStrokeDrawnPointData> DrawnPoints { get; }

        //@TODO: Probably need to play with this... or come up with another paging mechanism.
        private const int MAX_STROKE_COUNT = 64;

        [Networked(OnChanged = nameof(OnDataChanged)), Capacity(MAX_STROKE_COUNT)]
        public NetworkDictionary<int, NetworkedStrokePageData> StrokePageData { get; }

        [Networked]
        public Draw3D_NetworkedDrawing Drawing { get; private set; } = null;

        public void InitializeData(Draw3D_NetworkedDrawing drawing, int pageIndex)
        {
            Drawing = drawing;
            PageIndex = pageIndex;
        }

        public List<Vector3> GetStrokeDrawnPoints(Draw3D_BaseStrokeData stroke)
        {
            var drawnPoints = new List<Vector3>();
            if (StrokePageData.ContainsKey(stroke.StrokeIndex))
            {
                var strokePageData = StrokePageData[stroke.StrokeIndex];

                // Debug.LogError($"Draw3D_NetworkedDrawingDataPage - GetStrokeDrawnPoints - Stroke Index: {stroke.StrokeIndex}, Start: {strokePageData.StartIndex}, Count: {strokePageData.Count}");

                var rawPoints = GetRawPointsRange(strokePageData.StartIndex, strokePageData.Count);
                if (!rawPoints.IsNullOrDestroyed() && rawPoints.Any())
                {
                    drawnPoints.AddRange(rawPoints);
                }
            }
            return drawnPoints;
        }

        public static void OnDataChanged(Changed<Draw3D_NetworkedDrawingDataPage> changed)
        {
            var changedPage = changed.Behaviour;
            if (!changedPage.IsNullOrDestroyed() && !changedPage.Drawing.IsNullOrDestroyed())
            {
                changedPage.Drawing.OnDataPageChanged(changed);
            }
        }

        private IEnumerable<Vector3> GetRawPointsRange(int startIndex, int count)
        {
            var drawnPoints = DrawnPoints.Select(x => x.Position).ToList();
            var drawnPointsCount = drawnPoints.Count;
            // var drawnPointsCount = DrawnPoints.Count();
            // var drawnPointsCount = drawnPoints.Count;

            if (startIndex >= drawnPointsCount)
            {
                return null;
            }

            var sampleToCountDelta = drawnPointsCount - (startIndex + count);
            var adjustedCount = sampleToCountDelta > 0 ? count : count + sampleToCountDelta;

            return drawnPoints.GetRange(startIndex, adjustedCount);

        }

        private void AddDrawnPoint(Vector3 drawnPoint)
        {
            DrawnPoints.Add(new NetworkedStrokeDrawnPointData(drawnPoint));
        }

        public bool TryAddStrokeDrawnPoint(Draw3D_BaseStrokeData stroke, Vector3 drawnPoint)
        {
            // Debug.LogError($"Draw3D_NetworkedDrawingDataPage - TryAddStrokeDrawnPoint - Stroke Index: {stroke.StrokeIndex}, Brush Index: {brushIndex}");

            if (DrawnPoints.Count < PAGE_SIZE)
            {
                if (StrokePageData.ContainsKey(stroke.StrokeIndex))
                {
                    // Debug.LogError($"Draw3D_NetworkedDrawingDataPage - TryAddStrokeDrawnPoint - AddToCount - Stroke Index: {stroke.StrokeIndex}, Brush Index: {brushIndex}");
                    var strokePageData = StrokePageData[stroke.StrokeIndex];
                    strokePageData.Count++;
                    StrokePageData.Set(stroke.StrokeIndex, strokePageData);
                }
                else
                {
                    if (StrokePageData.Count < MAX_STROKE_COUNT)
                    {
                        var startIndex = DrawnPoints.Count;

                        StrokePageData.Add(stroke.StrokeIndex,
                            new NetworkedStrokePageData(stroke.PaletteColorIndex, stroke.BrushIndex, startIndex));
                    }
                    else
                    {
                        return false;
                    }
                }

                AddDrawnPoint(drawnPoint);

                return true;
            }

            return false;
        }

        public bool TryEndStroke()
        {
            // Debug.LogError($"Draw3D_NetworkedDrawingDataPage - TryEndStroke - Stroke Index");

            return true;

            // if (DrawnPoints.Count < PAGE_SIZE)
            // {
            //     AddDrawnPoint(Vector3.zero, Draw3D_BrushManager.INVALID_BRUSH_INDEX);
            //
            //     return true;
            // }
            //
            // return false;
        }
    }
}
