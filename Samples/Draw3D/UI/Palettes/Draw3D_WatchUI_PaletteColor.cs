using Draw3D.Palettes;
using UnityEngine;
using UnityEngine.UI;
using XRTK.Extensions;

namespace Draw3D.UI
{
    public class Draw3D_WatchUI_PaletteColor : MonoBehaviour
    {
        [SerializeField] private Image color = null;
        [SerializeField] private Image activeHighlight = null;

        public void SetPaletteColor(Draw3D_Palette palette, int colorIndex)
        {
            if (!color.material.IsNotNull())
            {
                Destroy(color.material);
                color.material = null;
            }

            color.material = new Material(palette.Material)
            {
                color = palette.GetColorSafe(colorIndex)
            };
        }

        public void SetHighlightActive(bool isActive)
        {
            activeHighlight.gameObject.SetActive(isActive);
        }
    }
}
