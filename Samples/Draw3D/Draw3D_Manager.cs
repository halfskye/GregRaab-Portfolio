using System;
using System.Collections.Generic;
using Emerge.Home.Experiments.Draw3D.Brushes;
using Emerge.Home.Experiments.Draw3D.Displays;
using Emerge.Home.Experiments.Draw3D.GestureDetection;
using Emerge.Home.Experiments.Draw3D.Minigames;
using Emerge.Home.Experiments.Draw3D.Palettes;
using Emerge.SDK.Core.Tracking;
using EmergeHome.Code.Core;
using EmergeHome.Code.Environments;
using EmergeHome.Code.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Emerge.Home.Experiments.Draw3D
{
    public class Draw3D_Manager : MonoBehaviour
    {
        private const bool USE_NETWORKED_DRAWINGS = true;

        [SerializeField] private Draw3D_BrushManager _brushManager = null;
        [SerializeField] private Draw3D_PaletteManager _paletteManager = null;
        [SerializeField] private Draw3D_GestureDetectionManager _gestureDetectionManager = null;
        [SerializeField] private Draw3D_Renderer _renderer = null;
        [SerializeField] private Draw3D_DisplayManager _displayManager = null;

        [SerializeField] private Draw3D_Drawing _baseDrawingPrefab = null;

        [SerializeField] private Draw3D_NetworkedDrawing _networkedDrawingPrefab = null;

        public static bool IsDraw3DValid => IsSeatedAtDraw3DTable;
        private static bool IsSeatedAtDraw3DTable { get; set; } = false;
        private const int DRAW3D_TABLE_INDEX = 1;
        public static bool GetIsSeatedAtDraw3DTable() { return (DRAW3D_TABLE_INDEX == TableManager.Instance.LocalSeatData.TableIndex); }
        public const bool UseGestureShortcuts = false;

        private Draw3D_Drawing _currentDrawing = null;
        public Draw3D_Drawing CurrentDrawing => _currentDrawing;
        private bool _isDrawingStroke = false;
        private Draw3D_Drawing _lastDrawing = null;

        private Color CurrentColor => _paletteManager.SelectedPaletteColor;
        private int CurrentPaletteIndex => _paletteManager.SelectedPaletteIndex;
        private int CurrentPaletteColorIndex => _paletteManager.SelectedPaletteColorIndex;

        private int CurrentBrushIndex => _brushManager.SelectedBrushIndex;

        private Vector3 PlayerEye => Camera.main.transform.position;
        private Vector3 DrawingPoint => _gestureDetectionManager.DrawingPoint.position;
        private Transform PaletteHand => _gestureDetectionManager.PaletteHand;

        private List<Draw3D_Drawing> _completedDrawings = new List<Draw3D_Drawing>();

        private List<Draw3D_Drawing> _remoteDrawings = new List<Draw3D_Drawing>();
        public void AddRemoteDrawing(Draw3D_Drawing drawing) { _remoteDrawings.Add(drawing); }

        private List<Draw3D_NetworkedStrokeData> _strokeDataToUpdate =
            new List<Draw3D_NetworkedStrokeData>();
        public void AddStrokeDataToUpdate(Draw3D_NetworkedStrokeData strokeData)
        {
            _strokeDataToUpdate.Add(strokeData);
        }

        public static event Action OnStrokeStart;
        public static event Action OnStrokeUpdate;
        public static event Action OnStrokeEnd;

        public static Draw3D_Manager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_Manager already exists. There should only be one.");
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            _gestureDetectionManager.OnStrokeStart += StrokeStart;
            _gestureDetectionManager.OnStrokeEnd += StrokeEnd;

            _gestureDetectionManager.OnPaletteChangeGesture += OnPaletteChangeGesture;
            _gestureDetectionManager.OnPaletteColorChangeGesture += OnPaletteColorChangeGesture;

            Draw3D_BrushManager.OnBrushSampled += OnBrushSampled;

            Draw3D_PaletteManager.OnPaletteChange += OnPaletteChange;

            TableManager.OnFirstSeatAssigned += TableManagerOnFirstSeatAssigned;
            TableManager.OnSeatChanged += TableManagerOnSeatChanged;
            ApplicationManager.Instance.FusionSceneManager.OnSceneLoadComplete += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            _gestureDetectionManager.OnStrokeStart -= StrokeStart;
            _gestureDetectionManager.OnStrokeEnd -= StrokeEnd;

            _gestureDetectionManager.OnPaletteChangeGesture -= OnPaletteChangeGesture;
            _gestureDetectionManager.OnPaletteColorChangeGesture -= OnPaletteColorChangeGesture;

            Draw3D_BrushManager.OnBrushSampled -= OnBrushSampled;

            Draw3D_PaletteManager.OnPaletteChange -= OnPaletteChange;

            TableManager.OnSeatChanged -= TableManagerOnSeatChanged;
            TableManager.OnFirstSeatAssigned -= TableManagerOnFirstSeatAssigned;
            ApplicationManager.Instance.FusionSceneManager.OnSceneLoadComplete -= OnSceneLoaded;
        }

        private void Update()
        {
            StrokeUpdate();

            Render();

            UpdateDebugInput();
        }

        //On first seat assigned
        private void TableManagerOnFirstSeatAssigned(int tableIndex,int seatIndex)
        {
            HandleSeatChanged();
        }

        private void TableManagerOnSeatChanged(int tableIndex, int seatIndex)
        {
            HandleSeatChanged();
        }

        private void HandleSeatChanged()
        {
            IsSeatedAtDraw3DTable = GetIsSeatedAtDraw3DTable();
        }

        private void OnSceneLoaded(Scenes scene)
        {
            IsSeatedAtDraw3DTable = false;
        }

        private void PublishCurrentDrawing()
        {
            if (_currentDrawing != null)
            {
                DebugLogError($"Publishing current Drawing ({_currentDrawing.ToString()})");

                _completedDrawings.Add(_currentDrawing);

                _displayManager.DisplayDrawing(_currentDrawing);

                _currentDrawing = null;
            }
        }

        public void DestroyCurrentDrawing()
        {
            if (!_currentDrawing.IsNullOrDestroyed())
            {
                var networkedDrawingData = _currentDrawing.DrawingDataManager as Draw3D_NetworkedDrawingDataManager;

                if (networkedDrawingData != null)
                {
                    ApplicationManager.Instance.Runner.Despawn(networkedDrawingData.Drawing.Object);
                }
                else
                {
                    GameObject.Destroy(_currentDrawing.gameObject);
                }
            }

            _currentDrawing = null;
            _isDrawingStroke = false;
        }

        private bool CanDraw()
        {
            return IsSeatedAtDraw3DTable &&
                   HandsManagerHelper.Instance.HandPresenceCheck(Chirality.Right) &&
                   Draw3D_MinigamesManager.Instance.CanDraw();
        }

        #region Stroke Start

        private void StrokeStart()
        {
            //@TODO: Deal w/ edge cases:
            if (!CanDraw())
            {
                return;
            }

            OnStrokeStart?.Invoke();

            _isDrawingStroke = true;

            if (!Draw3D_BrushManager.IsEraserActive)
            {
                StartDrawing();
            }
        }

        private void StartDrawing()
        {
            if (_currentDrawing == null)
            {
                var position = DrawingPoint;
                var playerFacing = Quaternion.LookRotation(position - PlayerEye, Vector3.up);
                playerFacing.x = playerFacing.z = 0f;

                if (USE_NETWORKED_DRAWINGS)
                {
                    var networkedDrawing =
                        ApplicationManager.Instance.Runner.Spawn(_networkedDrawingPrefab, position, playerFacing);

                    _currentDrawing = networkedDrawing.Drawing;

                    networkedDrawing.StartDrawing(CurrentPaletteIndex);
                }
                else
                {
                    _currentDrawing = Instantiate(_baseDrawingPrefab, position, playerFacing, this.transform);
                    _currentDrawing.SetDrawingDataManager(new Draw3D_DrawingDataManager());

                    _currentDrawing.StartDrawing(CurrentPaletteIndex);
                }
            }

            _lastDrawing = _currentDrawing;

            _currentDrawing.StartStroke(CurrentPaletteColorIndex, CurrentBrushIndex, DrawingPoint);
        }

        #endregion Stroke Start

        #region Stroke Update

        private void StrokeUpdate()
        {
            if (!CanDraw())
            {
                return;
            }

            if (_isDrawingStroke)
            {
                _brushManager.UpdateSelectedBrushSample(DrawingPoint);
            }
        }

        private void OnBrushSampled()
        {
            var sampled = false;
            if (Draw3D_BrushManager.IsEraserActive)
            {
                sampled = _brushManager.EraserSample(DrawingPoint);
            }
            else if(!_currentDrawing.IsNullOrDestroyed())
            {
                _currentDrawing.AddNewDrawnPoint(DrawingPoint);
                sampled = true;
            }

            if (sampled)
            {
                OnStrokeUpdate?.Invoke();
            }
        }

        #endregion Stroke Update

        public void StrokeEnd()
        {
            //@TODO: Deal w/ edge cases:
            if (!CanDraw())
            {
                return;
            }

            if (_isDrawingStroke)
            {
                OnStrokeEnd?.Invoke();

                if (!Draw3D_BrushManager.IsEraserActive &&
                    !_currentDrawing.IsNullOrDestroyed())
                {
                    _currentDrawing.EndDrawing(DrawingPoint);
                }

                _isDrawingStroke = false;
            }
        }

        // public void UndoStroke()
        // {
        //     _lastDrawing?.UndoLastStroke();
        // }

        private void SelectNextAvailableBrush()
        {
            _brushManager.SelectNextAvailableBrush();
        }

        private void OnPaletteChange(int paletteIndex)
        {
            if (_currentDrawing != null)
            {
                _currentDrawing.ChangePalette(paletteIndex);
            }
        }

        private void OnPaletteChangeGesture()
        {
            SelectNextAvailablePalette();
        }

        private void SelectNextAvailablePalette()
        {
            _paletteManager.SelectNextAvailablePalette();
        }

        private void OnPaletteColorChangeGesture()
        {
            SelectNextAvailablePaletteColor();
        }

        private void SelectNextAvailablePaletteColor()
        {
            _paletteManager.SelectNextAvailablePaletteColor();
        }

        #region Rendering

        private void Render()
        {
            RenderDrawings();

            if (_currentDrawing != null && _currentDrawing.CurrentStroke != null)
            {
                _renderer.UpdateStrokeRenderer(_currentDrawing, _brushManager.SelectedBrush, _currentDrawing.CurrentStroke);
            }
        }

        private void RenderDrawings()
        {
            _completedDrawings.ForEach(drawing => _renderer.RenderDrawing(drawing));

            UpdateChangedStrokeRenderers();
            _remoteDrawings.ForEach(drawing => _renderer.RenderDrawing(drawing));
        }

        private void UpdateChangedStrokeRenderers()
        {
            _strokeDataToUpdate.ForEach(strokeData =>
                {
                    if (!strokeData.Drawing.IsNullOrDestroyed() && !strokeData.Drawing.Drawing.IsNullOrDestroyed())
                    {
                        if (strokeData.IsErased)
                        {
                            _renderer.EraseStroke(
                                strokeData.Drawing.Drawing,
                                strokeData
                            );
                        }
                        else
                        {
                            _renderer.UpdateStrokeRenderer(
                                strokeData.Drawing.Drawing,
                                strokeData.Brush,
                                strokeData);
                        }
                    }
                }
            );

            _strokeDataToUpdate.Clear();
        }

        #endregion Rendering

        private void UpdateDebugInput()
        {
            #if DEBUG

            if (Input.GetKeyDown(KeyCode.B))
            {
                SelectNextAvailableBrush();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                SelectNextAvailablePalette();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                SelectNextAvailablePaletteColor();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                PublishCurrentDrawing();
            }

            #endif //DEBUG
        }

        public static void DebugLogError(string message, Object context=null)
        {
            #if DEBUG

            DebugLogUtilities.LogError(DebugLogUtilities.DebugLogType.DRAW_3D, message, context);

            #endif //DEBUG
        }
    }
}
