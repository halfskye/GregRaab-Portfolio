using UnityEngine;

namespace Draw3D.GestureDetection.Gestures
{
    public class GestureDetectorChangePaletteColor : BaseGestureDetectorDraw3D
    {
        [Header("Change Palette Color")]

        [SerializeField] private BaseGestureDetector _gestureDetectorInitiate = null;

        [SerializeField] private BaseGestureDetector _gestureDetectorFinalize = null;

        [SerializeField] private float _timeForCompleteGesture = 0.5f;
        private float _timerForCompleteGesture = 0f;

        private enum State
        {
            WAIT_FOR_INITIATE = 0,
            WAIT_FOR_FINALIZE = 1,
            CHANGE_PALETTE_COLOR = 2,
        }
        private State _state = State.WAIT_FOR_INITIATE;

        // public event Action OnChangePaletteColor;

        protected override bool ShouldUpdate()
        {
            return base.ShouldUpdate() &&
                   Draw3D_Manager.UseGestureShortcuts;
        }

        public override bool IsUserMakingGestureStart()
        {
            return IsUserMakingGestureHold();
        }

        public override bool IsUserMakingGestureHold()
        {
            return _state == State.CHANGE_PALETTE_COLOR;
        }

        protected override void Awake()
        {
            base.Awake();

            _gestureDetectorInitiate.OnDetectStart += OnDetectStartInitiate;
            _gestureDetectorFinalize.OnDetectStart += OnDetectStartFinalize;
        }

        protected override void OnDestroy()
        {
            _gestureDetectorInitiate.OnDetectStart -= OnDetectStartInitiate;
            _gestureDetectorFinalize.OnDetectStart -= OnDetectStartFinalize;

            base.OnDestroy();
        }

        private void OnDetectStartInitiate()
        {
            // DebugLogUtilities.LogError(DebugLogUtilities.DebugLogType.DRAW_3D, "Change Palette Color - INITIATED");

            _state = State.WAIT_FOR_FINALIZE;
            _timerForCompleteGesture = 0f;
        }

        private void OnDetectStartFinalize()
        {
            if (_state == State.WAIT_FOR_FINALIZE && _timerForCompleteGesture < _timeForCompleteGesture)
            {
                // DebugLogUtilities.LogError(DebugLogUtilities.DebugLogType.DRAW_3D, "Change Palette Color - FINALIZED");

                _state = State.CHANGE_PALETTE_COLOR;
            }
        }

        protected override void Update()
        {
            base.Update();

            switch (_state)
            {
                // case State.WAIT_FOR_INITIATE:
                //     // UpdateWaitForInitializeState();
                //     break;
                case State.WAIT_FOR_FINALIZE:
                    UpdateWaitForFinalizeState();
                    break;
                // case State.CHANGE_PALETTE_COLOR:
                //     UpdateChangePaletteColorState();
                //     break;;
                default:
                    break;
            }
        }

        private void Reset()
        {
            _state = State.WAIT_FOR_INITIATE;
            _timerForCompleteGesture = 0f;
        }

        // private void UpdateWaitForInitializeState()
        // {
        //     if (_gestureDetectorInitiate.IsUserMakingGestureStart())
        //     {
        //         DebugLogUtilities.LogError(DebugLogUtilities.DebugLogType.DRAW_3D, "Change Palette Color - EXTEND ACTIVATED");
        //
        //         _state = State.WAIT_FOR_FINALIZE;
        //         _timerForCompleteGesture = 0f;
        //     }
        // }

        private void UpdateWaitForFinalizeState()
        {
            _timerForCompleteGesture += Time.deltaTime;
            if (_timerForCompleteGesture > _timeForCompleteGesture)
            {
                Reset();
            }
        }

        // private void UpdateChangePaletteColorState()
        // {
        //     DebugLogUtilities.LogError(DebugLogUtilities.DebugLogType.DRAW_3D, "Change Palette Color - CHANGE COLOR ACTIVATED");
        //
        //     Reset();
        // }
    }
}
