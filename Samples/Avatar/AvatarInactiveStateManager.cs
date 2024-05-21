using Cloud;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Normal.Realtime;
using System.Threading.Tasks;

namespace Avatar
{
    public class AvatarInactiveStateManager : MonoBehaviour
    {
        [SerializeField] Image avatarImage;
        [SerializeField] TextMeshProUGUI textUI;
        [SerializeField] GameObject inactiveStateUI;

        [SerializeField] private float updateRate = 0.2f;
        private float _updateTimer = 0f;
        
        private bool _isLocal = false;
        
        public void InitUIManger(string msg=null)
        {
            if (!String.IsNullOrEmpty(msg))
                textUI.text = msg;
        }

        private void OnEnable()
        {
            _isLocal = this.gameObject.GetComponent<RealtimeView>().isOwnedLocallyInHierarchy;
            if (_isLocal)
            {
                SetUILayerEnabled(true);
                SetAwayUiPosition();
            }
        }

        private void OnDisable()
        {
            if (_isLocal)
            {
                SetUILayerEnabled(false);
            }
        }

        private void SetUILayerEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                Camera.main.cullingMask &= ~LayerMask.GetMask("AwayUI");
            }
            else
            {
                Camera.main.cullingMask |= LayerMask.GetMask("AwayUI");
            }
        }

        private void SetAwayUiPosition()
        {
            var thisTransform = this.transform;
            thisTransform.rotation = TactileCore.HardwareVisualizer.HardwareConfiguration.rotation;
            thisTransform.position = Camera.main.transform.position;
            this.transform.Translate(new Vector3(0, 0, -0.1f));
        }

        public void SetAvatarInactiveUiActive(bool isActive)
        {
            inactiveStateUI.SetActive(isActive);
        }

        public async void SetAvatarImage(string imageURL=null)
        {
            if(!String.IsNullOrEmpty(imageURL))
            {
                Texture2D texture = await GetAvatarImageAsync(imageURL);
                avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
        }
        private async Task<Texture2D> GetAvatarImageAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;
            
            Texture2D Image = await Rest.DownloadTextureAsync(url);
            return Image;
        }
    }
}
