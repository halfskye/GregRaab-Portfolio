using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Brushes
{
    [CreateAssetMenu(fileName = "Draw3D_Brush", menuName = "Experiments/Draw3D/New Brush", order = 0)]
    public class Draw3D_Brush : ScriptableObject
    {
        private const string BRUSH_DEFAULT_NAME = "Brush";
        [SerializeField] private string _displayName = BRUSH_DEFAULT_NAME;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? BRUSH_DEFAULT_NAME : _displayName;

        [SerializeField] private float _width = 0.01f;
        public float Width => _width;

        [Header("Sample Throttles")]
        [SerializeField] private float _sampleMinDistance = 0.05f;
        public float SampleMinDistance => _sampleMinDistance;
        [SerializeField] private float _sampleMinTime = 0.01f;
        public float SampleMinTime => _sampleMinTime;

        [Header("Width Curve (optional)")]
        [SerializeField] private bool _useWidthCurve = true;
        public bool UseWidthCurve => _useWidthCurve;

        [SerializeField] private float _minWidth = 0.005f;
        public float MinWidth => _minWidth;
        [SerializeField] private float _maxWidth = 0.03f;
        public float MaxWidth => _maxWidth;

        [Header("Width Curve - Sample Distance Config")]
        [SerializeField] private float _sampleDistancePerSecondThreshold = 0.1f;
        public float SampleDistancePerSecondThreshold => _sampleDistancePerSecondThreshold;

        [Header("Tactility")]
        [SerializeField] private Draw3D_BrushTactility _activeTactility = null;
        public Draw3D_BrushTactility ActiveTactility => _activeTactility;
        [SerializeField] private float _activeTactilityDuration = 0.1f;
        public float ActiveTactilityDuration => _activeTactilityDuration;
        [SerializeField] private Draw3D_BrushTactility _idleTactility = null;
        public Draw3D_BrushTactility IdleTactility => _idleTactility;
    }
}
