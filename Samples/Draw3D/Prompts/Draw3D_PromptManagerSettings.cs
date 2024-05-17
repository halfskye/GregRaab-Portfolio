using System.Collections.Generic;
using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Prompts
{
    [CreateAssetMenu(fileName = "Draw3D_PromptManagerSettings", menuName = "Experiments/Draw3D/New PromptManagerSettings", order = 0)]
    public class Draw3D_PromptManagerSettings : ScriptableObject
    {
        //@TODO: Turn this into an indexed map of prompts...

        public const int INVALID_PROMPT_ID = -1;

        [SerializeField] private List<string> _prompts = new List<string>();
        public List<string> Prompts => _prompts;

        public int TotalPromptsCount => Prompts.Count;

        [SerializeField] private int _defaultPromptIndex = 0;
        public string DefaultPrompt => Prompts[_defaultPromptIndex];

        public bool IsPromptIndexValid(int index)
        {
            return (index >= 0 && index < Prompts.Count);
        }
    }
}
