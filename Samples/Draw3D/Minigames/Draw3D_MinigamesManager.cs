using Emerge.SDK.Core.Tracking;
using EmergeHome.Code.Core;
using Fusion;
using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Minigames
{
    public class Draw3D_MinigamesManager : SimulationBehaviour
    {
        [SerializeField] private Draw3D_CharadesManager _charadesManagerPrefab = null;

        private Draw3D_CharadesManager _charadesManager = null;
        public Draw3D_CharadesManager CharadesManager => _charadesManager;
        public bool IsCharadesActive() { return !_charadesManager.IsNullOrDestroyed(); }
        public void SetCharadesManager(Draw3D_CharadesManager charadesManager)
        {
            _charadesManager = charadesManager;
        }

        public static Draw3D_MinigamesManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_MinigamesManager already exists. There should only be one.");
                Destroy(this.gameObject);

                return;
            }

            Instance = this;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public static void RPC_StartCharades(NetworkRunner runner)
        {
            Instance.TryStartCharades();
        }

        private void TryStartCharades()
        {
            var runner = ApplicationManager.Instance.Runner;
            if (runner.IsSharedModeMasterClient && !IsCharadesActive())
            {
                _charadesManager = runner.Spawn(_charadesManagerPrefab);
                _charadesManager.StartGame();
            }
        }

        public bool CanDraw()
        {
            return CanDraw_Charades();
        }

        private bool CanDraw_Charades()
        {
            return _charadesManager.IsNullOrDestroyed() || _charadesManager.IsCurrentPlayer;
        }
    }
}
