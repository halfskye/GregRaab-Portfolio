using System.Collections.Generic;
using Emerge.Home.Experiments.Draw3D.Minigames;
using Emerge.SDK.Core.Tracking;
using EmergeHome.Code.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using XRTK.Extensions;

namespace Emerge.Home.Experiments.Draw3D.UI.Minigames
{
    public class Draw3D_WatchUI_Charades : MonoBehaviour
    {
        [SerializeField] private Transform _playButton = null;

        [SerializeField] private Transform _timerMenu = null;
        [SerializeField] private TextMeshProUGUI _timerText = null;

        [SerializeField] private Transform _promptMenu = null;

        // [SerializeField] private List<TextMeshProUGUI> _prompts = null;
        [SerializeField] private List<Draw3D_WatchUI_Prompt> _prompts = null;

        [SerializeField] private List<Transform> _promptDividers = null;

        [SerializeField] private Transform _statusMenu = null;
        [SerializeField] private TextMeshProUGUI _statusText = null;

        [SerializeField] private Transform _endPromptMenu = null;

        private void Awake()
        {
            ApplicationManager.OnDidEnterState += OnApplicationStateChanged;
        }

        private void OnDestroy()
        {
            ApplicationManager.OnDidEnterState -= OnApplicationStateChanged;
        }

        private void OnApplicationStateChanged(ApplicationState oldstate, ApplicationState newstate)
        {
            if (newstate is ApplicationState.Labs or ApplicationState.FloatingIsland)
            {
                SetPlayButtonActive(true);
                SetTimerActive(false);
                SetEndPromptActive(false);
                SetPromptMenuActive(false);
            }
        }

        public void StartGame()
        {
            Draw3D_MinigamesManager.RPC_StartCharades(ApplicationManager.Instance.Runner);
        }

        public void SetPlayButtonActive(bool isActive)
        {
            _playButton.SetActive(isActive);
        }

        public void SetPromptMenuActive(bool isActive)
        {
            _promptMenu.SetActive(isActive);
        }

        public void SetPromptText(int promptIndex, string promptText)
        {
            _prompts[promptIndex].SetText(promptText);
        }

        public void SetPromptTexts(List<string> promptTexts)
        {
            Assert.AreEqual(_prompts.Count, promptTexts.Count,
                "Count of Prompt texts and menu entries don't match."
            );

            ResetPrompts();

            for (var i = 0; i < _prompts.Count; i++)
            {
                SetPromptText(i, promptTexts[i]);
                // _prompts[i].SetText(promptTexts[i]);
                // _prompts[i].text = promptTexts[i];
            }
        }

        public void SelectPrompt(int promptIndex)
        {
            Draw3D_MinigamesManager.Instance.CharadesManager.SelectPrompt(promptIndex);

            // OnPromptSelected(promptIndex);
        }

        public void OnPromptSelected(int promptIndex)
        {
            //@TODO: Set Status to Prompt
            _statusText.text = _prompts[promptIndex].GetText();

            SetPromptMenuActive(false);
            SetEndPromptActive(true);

            // for (var i = 0; i < _prompts.Count; i++)
            // {
            //     _prompts[i].SetActive(i == promptIndex);
            // }
            // SetPromptDividersActive(false);
        }

        // private void SetPromptDividersActive(bool isActive)
        // {
        //     _promptDividers.ForEach(x => x.gameObject.SetActive(isActive));
        // }

        public void EndPrompt()
        {
            Draw3D_MinigamesManager.Instance.CharadesManager.EndDrawPrompt();
            SetEndPromptActive(false);
        }

        private void ResetPrompts()
        {
            SetPromptMenuActive(true);
            // _prompts.ForEach(x => x.SetActive(true));
            // SetPromptDividersActive(true);
        }

        public void SetStatusActiveWithText(string status)
        {
            SetStatusActive(true);
            SetStatusText(status);
        }

        public void SetStatusActive(bool isActive)
        {
            _statusMenu.SetActive(isActive);
        }

        private void SetStatusText(string status)
        {
            _statusText.text = status;
        }

        public void SetTimerActive(bool isActive)
        {
            _timerMenu.SetActive(isActive);
        }

        public void SetTimerText(float time)
        {
            _timerText.text = time.ToString("F1");
        }

        public void SetEndPromptActive(bool isActive)
        {
            _endPromptMenu.SetActive(isActive);
        }
    }
}
