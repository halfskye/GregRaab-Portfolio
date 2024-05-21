using System.Linq;
using Cloud;
using Draw3D.Prompts;
using Draw3D.UI.Minigames;
using Tracking;
using Environments;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Draw3D.Minigames
{
    public class Draw3D_CharadesManager : NetworkBehaviour
    {
        public enum State
        {
            SELECT_PLAYER = 0,
            SELECT_PROMPT = 1,
            DRAW_PROMPT = 2,
            END_DRAW_PROMPT = 3,
            END_GAME = 4,
        }
        [Networked(OnChanged = nameof(OnStateChanged))] public State CurrentState { get; private set; } = State.SELECT_PLAYER;

        private const int MAX_CIRCLE_SIZE = 8;
        [Networked, Capacity(MAX_CIRCLE_SIZE)] public NetworkLinkedList<PlayerRef> SelectedPlayers { get; }
        [Networked(OnChanged = nameof(OnCurrentPlayerChanged))]
        public PlayerRef CurrentPlayer { get; private set; }
        public bool IsCurrentPlayer => CurrentPlayer == Runner.LocalPlayer;
        private string CurrentPlayerName { get; set; } = string.Empty;

        private const int ACTIVE_PROMPT_COUNT = 3;
        [Networked, Capacity(MAX_CIRCLE_SIZE * ACTIVE_PROMPT_COUNT)]
        public NetworkLinkedList<int> SelectedPrompts { get; }
        [Networked(OnChanged = nameof(OnPromptChoicesChanged)), Capacity(ACTIVE_PROMPT_COUNT)]
        public NetworkArray<int> PromptChoices { get; }
        [Networked] public int CurrentPrompt { get; private set; } = Draw3D_PromptManagerSettings.INVALID_PROMPT_ID;

        [Networked] public float Timer { get; private set; } = 0f;

        [SerializeField] private float SelectPromptTime = 10f;
        [SerializeField] private float DrawPromptTime = 60f;

        private Draw3D_WatchUI_Charades _watchUI = null;
        // private Draw3D_WatchUI_Charades WatchUI => _watchUI;
        private Draw3D_WatchUI_Charades WatchUI
        {
            get
            {
                if (_watchUI.IsNullOrDestroyed())
                {
                    _watchUI = FindObjectOfType<Draw3D_WatchUI_Charades>(true);
                }

                return _watchUI;
            }
        }

        public override void Spawned()
        {
            DebugLog("Draw3D_CharadesManager - Spawned");

            base.Spawned();

            Draw3D_MinigamesManager.Instance.SetCharadesManager(this);

            TableManager.OnFirstSeatAssigned += TableManagerOnFirstSeatAssigned;
            TableManager.OnSeatChanged += TableManagerOnSeatChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            DebugLog("Draw3D_CharadesManager - Despawned");

            ResetGame();

            TableManager.OnFirstSeatAssigned -= TableManagerOnFirstSeatAssigned;
            TableManager.OnSeatChanged -= TableManagerOnSeatChanged;

            Draw3D_MinigamesManager.Instance.SetCharadesManager(null);

            base.Despawned(runner, hasState);
        }

        private void Start()
        {
            WatchUI.SetPlayButtonActive(false);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            UpdateState();
        }

        public void StartGame()
        {
        }

        private void UpdateState()
        {
            UpdateTimer();

            switch (CurrentState)
            {
                case State.SELECT_PLAYER:
                    UpdateSelectPlayer();
                    break;
                case State.SELECT_PROMPT:
                    UpdateSelectPrompt();
                    break;
                case State.DRAW_PROMPT:
                    UpdateDrawPrompt();
                    break;
                case State.END_DRAW_PROMPT:
                    UpdateEndDrawPrompt();
                    break;
                case State.END_GAME:
                    UpdateEndGame();
                    break;
            }
        }

        private static void OnStateChanged(Changed<Draw3D_CharadesManager> changed)
        {
            var behavior = changed.Behaviour;
            behavior.OnStateChanged(behavior.CurrentState);
        }

        private void OnStateChanged(State currentState)
        {
            // Debug.LogError($"Draw3D_CharadesManager::OnStateChanged - {currentState}");
            switch (currentState)
            {
                case State.SELECT_PLAYER:
                    break;
                case State.SELECT_PROMPT:
                    Draw3D_Manager.Instance.DestroyCurrentDrawing();
                    SetPromptTextsIfCurrentPlayer();
                    break;
                case State.DRAW_PROMPT:
                    break;
                case State.END_DRAW_PROMPT:
                    break;
                case State.END_GAME:
                    break;
            }
        }

        private void UpdateTimer()
        {
            var isTimerVisible = CurrentState == State.SELECT_PROMPT ||
                                 CurrentState == State.DRAW_PROMPT;

            WatchUI.SetTimerActive(isTimerVisible);
            if (isTimerVisible)
            {
                WatchUI.SetTimerText(Timer);
            }
        }

        private void UpdateSelectPlayer()
        {
            if (Object.HasStateAuthority)
            {
                //@TODO: Select previously unselected player.

                var player = Runner.ActivePlayers.FirstOrDefault(x => !SelectedPlayers.Contains(x));
                if (player.IsValid)
                {
                    SelectPlayer(player);
                }
                else
                {
                    // No other players are selectable, so end game.
                    CurrentState = State.END_GAME;
                }
            }
        }

        private void SelectPlayer(PlayerRef player)
        {
            SelectPromptChoices();

            CurrentPlayer = player;
            SelectedPlayers.Add(CurrentPlayer);

            Timer = SelectPromptTime;
            CurrentState = State.SELECT_PROMPT;

            // RPC_SelectPlayer(player);
        }

        private void SetPromptTextsIfCurrentPlayer()
        {
            if (IsCurrentPlayer)
            {
                RPC_SendCurrentPlayerName(Cloud.CurrentUser.FirstName);
                SetPromptTexts();
                WatchUI.SetStatusActiveWithText($"Pick one:");
            }
        }

        private static void OnCurrentPlayerChanged(Changed<Draw3D_CharadesManager> changed)
        {
            var behavior = changed.Behaviour;
            behavior.SetPromptTextsIfCurrentPlayer();
        }

        private void SetPromptTexts()
        {
            var promptTexts = PromptChoices.Select(x => Draw3D_PromptManager.Instance.GetPromptByIndex(x)).ToList();
            WatchUI.SetPromptTexts(promptTexts);
        }

        private void SelectPromptChoices()
        {
            var totalPromptCount = Draw3D_PromptManager.Instance.TotalPromptsCount;
            for (int i = 0; i < ACTIVE_PROMPT_COUNT; i++)
            {
                bool foundChoice = false;
                while (!foundChoice)
                {
                    var choice = Random.Range(0, totalPromptCount);

                    if (!SelectedPrompts.Contains(choice))
                    {
                        PromptChoices.Set(i, choice);
                        SelectedPrompts.Add(choice);
                        foundChoice = true;
                    }
                }
            }
        }

        private static void OnPromptChoicesChanged(Changed<Draw3D_CharadesManager> changed)
        {
        }

        private void UpdateSelectPrompt()
        {
            if (this.Object.HasStateAuthority)
            {
                Timer -= Runner.DeltaTime;
                if (Timer < 0f)
                {
                    // CurrentPlayer ran out of time to select prompt, select for them.

                    var index = Random.Range(0, PromptChoices.Length);
                    SelectPrompt(index);
                    // CurrentPrompt = PromptChoices[index];

                    // Timer = DrawPromptTime;
                    // CurrentState = State.DRAW_PROMPT;
                }
            }
            // WatchUI.SetTimerText(Timer);
        }

        public void SelectPrompt(int promptIndex)
        {
            if (CurrentState == State.SELECT_PROMPT)
            {
                RPC_SelectPrompt(promptIndex);
            }
            else if (CurrentState == State.DRAW_PROMPT)
            {
                EndDrawPrompt();
            }
        }

        private void UpdateDrawPrompt()
        {
            if (this.Object.HasStateAuthority)
            {
                Timer -= Runner.DeltaTime;
                if (Timer < 0f)
                {
                    // CurrentPlayer ran out of time for guesses.

                    EndDrawPrompt();
                    // CurrentState = State.END_DRAW_PROMPT;
                }
            }
        }

        public void EndDrawPrompt()
        {
            WatchUI.SetTimerActive(false);
            WatchUI.SetPromptMenuActive(false);

            RPC_EndDrawPrompt();
        }

        private void UpdateEndDrawPrompt()
        {
            if (this.Object.HasStateAuthority)
            {
                CurrentState = State.SELECT_PLAYER;
            }
        }

        private void UpdateEndGame()
        {
            ResetGame();

            Despawn();
        }

        private void ResetGame()
        {
            WatchUI.SetTimerActive(false);
            WatchUI.SetStatusActive(false);
            WatchUI.SetPromptMenuActive(false);
            WatchUI.SetPlayButtonActive(true);
        }

        private void Despawn()
        {
            if (this.Object.HasStateAuthority)
            {
                Runner.Despawn(this.Object);
            }
        }

        private void TableManagerOnSeatChanged(int tableIndex, int seatIndex)
        {
            DebugLog("Draw3D_CharadesManager - TableManagerOnSeatChanged");

            OnSeatChanged();
        }

        private void TableManagerOnFirstSeatAssigned(int tableIndex,int seatIndex)
        {
            DebugLog("Draw3D_CharadesManager - TableManagerOnFirstSeatAssigned");

            OnSeatChanged();
        }

        private void OnSeatChanged()
        {
            if (IsCurrentPlayer && !Draw3D_Manager.GetIsSeatedAtDraw3DTable())
            {
                EndDrawPrompt();
            }
        }

        private void DebugLog(string message)
        {
            DebugLogUtilities.LogError(DebugLogUtilities.DebugLogType.DRAW_3D, message, this);
        }

        #region RPCs

        // [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        // private void RPC_SelectPlayer(PlayerRef player)
        // {
        //     if (Runner.LocalPlayer == player)
        //     {
        //     }
        // }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SelectPrompt(int promptIndex)
        {
            //@NOTE: RpcTargets.StateAuthority should just work, but for safety:
            if (this.Object.HasStateAuthority)
            {
                CurrentPrompt = PromptChoices[promptIndex];

                Timer = DrawPromptTime;
                CurrentState = State.DRAW_PROMPT;
            }

            if (IsCurrentPlayer)
            {
                WatchUI.OnPromptSelected(promptIndex);
            }
            else
            {
                WatchUI.SetStatusActiveWithText($"{CurrentPlayerName} is drawing...");
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_EndDrawPrompt()
        {
            //@NOTE: RpcTargets.StateAuthority should just work, but for safety:
            if (this.Object.HasStateAuthority)
            {
                CurrentState = State.END_DRAW_PROMPT;
            }

            WatchUI.SetEndPromptActive(false);
            WatchUI.SetStatusActive(false);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SendCurrentPlayerName(string playerName)
        {
            if (CurrentPlayer != Runner.LocalPlayer)
            {
                CurrentPlayerName = playerName;
                WatchUI.SetStatusActiveWithText($"{CurrentPlayerName} is picking...");
            }
        }

        #endregion RPCs
    }
}
