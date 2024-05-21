using System;
using System.Collections.Generic;
using System.Linq;
using Tracking;
using UnityEngine;
using static GestureDetectionHandFinder;

namespace Draw3D.GestureDetection.Gestures
{
    public class BaseGestureDetector : MonoBehaviour
    {
        [Header("Base Gesture Detector")]

        [SerializeField] private GestureDetectionHandFinder _handFinder = null;
        protected GestureDetectionHandFinder HandFinder => Draw3D_GestureDetectionManager.Instance.HandFinder;

        [SerializeField] private float _detectionTime = 0.01f;
        private float _detectionTimer = 0f;
        [SerializeField] private float _gestureDetectionThreshold = 0.95f; // Percentage of gesture-detecting samples per total samples required to detect a gesture

        private struct DetectedGestureSample
        {
            public float TimeStamp { get; }
            public bool WasDetected { get; }

            public DetectedGestureSample(float timeStamp, bool wasDetected)
            {
                TimeStamp = timeStamp;
                WasDetected = wasDetected;
            }
        }

        private readonly List<DetectedGestureSample> _detectedGestureSamples = new List<DetectedGestureSample>();

        [SerializeField] private bool _isGestureResetRequired = true;
        private bool _isGestureReset = true;

        [SerializeField] private float _minimumTimeBetweenDetections = 1f;
        private float _timerBetweenDetections = 0f;

        private bool _isGestureActive = false;
        private bool _wasEnabled;

        protected Transform Face => HandFinder.face;
        [SerializeField] private Chirality _chirality = Chirality.Right;
        protected Chirality Chirality => _chirality;
        public Transform HandPalm => HandFinder.HandPalm(Chirality);
        protected Transform HandPalmOpposite => HandFinder.HandPalm(ChiralityOpposite);
        protected Transform HandWrist => HandFinder.HandWrist(Chirality);
        protected Transform HandWristOpposite => HandFinder.HandWrist(ChiralityOpposite);
        protected Transform FingerTip(FingerType fingerType) => HandFinder.FingerTip(Chirality, fingerType);
        protected Transform Knuckle(FingerType fingerType) => HandFinder.Knuckle(Chirality, fingerType);

        protected Chirality ChiralityOpposite => Chirality == Chirality.Left ? Chirality.Right : Chirality.Left;

        [SerializeField] private float _palmToFaceDistance = 0.1f;

        public event Action OnDetectStart;
        public event Action OnDetectStop;

        #region Application Lifecycle

        protected virtual void Awake()
        {
            ApplicationManager.OnDidEnterState += OnApplicationStateChange;
        }

        private void OnApplicationStateChange(ApplicationState oldState, ApplicationState newState)
        {
            if (oldState == ApplicationState.Calibration)
            {
                OnCalibrationEnd();
            }
            else if (newState == ApplicationState.Calibration)
            {
                OnCalibrationBegin();
            }
        }

        private void OnCalibrationBegin()
        {
            _wasEnabled = this.enabled;
            this.enabled = false;
        }

        private void OnCalibrationEnd()
        {
            this.enabled = _wasEnabled;
        }

        protected virtual void OnDestroy()
        {
            ApplicationManager.OnDidEnterState -= OnApplicationStateChange;
        }

        #endregion Application Lifecycle

        protected virtual bool ShouldUpdate()
        {
            return OVRPlugin.GetHandTrackingEnabled() &&
                   !ApplicationManager.Instance.FusionSceneManager.IsLoadingScene;
        }

        protected virtual void Update()
        {
            if (!ShouldUpdate())
            {
                // Debug.LogError("BaseGestureDetector::Update - !ShouldUpdate");
                StopGestureIfActive();

                return;
            }

            //@TODO: Could introduce Sample Rate (to throttle the update as an efficiency)
            //@TODO: Could introduce Minimum Samples Required (in cases of low frame rate, etc)

            var deltaTime = Time.deltaTime;
            var timestamp = Time.time;

            _detectionTimer += deltaTime;
            _timerBetweenDetections = Mathf.Max(0f, _timerBetweenDetections - deltaTime);

            // Prune older samples
            _detectedGestureSamples.RemoveAll(sample => sample.TimeStamp < timestamp - _detectionTime);

            var isUserMakingGesture = _isGestureActive ? IsUserMakingGestureHold() : IsUserMakingGestureStart();
            _detectedGestureSamples.Add(new DetectedGestureSample(timestamp, isUserMakingGesture));

            if (_detectionTimer >= _detectionTime && GetDetectedGestureSampleRatio() > _gestureDetectionThreshold)
            {
                if (CanDetectGesture())
                {
                    DetectSuccessfulGesture();
                }
            }
            else
            {
                StopGestureIfActive();
            }
        }

        private void StopGestureIfActive()
        {
            if (_isGestureActive)
            {
                OnDetectStop?.Invoke();
            }

            _isGestureActive = false;
            _isGestureReset = true;
        }

        private float GetDetectedGestureSampleRatio()
        {
            return (float)_detectedGestureSamples.Count(sample => sample.WasDetected) / _detectedGestureSamples.Count;
        }

        private void DetectSuccessfulGesture()
        {
            if (!_isGestureActive)
            {
                OnDetectStart?.Invoke();
            }

            _isGestureActive = true;
            _isGestureReset = false;
            _timerBetweenDetections = _minimumTimeBetweenDetections;
        }

        public virtual bool IsUserMakingGestureStart()
        {
            return false;
        }

        public virtual bool IsUserMakingGestureHold()
        {
            return IsUserMakingGestureStart();
        }

        private bool CanDetectGesture()
        {
            return IsTimerBetweenDetectionsReady() && !IsGestureResetRequired();
        }

        private bool IsTimerBetweenDetectionsReady()
        {
            return _timerBetweenDetections <= 0f;
        }

        private bool IsGestureResetRequired()
        {
            return _isGestureResetRequired && !_isGestureReset;
        }

        protected bool IsPalmDistanceFromFaceAboveThreshold()
        {
            var face = Face;
            var palm = HandPalm;

            if (palm == null || face == null)
            {
                return false;
            }

            return MathUtils.IsDistanceBetweenTransformsGreaterThan(palm, face, _palmToFaceDistance);
        }
    }
}
