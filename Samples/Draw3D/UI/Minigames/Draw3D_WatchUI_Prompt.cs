using TMPro;
using UnityEngine;
using XRTK.Extensions;

namespace Emerge.Home.Experiments.Draw3D.UI.Minigames
{
    public class Draw3D_WatchUI_Prompt : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text = null;

        public void SetActive(bool isActive)
        {
            this.transform.SetActive(isActive);
        }

        public void SetText(string text)
        {
            _text.text = text;
        }

        public string GetText()
        {
            return _text.text;
        }
    }
}
