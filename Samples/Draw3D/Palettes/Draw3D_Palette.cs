using UnityEditor;
using UnityEngine;

namespace Draw3D.Palettes
{
    [CreateAssetMenu(fileName = "Draw3D_Palette", menuName = "Experiments/Draw3D/New Palette", order = 0)]
    public class Draw3D_Palette : ScriptableObject
    {
        public const int PALETTE_COLORS_COUNT = 5;
        private const string PALETTE_DEFAULT_NAME = "Palette";
        public static Color UNINITIALIZED_COLOR = Color.magenta;

        [SerializeField] private string _displayName = PALETTE_DEFAULT_NAME;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? PALETTE_DEFAULT_NAME : _displayName;

        [SerializeField] private Shader _shader = null;
        public Shader Shader => _shader;

        [SerializeField]
        private Material _material = null;
        public Material Material
        {
            get
            {
                if (_material == null)
                {
                    _material = _shader != null ? new Material(_shader) : Draw3D_PaletteManager.Instance.DefaultMaterial;
                }
                return _material;
            }
        }

        [SerializeField]
        private Color[] _colors = new Color[PALETTE_COLORS_COUNT];
        public int ColorCount => _colors.Length;

        public static bool IsColorIndexValid(int index)
        {
            return (index >= 0 && index < PALETTE_COLORS_COUNT);
        }

        public Color GetColorSafe(int index)
        {
            return IsColorIndexValid(index) ? _colors[index] : UNINITIALIZED_COLOR;
        }

        public Color this[int index] => GetColorSafe(index);

        private void OnValidate()
        {
            var colorCount = _colors.Length;
            const int paletteTemplateColorCount = PALETTE_COLORS_COUNT;

            if (colorCount != paletteTemplateColorCount)
            {
                Debug.LogError($"Draw3D_Palette '{this.name}' color count is incorrect. Has {colorCount}, needs {paletteTemplateColorCount}.");
            }
        }

        #if UNITY_EDITOR
        public void SetAllColorsAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            for (var i = 0; i < ColorCount; i++)
            {
                var newColor = _colors[i];
                newColor.a = alpha;
                _colors[i] = newColor;
            }

            EditorUtility.SetDirty(this);
        }
        #endif //UNITY_EDITOR
    }
}
