using System;
using Draw3D.GestureDetection;
using UnityEngine;

namespace Draw3D.Palettes
{
    //@TODO: Index of Palettes available, controlled by a ScriptableObject?
    public class Draw3D_PaletteManager : MonoBehaviour
    {
        public const int INVALID_PALETTE_INDEX = -1;
        public const int INVALID_COLOR_INDEX = -1;

        [SerializeField] private Draw3D_PaletteManagerSettings _paletteSettings = null;
        //@TODO: Add concept of available palettes (subset of total Palette Settings)

        private bool IsPaletteIndexValid(int index)
        {
            return _paletteSettings.IsPaletteIndexValid(index);
        }

        [SerializeField] private Material _defaultMaterial = null;
        public Material DefaultMaterial
        {
            get
            {
                if (_defaultMaterial == null)
                {
                    _defaultMaterial = new Material(DefaultShader);
                }

                return _defaultMaterial;
            }
        }

        private const string DEFAULT_SHADER_NAME = "Sprites/Default";
        [SerializeField] private Shader _defaultShader = null;
        public Shader DefaultShader
        {
            get
            {
                if (_defaultShader == null)
                {
                    _defaultShader = Shader.Find(DEFAULT_SHADER_NAME);
                }

                return _defaultShader;
            }
        }

        public Material EraserMaterial
        {
            get
            {
                if (_paletteSettings.EraserMaterial == null)
                {
                    return DefaultMaterial;
                }

                return _paletteSettings.EraserMaterial;
            }
        }
        public Color EraserColor => _paletteSettings.EraserColor;

        public static Action<int> OnPaletteChange;
        public static Action<int> OnPaletteColorChange;

        #region Singleton

        public static Draw3D_PaletteManager Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_PaletteManager already exists. There should only be one.");
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        #endregion Singleton

        private void Start()
        {
            SelectPaletteByIndex(_paletteSettings.DefaultPaletteIndex);
            SelectActivePaletteColorByIndex(_paletteSettings.DefaultPaletteColorIndex);
        }

        public int TotalPaletteCount => _paletteSettings.Palettes.Count;
        public Draw3D_Palette GetPaletteByIndex(int index)
        {
            if (index >= 0 && index < TotalPaletteCount)
            {
                return _paletteSettings.Palettes[index];
            }

            Debug.LogError($"Palette index is out of bounds: {index} (Total Palette Count: {TotalPaletteCount})");
            return null;
        }

        public Color GetColor(int paletteIndex, int paletteColorIndex)
        {
            return _paletteSettings.Palettes[paletteIndex].GetColorSafe(paletteColorIndex);
        }

        public int SelectedPaletteIndex { get; private set; } = 0;
        public Draw3D_Palette SelectedPalette => _paletteSettings.Palettes[SelectedPaletteIndex];

        private Draw3D_Palette ChangePalette(int paletteIndex)
        {
            SelectedPaletteIndex = paletteIndex % _paletteSettings.Palettes.Count;
            if (SelectedPaletteIndex < 0)
            {
                SelectedPaletteIndex += _paletteSettings.Palettes.Count;
            }

            OnPaletteChange?.Invoke(SelectedPaletteIndex);

            return SelectedPalette;
        }
        public Draw3D_Palette SelectNextAvailablePalette()
        {
            return ChangePalette(SelectedPaletteIndex + 1);
        }
        public Draw3D_Palette SelectPreviousAvailablePalette()
        {
            return ChangePalette(SelectedPaletteIndex - 1);
        }
        public Draw3D_Palette SelectPaletteByIndex(int paletteIndex)
        {
            if (!IsPaletteIndexValid(paletteIndex))
            {
                Debug.LogError($"Invalid Palette Index ({paletteIndex}) specified");
                return SelectedPalette;
            }

            return ChangePalette(paletteIndex);
        }

        public int SelectedPaletteColorIndex { get; private set; } = 0;
        public Color SelectedPaletteColor => SelectedPalette.GetColorSafe(SelectedPaletteColorIndex);
        private Color ChangePaletteColor(int colorIndex)
        {
            SelectedPaletteColorIndex = colorIndex % SelectedPalette.ColorCount;
            if (SelectedPaletteColorIndex < 0)
            {
                SelectedPaletteColorIndex += SelectedPalette.ColorCount;
            }

            OnPaletteColorChange?.Invoke(SelectedPaletteColorIndex);

            return SelectedPaletteColor;
        }
        public Color SelectNextAvailablePaletteColor()
        {
            return ChangePaletteColor(SelectedPaletteColorIndex + 1);
        }
        public Color SelectPreviousAvailablePaletteColor()
        {
            return ChangePaletteColor(SelectedPaletteColorIndex - 1);
        }
        public Color SelectActivePaletteColorByIndex(int colorIndex)
        {
            if (!Draw3D_Palette.IsColorIndexValid(colorIndex))
            {
                Debug.LogError($"Invalid Color Index ({colorIndex}) specified for {SelectedPalette.DisplayName}");
                return Draw3D_Palette.UNINITIALIZED_COLOR;
            }

            return ChangePaletteColor(colorIndex);
        }

        public static Transform PaletteHand => Draw3D_GestureDetectionManager.Instance.PaletteHand;
    }
}
