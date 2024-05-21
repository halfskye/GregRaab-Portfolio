using UnityEngine;
using System;
using System.IO;

namespace Avatar
{
    public class HiResScreenshot : MonoBehaviour
    {
        public event Action<Sprite> NewPhotoTaken;

        private Camera screenShotCamera = null;
        private Camera Camera => screenShotCamera == null
            ? screenShotCamera = GetComponent<Camera>()
            : screenShotCamera;

        private Texture2D screenShotTexture;
        private RenderTexture renderTexture;
        private readonly int width = 512;
        private readonly int height = 512;

        private void OnEnable()
        {
            if (screenShotTexture == null)
            {
                screenShotTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            }
        }

        public void TakeScreenShotAsync(bool overWrite = true)
        {
            var filename = $"{Application.persistentDataPath}/profile_{width}x{height}.png";

            if (File.Exists(filename))
            {
                if (overWrite)
                {
                    File.Delete(filename);
                }
                else
                {
                    return;
                }
            }
            gameObject.SetActive(true);
            renderTexture = RenderTexture.GetTemporary(width, height, 24);
            Camera.targetTexture = renderTexture;
            Camera.Render();
            RenderTexture.active = renderTexture;
            screenShotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShotTexture.Apply();
            Camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            RenderTexture.active = null; // JC: added to avoid errors
            if (IsTransparent(screenShotTexture))
            {
                Debug.LogWarning("Avatar image is transparent - skipping upload");
                gameObject.SetActive(false);
                return;
            }
            File.WriteAllBytes(filename, screenShotTexture.EncodeToPNG());
            NewPhotoTaken?.Invoke(Sprite.Create(screenShotTexture, new Rect(0, 0, screenShotTexture.width, screenShotTexture.height), new Vector2(0.5f, 0.5f), 100.0f));
            gameObject.SetActive(false);
        }

        private bool IsTransparent(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i].a != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
