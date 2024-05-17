using System;
using UnityEngine;
using static GestureDetectionHandFinder;

namespace Emerge.Home.Experiments.Draw3D.GestureDetection.Gestures
{
    [CreateAssetMenu(fileName = "FingersToPalmRawOpennessData", menuName = "Experiments/Draw3D/New FingersToPalmRawOpenessData", order = 0)]
    public class FingersToPalmRawOpennessData : ScriptableObject
    {
        [Serializable]
        internal class RawFingerData
        {
            public bool isEnabled = true;
            public float extendFromPalmRatio = 2.1f;
            public float closedToPalmRatio = 0.3f;
        }

        [SerializeField] private RawFingerData thumbFingerData = null;
        [SerializeField] private RawFingerData indexFingerData = null;
        [SerializeField] private RawFingerData middleFingerData = null;
        [SerializeField] private RawFingerData ringFingerData = null;
        [SerializeField] private RawFingerData littleFingerData = null;

        internal RawFingerData FingerData(FingerType fingerType)
        {
            switch (fingerType)
            {
                case FingerType.Thumb:
                    return thumbFingerData;
                case FingerType.Index:
                    return indexFingerData;
                case FingerType.Middle:
                    return middleFingerData;
                case FingerType.Ring:
                    return ringFingerData;
                case FingerType.Little:
                    return littleFingerData;
                default:
                    Debug.LogError($"Finger Type {fingerType} not recognized. Defaulting to Index.");
                    return indexFingerData;
            }
        }
    }
}
