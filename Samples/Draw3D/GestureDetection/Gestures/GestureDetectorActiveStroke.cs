using Emerge.Home.Experiments.Draw3D.Brushes;
using Emerge.Home.Experiments.Draw3D.Palettes;
using Emerge.Home.TempUtils;
using Emerge.SDK.Core.Tracking;
using UnityEngine;
using XRTK.Extensions;
using static GestureDetectionHandFinder;

namespace Emerge.Home.Experiments.Draw3D.GestureDetection.Gestures
{
    public class GestureDetectorActiveStroke : BaseGestureDetectorDraw3D
    {
        [Header("Active Stroke")]

        [SerializeField] private float _indexFingerDistanceRatioMinThreshold = 2.1f;

        [SerializeField] private float _middleFingerDistanceRatioMinThreshold = 2.1f;

        [SerializeField] private float _middleFingerDistanceRatioMaxThreshold = 5.0f;

        [SerializeField] private float _ringFingerDistanceRatioMaxThreshold = 0.75f;

        [SerializeField] private float _indexFingerAndFaceFacingsAngleThreshold = 120;

        [SerializeField] private float _indexFingerToOppositePalmMinThreshold = 0.1f;

        [SerializeField] private float _pinchIndexToThumbDistanceMinThreshold = 0.1f;

        [SerializeField] private Renderer _previewRenderer = null;

        private enum DrawGestureStyle
        {
            ONE_FINGER = 0,
            TWO_FINGER = 1,
            PINCH = 2,
        }
        [SerializeField] private DrawGestureStyle _drawGestureStyle = DrawGestureStyle.PINCH;

        public Transform DrawPoint
        {
            get
            {
                if (_pen.IsNullOrDestroyed())
                {
                    _pen = FindObjectOfType<Draw3D_Pen>();
                }
                if (!_pen.IsNullOrDestroyed())
                {
                    return _pen.DrawPoint;
                }

                return HandFinder.FingerTip(Chirality, FingerType.Index);
            }
        }
        private Draw3D_Pen _pen = null;

        protected override void Awake()
        {
            base.Awake();

            OnDetectStart += OnActiveStrokeStart;
            OnDetectStop += OnActiveStrokeStop;
        }

        protected override void OnDestroy()
        {
            OnDetectStart -= OnActiveStrokeStart;
            OnDetectStop -= OnActiveStrokeStop;

            base.OnDestroy();
        }

        private void OnActiveStrokeStart()
        {
            DestroyPreviewMaterial();

            _previewRenderer.material = new Material(GetPreviewMaterial())
            {
                color = GetPreviewColor()
            };

            var previewTransform = _previewRenderer.transform;
            previewTransform.parent = DrawPoint;
            previewTransform.localPosition = Vector3.zero;
            _previewRenderer.SetActive(true);
        }

        private Material GetPreviewMaterial()
        {
            return Draw3D_BrushManager.IsEraserActive
                ? Draw3D_PaletteManager.Instance.EraserMaterial
                : Draw3D_PaletteManager.Instance.SelectedPalette.Material;
        }

        private Color GetPreviewColor()
        {
            return Draw3D_BrushManager.IsEraserActive
                ? Draw3D_PaletteManager.Instance.EraserColor
                : Draw3D_PaletteManager.Instance.SelectedPaletteColor;
        }

        private void OnActiveStrokeStop()
        {
            _previewRenderer.SetActive(false);

            DestroyPreviewMaterial();
        }

        private void DestroyPreviewMaterial()
        {
            if (_previewRenderer.material != null)
            {
                Destroy(_previewRenderer.material);
                _previewRenderer.material = null;
            }
        }

        public override bool IsUserMakingGestureStart()
        {
            return IsUserMakingGestureHold();
        }

        public override bool IsUserMakingGestureHold()
        {
            switch (_drawGestureStyle)
            {
                case DrawGestureStyle.ONE_FINGER:
                    return IsUserMakingOneFingerDrawGesture();
                case DrawGestureStyle.TWO_FINGER:
                    return IsUserMakingTwoFingerDrawGesture();
                case DrawGestureStyle.PINCH:
                    return IsUserMakingPinchDrawGesture();
                default:
                    return false;
            }
        }

        private bool IsUserMakingOneFingerDrawGesture()
        {
            return IsUserMakingExtendedFingerDrawGesture();
        }

        private bool IsUserMakingTwoFingerDrawGesture()
        {
            return IsUserMakingExtendedFingerDrawGesture();
        }

        private bool IsUserMakingExtendedFingerDrawGesture()
        {
            return IsDrawingHandInPosition()
                   && AreDrawingFingersExtended()
                   && IsFirstNonDrawingFingerCloseToPalm()
                   && AreDrawingFingersPointedInSameDirectionAsFace();
        }

        private bool IsUserMakingPinchDrawGesture()
        {
            return IsDrawingHandInPosition()
                   && IsUserMakingPinchGesture();
        }

        private bool IsDrawingHandInPosition()
        {
            return IsPalmDistanceFromFaceAboveThreshold()
                   && IsFingertipFarFromOppositeWrist();
        }

        private bool AreDrawingFingersExtended()
        {
            switch (_drawGestureStyle)
            {
                case DrawGestureStyle.ONE_FINGER:
                    return IsIndexFingerExtended();
                case DrawGestureStyle.TWO_FINGER:
                    return IsIndexFingerExtended() && IsMiddleFingerExtended();
                case DrawGestureStyle.PINCH:
                    return IsIndexFingerExtended();
                default:
                    return false;
            }
        }

        private bool IsFingerExtended(FingerType fingerType, float minTreshold)
        {
            var palm = HandPalm;
            if (palm.IsNullOrDestroyed())
            {
                return false;
            }

            var finger = FingerTip(fingerType);
            var knuckle = Knuckle(fingerType);

            var palmPosition = palm.position;
            return MathUtils.SquareMagnitudeRatio(
                palmPosition - finger.position,
                palmPosition - knuckle.position
            ) > minTreshold;
        }

        private bool IsIndexFingerExtended()
        {
            return IsFingerExtended(FingerType.Index, _indexFingerDistanceRatioMinThreshold);
        }

        private bool IsMiddleFingerExtended()
        {
            return IsFingerExtended(FingerType.Middle, _middleFingerDistanceRatioMinThreshold);
        }

        private bool IsFirstNonDrawingFingerCloseToPalm()
        {
            switch (_drawGestureStyle)
            {
                case DrawGestureStyle.ONE_FINGER:
                    return IsMiddleFingerCloseToPalm();
                case DrawGestureStyle.TWO_FINGER:
                    return IsRingFingerCloseToPalm();
                default:
                    return false;
            }
        }

        private bool IsFingerCloseToPalm(FingerType fingerType, float fingerDistanceRatioMaxThreshold)
        {
            var palm = HandPalm;
            if (palm != null)
            {
                var palmPosition = palm.position;
                var finger = FingerTip(fingerType);
                var knuckle = Knuckle(fingerType);

                var ratio = MathUtils.SquareDistanceRatio(
                    palmPosition - finger.position,
                    palmPosition - knuckle.position
                );
                // DebugLogError($"Finger Ratio: {ratio}", this);

                return ratio < fingerDistanceRatioMaxThreshold;
            }

            return false;
        }

        private bool IsMiddleFingerCloseToPalm()
        {
            return IsFingerCloseToPalm(FingerType.Middle, _middleFingerDistanceRatioMaxThreshold);
        }

        private bool IsRingFingerCloseToPalm()
        {
            return IsFingerCloseToPalm(FingerType.Ring, _ringFingerDistanceRatioMaxThreshold);
        }

        private bool AreDrawingFingersPointedInSameDirectionAsFace()
        {
            switch (_drawGestureStyle)
            {
                case DrawGestureStyle.ONE_FINGER:
                    return IsIndexFingerPointedInSameDirectionAsFace();
                case DrawGestureStyle.TWO_FINGER:
                    return IsIndexFingerPointedInSameDirectionAsFace() && IsMiddleFingerPointedInSameDirectionAsFace();
                default:
                    return false;
            }
        }

        private bool IsFingerPointedInSameDirectionAsFace(FingerType fingerType, float facingsAngleTreshold)
        {
            var face = Face;
            var palm = HandPalm;
            var finger = FingerTip(fingerType);

            if (palm == null || face == null || finger == null)
            {
                return false;
            }

            var palmPosition = palm.position;
            var angleBetweenFaceAndFingerFacings = Vector3.Angle(palmPosition - finger.position, face.position - palmPosition);
            return angleBetweenFaceAndFingerFacings < facingsAngleTreshold;
        }

        private bool IsIndexFingerPointedInSameDirectionAsFace()
        {
            return IsFingerPointedInSameDirectionAsFace(FingerType.Index, _indexFingerAndFaceFacingsAngleThreshold);
        }

        private bool IsMiddleFingerPointedInSameDirectionAsFace()
        {
            return IsFingerPointedInSameDirectionAsFace(FingerType.Middle, _indexFingerAndFaceFacingsAngleThreshold); //@TODO: Do we need unique variable for Middle finger?
        }

        private bool IsFingertipFarFromOppositeWrist()
        {
            var fingertip = FingerTip(FingerType.Index);
            var oppositeWrist = HandWristOpposite;

            if (fingertip == null || oppositeWrist == null)
            {
                return false;
            }

            var isDistanceOverThreshold = MathUtils.IsDistanceBetweenTransformsGreaterThan(fingertip, oppositeWrist, _indexFingerToOppositePalmMinThreshold);
            return isDistanceOverThreshold;
        }

        private bool IsUserMakingPinchGesture()
        {
            var indexTip = FingerTip(FingerType.Index);
            var thumbTip = FingerTip(FingerType.Thumb);

            if (indexTip == null || thumbTip == null)
            {
                return false;
            }

            return MathUtils.IsDistanceBetweenTransformsLessThan(
                indexTip,
                thumbTip,
                _pinchIndexToThumbDistanceMinThreshold
            );
        }
    }
}
