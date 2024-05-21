using UnityEngine;

namespace Interaction
{
    [CreateAssetMenu(fileName = "TargetObjectSelectData", menuName = "Targeting/new TargetObjectSelectData", order = 0)]
    public class TargetObjectSelectData : ScriptableObject
    {
        [SerializeField] private float focusTime = 0.5f;
        public float FocusTime => focusTime;
        
        [SerializeField] private float unfocusTime = 0.15f;
        public float UnfocusTime => unfocusTime;

        // FX
        [SerializeField] private GameObject focusFX = null;
        public GameObject FocusFX => focusFX;
        [SerializeField] private GameObject unfocusFX = null;
        public GameObject UnfocusFX => unfocusFX;
        [SerializeField] private GameObject selectFX = null;
        public GameObject SelectFX => selectFX;
    }
}
