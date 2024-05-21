using Draw3D.GestureDetection;
using Tracking;
using UnityEngine;

namespace Draw3D.Brushes
{
    [CreateAssetMenu(fileName = "Draw3D_BrushTactility", menuName = "Experiments/Draw3D/New BrushTactility", order = 0)]
    public class Draw3D_BrushTactility : ScriptableObject
    {
        private GestureDetectionHandFinder HandFinder => Draw3D_GestureDetectionManager.Instance.HandFinder;

        private Transform HandPalm => HandFinder.HandPalm(_chirality);
        private Transform FingerTip(GestureDetectionHandFinder.FingerType fingerType) => HandFinder.FingerTip(_chirality, fingerType);

        [Header("Anchor Position")]
        [SerializeField, Range(0f,1f)] private float thumbToIndexWeight = 0.5f;
        private Vector3 PinchPoint => Vector3.Lerp(FingerTip(GestureDetectionHandFinder.FingerType.Thumb).position, FingerTip(GestureDetectionHandFinder.FingerType.Index).position, thumbToIndexWeight);
        [SerializeField, Range(0f,1f)] private float staticPinchPointToPalmWeight = 0.5f;
        private Vector3 TactilityPoint(float pinchPointToPalmTime)
        {
            pinchPointToPalmTime = Mathf.Clamp01(pinchPointToPalmTime);
            return Vector3.Lerp(PinchPoint, HandPalm.position, pinchPointToPalmTime);
        }

        private Vector3 StaticTactilityPoint => TactilityPoint(staticPinchPointToPalmWeight);

        [Header("Strength Pulse")]
        [SerializeField] private bool useStrengthPulse = true;
        [SerializeField] private Vector2 strengthPulseActiveAmplitudeRange = new Vector2(90f,100f);
        private float StrengthPulseActiveAmplitude => Random.Range(strengthPulseActiveAmplitudeRange.x, strengthPulseActiveAmplitudeRange.y);
        [SerializeField, Min(0f)] private Vector2 strengthPulseActiveTimeRange = new Vector2(0.5f,1f);
        private float StrengthPulseActiveTime => Random.Range(strengthPulseActiveTimeRange.x, strengthPulseActiveTimeRange.y);
        [SerializeField] private Vector2 strengthPulseDisabledAmplitudeRange = new Vector2(0f,10f);
        private float StrengthPulseDisabledAmplitude => Random.Range(strengthPulseDisabledAmplitudeRange.x, strengthPulseDisabledAmplitudeRange.y);
        [SerializeField, Min(0f)] private Vector2 strengthPulseDisabledTimeRange = new Vector2(0f,0.1f);
        private float StrengthPulseDisabledTime => Random.Range(strengthPulseDisabledTimeRange.x, strengthPulseDisabledTimeRange.y);
        private bool _strengthPulseActive = true;
        private float _strengthPulseTimer = 0f;

        [Header("Bounce Position")]
        [SerializeField] private bool useTactilityBounce = true;
        [SerializeField] private Vector2 tactilityBounceSpeedRange = new Vector2(0.5f, 1f);
        private float RandomTactilityBounceSpeed => Random.Range(tactilityBounceSpeedRange.x, tactilityBounceSpeedRange.y);
        private float _tactilityBounceSpeed = 0f;
        [SerializeField] private Vector2 tactilityBounceExtentTimes = new Vector2(0f, 1f);
        [SerializeField] private float tactilityBounceStartTime = 0.5f;
        private float _tactilityBounceTime = 0f;
        private bool _tactilityBounceDirection = true;

        public void SetChirality(Chirality chirality) { _chirality = chirality; }
        private Chirality _chirality = Chirality.Right;

        public void SetTactileCluster(TactileCluster tactileCluster) { _tactileCluster = tactileCluster; }
        private TactileCluster _tactileCluster = null;

        public void StartTactility(TactileCluster tactileCluster, Chirality chirality)
        {
            _tactileCluster = tactileCluster;
            _chirality = chirality;
            StartTactilityPosition();
            StartStrengthPulse();
            _tactileCluster.TactilityEnabled = true;
        }

        public void UpdateTactility()
        {
            UpdateStrengthPulse();

            UpdateTactilityPosition();
        }

        private void SetTactilityPosition(Vector3 position)
        {
            _tactileCluster.transform.position = position;
        }

        private void StartTactilityPosition()
        {
            if (useTactilityBounce)
            {
                StartTactilityBounce();
            }
            else
            {
                UpdateStaticTactilityPosition();
            }
        }

        private void UpdateTactilityPosition()
        {
            if (useTactilityBounce)
            {
                UpdateTactilityBounce();
            }
            else
            {
                UpdateStaticTactilityPosition();
            }
        }

        private void UpdateStaticTactilityPosition()
        {
            SetTactilityPosition(StaticTactilityPoint);
        }

        private void StartTactilityBounce()
        {
            if (!useTactilityBounce)
            {
                return;
            }

            _tactilityBounceDirection = !_tactilityBounceDirection;
            _tactilityBounceSpeed = RandomTactilityBounceSpeed;
            SetTactilityPosition(TactilityPoint(tactilityBounceStartTime));
        }

        private void UpdateTactilityBounce()
        {
            if (!useTactilityBounce)
            {
                return;
            }

            float bounceDirection = _tactilityBounceDirection ? 1f : -1f;
            _tactilityBounceTime += (Time.deltaTime * _tactilityBounceSpeed * bounceDirection);

            SetTactilityPosition(TactilityPoint(_tactilityBounceTime));

            if ((_tactilityBounceDirection && _tactilityBounceTime >= tactilityBounceExtentTimes.y) ||
                (!_tactilityBounceDirection && _tactilityBounceTime <= tactilityBounceExtentTimes.x))
            {
                _tactilityBounceDirection = !_tactilityBounceDirection;
                _tactilityBounceSpeed = RandomTactilityBounceSpeed;
            }
        }

        private void StartStrengthPulse()
        {
            if (!useStrengthPulse)
            {
                return;
            }

            _strengthPulseActive = true;
            _strengthPulseTimer = StrengthPulseActiveTime;
        }

        private void UpdateStrengthPulse()
        {
            if (!useStrengthPulse)
            {
                return;
            }

            _strengthPulseTimer -= Time.deltaTime;
            if (_strengthPulseTimer < 0f)
            {
                _strengthPulseActive = !_strengthPulseActive;
                if (_strengthPulseActive)
                {
                    _strengthPulseTimer = StrengthPulseActiveTime;
                    _tactileCluster.Strength = StrengthPulseActiveAmplitude;
                }
                else
                {
                    _strengthPulseTimer = StrengthPulseDisabledTime;
                    _tactileCluster.Strength = StrengthPulseDisabledAmplitude;
                }
            }
        }
    }
}
