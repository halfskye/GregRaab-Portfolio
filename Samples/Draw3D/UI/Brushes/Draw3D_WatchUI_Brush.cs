using UnityEngine;
using UnityEngine.UI;

namespace Emerge.Home.Experiments.Draw3D.UI
{
    public class Draw3D_WatchUI_Brush : MonoBehaviour
    {
        [SerializeField] private Image activeHighlight = null;

        public void SetHighlightActive(bool isActive)
        {
            activeHighlight.gameObject.SetActive(isActive);
        }
    }
}
