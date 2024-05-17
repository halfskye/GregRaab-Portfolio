using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Palettes
{
    [CreateAssetMenu(fileName = "Draw3D_PaletteManagerSettings", menuName = "Experiments/Draw3D/New PaletteManagerSettings", order = 0)]
    public class Draw3D_PaletteManagerSettings : ScriptableObject
    {
        //@TODO: Turn this into an indexed map of palettes...
        // [SerializeField] private Dictionary<int, Draw3D_Palette> _palettes = new Dictionary<int, Draw3D_Palette>();
        // public Dictionary<int, Draw3D_Palette> Palettes => _palettes;

        [SerializeField] private List<Draw3D_Palette> _palettes = new List<Draw3D_Palette>();
        public List<Draw3D_Palette> Palettes => _palettes;

        [SerializeField] private int _defaultPaletteIndex = 0;
        public int DefaultPaletteIndex => _defaultPaletteIndex;

        [SerializeField] private int _defaultPaletteColorIndex = 0;
        public int DefaultPaletteColorIndex => _defaultPaletteColorIndex;

        public bool IsPaletteIndexValid(int index)
        {
            return (index >= 0 && index < Palettes.Count);
        }

        #if UNITY_EDITOR
        [Button("Set All Colors Alpha")]
        private void SetAllColorsAlpha()
        {
            const float alpha = 1.0f;
            _palettes.ForEach(x => x.SetAllColorsAlpha(alpha));

            EditorUtility.SetDirty(this);
        }
        #endif //UNITY_EDITOR

        #region Eraser

        [SerializeField] private Material _eraserMaterial = null;
        public Material EraserMaterial => _eraserMaterial;

        [SerializeField] private Color _eraserColor = Color.gray;
        public Color EraserColor => _eraserColor;

        #endregion Eraser
    }
}
