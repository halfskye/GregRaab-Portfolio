using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Emerge.Home.Experiments.Draw3D.Displays
{
    public class Draw3D_DisplayManager : MonoBehaviour
    {
        public static Draw3D_DisplayManager Instance { get; private set; } = null;

        private List<Draw3D_BaseDisplay> _displays = new List<Draw3D_BaseDisplay>();

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_DisplayManager already exists. There should only be one.");
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        public bool RegisterDisplay(Draw3D_BaseDisplay display)
        {
            if (_displays.Contains(display))
            {
                Draw3D_Manager.DebugLogError("Display is already registered.", display);
                return false;
            }

            _displays.Add(display);
            return true;
        }

        public bool UnregisterDisplay(Draw3D_BaseDisplay display)
        {
            return _displays.Remove(display);
        }

        public bool DisplayDrawing(Draw3D_Drawing drawing)
        {
            //@TODO: Better mechanism for picking
            var display = _displays.FirstOrDefault(display => !display.IsOccupied);

            return display != null && display.TryAnchorDrawing(drawing);
        }
    }
}
