using System;
using System.Linq;
using Draw3D.Palettes;
using Tracking;
using UnityEngine;

namespace Draw3D.Brushes
{
    public class Draw3D_BrushManager : MonoBehaviour
    {
        public const int INVALID_BRUSH_INDEX = -1;

        [SerializeField] private Draw3D_BrushManagerSettings _brushSettings = null;
        //@TODO: Add concept of available brushes (subset of total Brush Settings)

        private float _strokeSampleTimer = 0f;
        private static readonly Vector3 InvalidSamplePosition = Vector3.negativeInfinity;
        private Vector3 _lastSampledPosition = InvalidSamplePosition;
        private bool IsLastSampledPositionValid() => !_lastSampledPosition.Approximately(InvalidSamplePosition);

        public static bool IsEraserActive { get; private set; } = false;

        public bool IsBrushIndexValid(int index)
        {
            return _brushSettings.IsBrushIndexValid(index);
        }

        public static Action OnBrushSampled;

        public static Action<int> OnBrushChange;

        public static Action OnEraserActive;
        public static Action OnEraserInactive;

        #region Singleton

        public static Draw3D_BrushManager Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_BrushManager already exists. There should only be one.");
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            Draw3D_Manager.OnStrokeStart += OnStrokeStart;

            OnBrushChange += OnChangeNonEraser;
            Draw3D_PaletteManager.OnPaletteChange += OnChangeNonEraser;
            Draw3D_PaletteManager.OnPaletteColorChange += OnChangeNonEraser;
        }

        private void OnDestroy()
        {
            Draw3D_Manager.OnStrokeStart -= OnStrokeStart;

            OnBrushChange -= OnChangeNonEraser;
            Draw3D_PaletteManager.OnPaletteChange -= OnChangeNonEraser;
            Draw3D_PaletteManager.OnPaletteColorChange -= OnChangeNonEraser;
        }

        #endregion Singleton

        private void Start()
        {
            SelectBrushByIndex(_brushSettings.DefaultBrushIndex);
        }

        public int TotalBrushCount => _brushSettings.Brushes.Count;
        public Draw3D_Brush GetBrushByIndex(int index)
        {
            if (index >= 0 && index < TotalBrushCount)
            {
                return _brushSettings.Brushes[index];
            }

            Debug.LogError($"Brush index is out of bounds: {index} (Total Brush Count: {TotalBrushCount})");
            return null;
        }

        public int SelectedBrushIndex { get; private set; } = 0;
        public Draw3D_Brush SelectedBrush => _brushSettings.Brushes[SelectedBrushIndex];

        private Draw3D_Brush ChangeBrush(int brushIndex)
        {
            SelectedBrushIndex = brushIndex % _brushSettings.Brushes.Count;
            if (SelectedBrushIndex < 0)
            {
                SelectedBrushIndex += _brushSettings.Brushes.Count;
            }

            OnBrushChange?.Invoke(SelectedBrushIndex);

            return SelectedBrush;
        }
        public Draw3D_Brush SelectNextAvailableBrush()
        {
            return ChangeBrush(SelectedBrushIndex + 1);
        }
        public Draw3D_Brush SelectPreviousAvailableBrush()
        {
            return ChangeBrush(SelectedBrushIndex - 1);
        }
        public Draw3D_Brush SelectBrushByIndex(int brushIndex)
        {
            if (!IsBrushIndexValid(brushIndex))
            {
                Debug.LogError($"Invalid Brush Index ({brushIndex}) specified");
                return SelectedBrush;
            }

            return ChangeBrush(brushIndex);
        }

        private void OnStrokeStart()
        {
            _strokeSampleTimer = 0f;
            _lastSampledPosition = InvalidSamplePosition;
        }

        public void UpdateSelectedBrushSample(Vector3 drawingPoint)
        {
            // Throttle by time first
            _strokeSampleTimer += Time.deltaTime;

            if (_strokeSampleTimer > GetSampleMinTime())
            {
                _strokeSampleTimer = 0f;

                // Throttle by distance next
                if (!IsLastSampledPositionValid() ||
                    MathUtils.IsDistanceBetweenPointsGreaterThan(
                        drawingPoint,
                        _lastSampledPosition,
                        GetSampleMinDistance()
                    )
                   )
                {
                    _lastSampledPosition = drawingPoint;

                    OnBrushSampled?.Invoke();
                }
            }
        }

        private float GetSampleMinTime()
        {
            return IsEraserActive ? _brushSettings.EraserSampleMinTime : SelectedBrush.SampleMinTime;
        }

        private float GetSampleMinDistance()
        {
            return IsEraserActive ? _brushSettings.EraserSampleMinDistance : SelectedBrush.SampleMinDistance;
        }

        #region Eraser

        public void ToggleEraser()
        {
            if (IsEraserActive)
            {
                DeactivateEraser();
            }
            else
            {
                ActivateEraser();
            }
        }

        private void ActivateEraser()
        {
            if (!IsEraserActive)
            {
                IsEraserActive = true;

                OnEraserActive?.Invoke();
            }
        }

        private void DeactivateEraser()
        {
            if (IsEraserActive)
            {
                IsEraserActive = false;

                OnEraserInactive?.Invoke();
            }
        }

        private void OnChangeNonEraser(int _)
        {
            DeactivateEraser();
        }

        public bool EraserSample(Vector3 eraserPosition)
        {
            var erased = false;

            var drawing = Draw3D_Manager.Instance.CurrentDrawing;
            if (drawing.IsNullOrDestroyed())
            {
                return erased;
            }

            var eraserRadius = _brushSettings.EraserSampleRadius;
            var networkedDrawingData = drawing.DrawingDataManager as Draw3D_NetworkedDrawingDataManager;
            foreach (var strokeData in networkedDrawingData.StrokeData.Values)
            {
                if (!strokeData.IsErased &&
                    strokeData.DataPoints.Any(x => MathUtils.IsDistanceBetweenPointsLessThan(x, eraserPosition, eraserRadius)))
                {
                    var networkedStrokeData = strokeData as Draw3D_NetworkedStrokeData;
                    networkedDrawingData.Drawing.EraseStroke(networkedStrokeData);

                    Draw3D_Manager.Instance.AddStrokeDataToUpdate(networkedStrokeData);

                    erased = true;
                }
            }

            return erased;
        }

        #endregion // Eraser
    }
}
