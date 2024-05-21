using System.Collections.Generic;
using UnityEngine;

namespace Draw3D.Brushes
{
    [CreateAssetMenu(fileName = "Draw3D_BrushManagerSettings", menuName = "Experiments/Draw3D/New BrushManagerSettings", order = 0)]
    public class Draw3D_BrushManagerSettings : ScriptableObject
    {
        //@TODO: Turn this into an indexed map of brushes...
        // [SerializeField] private Dictionary<int, Draw3D_Brush> _brushes = new Dictionary<int, Draw3D_Brush>();
        // public Dictionary<int, Draw3D_Brush> Brushes => _brushes;

        [SerializeField] private List<Draw3D_Brush> _brushes = new List<Draw3D_Brush>();
        public List<Draw3D_Brush> Brushes => _brushes;

        [SerializeField] private int _defaultBrushIndex = 0;
        public int DefaultBrushIndex => _defaultBrushIndex;

        public bool IsBrushIndexValid(int index)
        {
            return (index >= 0 && index < Brushes.Count);
        }

        #region Eraser

        [SerializeField] private float _eraserSampleMinTime = 0.01f;
        public float EraserSampleMinTime => _eraserSampleMinTime;

        [SerializeField] private float _eraserSampleMinDistance = 0.1f;
        public float EraserSampleMinDistance => _eraserSampleMinDistance;

        [SerializeField] private float _eraserSampleRadius = 0.1f;
        public float EraserSampleRadius => _eraserSampleRadius;

        #endregion Eraser
    }
}
