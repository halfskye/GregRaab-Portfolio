using UnityEngine;
using static GestureDetectionHandFinder;

namespace Draw3D.GestureDetection.Gestures
{
    [CreateAssetMenu(fileName = "FingersToPalmOpennessData", menuName = "Experiments/Draw3D/New FingersToPalmOpenessData", order = 0)]
    public class FingersToPalmOpennessData : ScriptableObject
    {
        [SerializeField] internal bool isExtendCheckEnabled = true;
        [SerializeField] internal bool isClosedCheckEnabled = false;

        [SerializeField] internal FingersToPalmRawOpennessData rawOpennessData = null;

        [Header("Finger Enabled Overrides")]

        [SerializeField] private bool isThumbEnabledOverride = true;
        [SerializeField] private bool isIndexEnabledOverride = true;
        [SerializeField] private bool isMiddleEnabledOverride = true;
        [SerializeField] private bool isRingEnabledOverride = true;
        [SerializeField] private bool isLittleEnabledOverride = true;

        internal FingersToPalmRawOpennessData.RawFingerData FingerData(FingerType fingerType)
        {
            return rawOpennessData.FingerData(fingerType);
        }

        internal bool IsFingerEnabled(FingerType fingerType)
        {
            var fingerData = FingerData(fingerType);
            if (!fingerData.isEnabled)
            {
                return false;
            }

            switch (fingerType)
            {
                case FingerType.Thumb:
                    return isThumbEnabledOverride;
                case FingerType.Index:
                    return isIndexEnabledOverride;
                case FingerType.Middle:
                    return isMiddleEnabledOverride;
                case FingerType.Ring:
                    return isRingEnabledOverride;
                case FingerType.Little:
                    return isLittleEnabledOverride;
                default:
                    return false;
            }
        }
    }
}
