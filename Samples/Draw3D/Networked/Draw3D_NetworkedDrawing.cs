using System.Collections.Generic;
using System.Linq;
using Tracking;
using Fusion;
using UnityEngine;

namespace Draw3D
{
    public class Draw3D_NetworkedDrawing : NetworkBehaviour
    {
        [SerializeField] private Draw3D_Drawing _baseDrawingPrefab = null;

        [SerializeField] private Draw3D_NetworkedDrawingDataPage _dataPagePrefab = null;

        public Draw3D_Drawing Drawing { get; private set; } = null;

        [Networked(OnChanged = nameof(OnPaletteIndexChanged))]
        public int PaletteIndex { get; private set; }
        public void ChangePalette(int paletteIndex) { PaletteIndex = paletteIndex; }

        private Dictionary<int, Draw3D_NetworkedDrawingDataPage> DataPages { get; } =
            new Dictionary<int, Draw3D_NetworkedDrawingDataPage>();

        // private int _currentDataPageIndex = -1;

        public override void Spawned()
        {
            base.Spawned();

            var thisTransform = this.transform;
            Drawing = Instantiate(_baseDrawingPrefab, thisTransform.position, thisTransform.rotation, thisTransform);

            if (Object.HasStateAuthority)
            {
                AddNewDataPage();
            }
            else
            {
                StartDrawing(PaletteIndex);

                Draw3D_Manager.Instance.AddRemoteDrawing(Drawing);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Destroy(Drawing.gameObject);

            base.Despawned(runner, hasState);
        }

        public void StartDrawing(int paletteIndex)
        {
            PaletteIndex = paletteIndex;

            Drawing.SetDrawingDataManager(new Draw3D_NetworkedDrawingDataManager(this));
            Drawing.StartDrawing(paletteIndex);
        }

        private Draw3D_NetworkedDrawingDataPage AddNewDataPage()
        {
            var thisTransform = this.transform;
            var dataPage = Runner.Spawn(_dataPagePrefab, thisTransform.position, thisTransform.rotation);

            // var pageIndex = ++_currentDataPageIndex;
            var pageIndex = DataPages.Count;
            dataPage.InitializeData(this, pageIndex);

            RegisterDataPage(dataPage);

            return dataPage;
        }

        private void RegisterDataPage(Draw3D_NetworkedDrawingDataPage dataPage)
        {
            var pageIndex = dataPage.PageIndex;
            if (!DataPages.ContainsKey(pageIndex))
            {
                DataPages.Add(pageIndex, dataPage);
                // _currentDataPageIndex = Math.Max(_currentDataPageIndex, pageIndex);

                dataPage.transform.SetParent(this.transform);
            }
        }

        public void OnDataPageChanged(Changed<Draw3D_NetworkedDrawingDataPage> changed)
        {
            var changedPage = changed.Behaviour;
            var changedPageObject = changedPage.Object;

            // Only receive data from Players that aren't us:
            if (!changedPageObject.HasStateAuthority)
            {
                RegisterDataPage(changedPage);

                // Load Old and New states to compare exact relevant delta data:
                changed.LoadOld();
                var oldStrokePageData = changedPage.StrokePageData;
                changed.LoadNew();
                var newStrokePageData = changedPage.StrokePageData;

                var strokePageDataDeltas = newStrokePageData.Where(
                        x =>
                            !oldStrokePageData.ContainsKey(x.Key) ||
                            oldStrokePageData[x.Key] != newStrokePageData[x.Key]
                    )
                    .ToList();

                strokePageDataDeltas.ForEach(x =>
                {
                    var strokeIndex = x.Key;
                    var strokePageData = x.Value;

                    DebugLogError($"Draw3D_NetworkedDrawing - OnDataPageChanged - Stroke Index: {strokeIndex}, Start: {strokePageData.StartIndex}, Count: {strokePageData.Count}, Brush: {strokePageData.BrushIndex}, Color: {strokePageData.PaletteColorIndex}");

                    if (!Drawing.DrawingDataManager.StrokeData.ContainsKey(strokeIndex))
                    {
                        DebugLogError($"Draw3D_NetworkedDrawing - OnDataPageChanged - Stroke Index: {strokeIndex} not found, adding...");

                        Drawing.DrawingDataManager.StrokeData.Add(strokeIndex,
                            new Draw3D_NetworkedStrokeData(
                                this,
                                strokeIndex,
                                strokePageData.PaletteColorIndex,
                                strokePageData.BrushIndex
                            )
                        );
                    }

                    Drawing.DrawingDataManager.StrokeData[strokeIndex].UpdateData(
                        strokePageData.PaletteColorIndex,
                        strokePageData.BrushIndex
                    );

                    Draw3D_Manager.Instance.AddStrokeDataToUpdate(
                        Drawing.DrawingDataManager.StrokeData[strokeIndex] as Draw3D_NetworkedStrokeData
                    );
                });
            }
        }

        public void AddStrokeDrawnPoint(Draw3D_BaseStrokeData stroke, Vector3 drawnPoint)
        {
            DebugLogError("Draw3D_NetworkedDrawing - AddStrokeDrawnPoint");

            // if (DataPages[_currentDataPageIndex].TryAddStrokeDrawnPoint(stroke, drawnPoint, brushIndex))
            if (DataPages.Last().Value.TryAddStrokeDrawnPoint(stroke, drawnPoint))
            {
                return;
            }

            var newDataPage = AddNewDataPage();
            newDataPage.TryAddStrokeDrawnPoint(stroke, drawnPoint);
        }

        public void EndStroke()
        {
            if (DataPages.Last().Value.TryEndStroke())
            {
                return;
            }

            var newDataPage = AddNewDataPage();
            newDataPage.TryEndStroke();
        }

        public List<Vector3> GetStrokeDrawnPoints(Draw3D_BaseStrokeData stroke)
        {
            var drawnPoints = new List<Vector3>();
            foreach (var pageDrawnPoints in DataPages.Values.Select(dataPage => dataPage.GetStrokeDrawnPoints(stroke)))
            {
                drawnPoints.AddRange(pageDrawnPoints);
            }
            return drawnPoints;
        }

        public static void OnPaletteIndexChanged(Changed<Draw3D_NetworkedDrawing> changedDrawing)
        {
            if (!changedDrawing.Behaviour.IsNullOrDestroyed())
            {
                changedDrawing.Behaviour.OnPaletteIndexChanged();
            }
        }

        private void OnPaletteIndexChanged()
        {
            Drawing.ChangePalette(PaletteIndex);
        }

        public void EraseStroke(Draw3D_NetworkedStrokeData strokeData)
        {
            foreach (var dataPage in DataPages.Values)
            {
                if (dataPage.StrokePageData.TryGet(strokeData.StrokeIndex, out var strokePageData))
                {
                    strokePageData.EraseStroke();
                    dataPage.StrokePageData.Set(strokeData.StrokeIndex, strokePageData);
                }
            }

            strokeData.Erase();
        }

        private void DebugLogError(string message)
        {
            #if DEBUG

            // Draw3D_Manager.DebugLogError(message, this);

            #endif //DEBUG
        }
    }
}
