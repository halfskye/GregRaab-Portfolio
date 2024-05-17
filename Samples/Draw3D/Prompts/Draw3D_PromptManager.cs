using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Prompts
{
    public class Draw3D_PromptManager : MonoBehaviour
    {
        [SerializeField] private Draw3D_PromptManagerSettings _promptSettings = null;

        public int TotalPromptsCount => _promptSettings.TotalPromptsCount;

        private bool IsPromptIndexValid(int index)
        {
            return _promptSettings.IsPromptIndexValid(index);
        }

        #region Singleton

        public static Draw3D_PromptManager Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_PromptManager already exists. There should only be one.");
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        #endregion Singleton

        public string GetPromptByIndex(int promptIndex)
        {
            if (!IsPromptIndexValid(promptIndex))
            {
                Debug.LogError($"Invalid Prompt Index ({promptIndex}) specified");
                return _promptSettings.DefaultPrompt;
            }

            return _promptSettings.Prompts[promptIndex];
        }
    }
}
