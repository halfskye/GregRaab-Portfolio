using Emerge.Home.TempUtils;
using UnityEngine;
using static GestureDetectionHandFinder;

namespace Emerge.Home.Experiments.Draw3D.GestureDetection.Gestures
{
    public class GestureDetectorFingerToPalmOpenness : BaseGestureDetector
    {
        // [Serializable]
        // private class FingerToPalmOpennessData
        // {
        //     public bool IsEnabled = true;
        //     public float ExtendFromPalmRatio = 2.1f;
        //     public float ClosedToPalmRatio = 0.3f;
        // }

        [Header("Finger To Palm Openness")]

        [SerializeField] private FingersToPalmOpennessData _fingerData = null;

        // [SerializeField] private bool _isExtendCheckEnabled = true;
        // [SerializeField] private bool _isClosedCheckEnabled = false;

        // [SerializeField] private FingerToPalmOpennessData _thumbData = null;
        // [SerializeField] private FingerToPalmOpennessData _indexData = null;
        // [SerializeField] private FingerToPalmOpennessData _middleData = null;
        // [SerializeField] private FingerToPalmOpennessData _ringData = null;
        // [SerializeField] private FingerToPalmOpennessData _littleData = null;

        public override bool IsUserMakingGestureStart()
        {
            return IsUserMakingGestureHold();
        }

        public override bool IsUserMakingGestureHold()
        {
            return IsPalmDistanceFromFaceAboveThreshold() &&
                   AreFingersExtendedFromPalmOrIgnored() &&
                   AreFingersClosedToPalmOrIgnored();
        }

        #region Utilities

        private bool TryGetFingerToPalmRatio(FingerType fingerType, out float ratio)
        {
            var palm = HandPalm;
            if (palm != null)
            {
                var palmPosition = palm.position;

                ratio = MathUtils.SquareMagnitudeRatio(
                    palmPosition - FingerTip(fingerType).position,
                    palmPosition - Knuckle(fingerType).position
                );

                // DebugLogError($"Finger {fingerType} Ratio: {ratio}", this);

                return true;
            }

            ratio = 0f;
            return false;
        }

        #endregion Utilities

        #region Extend / Open

        private bool IsFingerExtendedFromPalmOrIgnored(FingerType fingerType)
        {
            if (_fingerData.IsFingerEnabled(fingerType))
            {
                if (TryGetFingerToPalmRatio(fingerType, out var ratio))
                {
                    var fingerData = _fingerData.FingerData(fingerType);

                    // DebugLogError($"Finger {fingerType} Ratio: {ratio}, Threshold: {fingerData.extendFromPalmRatio}");

                    return ratio > fingerData.extendFromPalmRatio;
                }

                return false;
            }

            return true;
        }

        private bool AreFingersExtendedFromPalmOrIgnored()
        {
            return !_fingerData.isExtendCheckEnabled ||
                   (
                       IsFingerExtendedFromPalmOrIgnored(FingerType.Thumb) &&
                       IsFingerExtendedFromPalmOrIgnored(FingerType.Index) &&
                       IsFingerExtendedFromPalmOrIgnored(FingerType.Middle) &&
                       IsFingerExtendedFromPalmOrIgnored(FingerType.Ring) &&
                       IsFingerExtendedFromPalmOrIgnored(FingerType.Little)
                   );
        }

        #endregion Extend / Open

        #region Closed

        private bool IsFingerClosedToPalmOrIgnored(FingerType fingerType)
        {
            if (_fingerData.IsFingerEnabled(fingerType))
            {
                if (TryGetFingerToPalmRatio(fingerType, out var ratio))
                {
                    var fingerData = _fingerData.FingerData(fingerType);

                    // DebugLogError($"Finger {fingerType} Ratio: {ratio}, Threshold: {fingerData.closedToPalmRatio}");

                    return ratio < fingerData.closedToPalmRatio;
                }

                return false;
            }

            return true;
        }

        private bool AreFingersClosedToPalmOrIgnored()
        {
            return !_fingerData.isClosedCheckEnabled ||
                   (
                       IsFingerClosedToPalmOrIgnored(FingerType.Thumb) &&
                       IsFingerClosedToPalmOrIgnored(FingerType.Index) &&
                       IsFingerClosedToPalmOrIgnored(FingerType.Middle) &&
                       IsFingerClosedToPalmOrIgnored(FingerType.Ring) &&
                       IsFingerClosedToPalmOrIgnored(FingerType.Little
                       )
                   );
        }

        #endregion Closed
    }
}
