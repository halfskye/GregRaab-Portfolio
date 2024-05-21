using System;
using Draw3D.GestureDetection.Gestures;
using UnityEngine;

namespace Draw3D.GestureDetection
{
    public class Draw3D_GestureDetectionManager : MonoBehaviour
    {
        [SerializeField]
        private GestureDetectionHandFinder _handFinder = null;
        public GestureDetectionHandFinder HandFinder => _handFinder;

        [SerializeField]
        private GestureDetectorActiveStroke _activeStrokeGesture = null;

        [SerializeField]
        private GestureDetectorChangePaletteColor _changePaletteGesture = null;

        [SerializeField]
        private GestureDetectorChangePaletteColor _changePaletteColorGesture = null;

        private bool _isDrawingActive = false;

        public Transform DrawingPoint => _activeStrokeGesture.DrawPoint;
        public Transform PaletteHand => _changePaletteGesture.HandPalm;

        public event Action OnStrokeStart;
        public event Action OnStrokeEnd;

        public event Action OnPaletteChangeGesture;
        public event Action OnPaletteColorChangeGesture;

        public static Draw3D_GestureDetectionManager Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Draw3D_GestureDetectionManager already exists. There should only be one.");
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            _changePaletteGesture.OnDetectStart += OnDetectGestureStart_ChangePalette;
            _changePaletteColorGesture.OnDetectStart += OnDetectGestureStart_ChangePaletteColor;
        }

        private void OnDestroy()
        {
            _changePaletteGesture.OnDetectStart -= OnDetectGestureStart_ChangePalette;
            _changePaletteColorGesture.OnDetectStart -= OnDetectGestureStart_ChangePaletteColor;
        }

        private void Update()
        {
            // UpdatePaletteState();

            UpdateDrawingState();
        }

        //@TODO: Update to use Start and End Detect events
        private void UpdateDrawingState()
        {
            var isMakingDrawingGesture = IsMakingDrawingGesture();

            if (isMakingDrawingGesture)
            {
                if (!_isDrawingActive)
                {
                    // Draw3D_Manager.DebugLogError("Drawing State - ACTIVE", this);
                    _isDrawingActive = true;
                    OnStrokeStart?.Invoke();
                }
            }
            else
            {
                if (_isDrawingActive)
                {
                    // Draw3D_Manager.DebugLogError("Drawing State - FALSE", this);
                    _isDrawingActive = false;
                    OnStrokeEnd?.Invoke();
                }
            }
        }

        public bool IsMakingDrawingGesture()
        {
            //@TODO: Change to Initiate step first w/ Left Hand, then Drawing gesture
            return _activeStrokeGesture.IsUserMakingGestureHold();
        }

        private void OnDetectGestureStart_ChangePalette()
        {
            Draw3D_Manager.DebugLogError("CHANGE PALETTE - ACTIVATED", this);

            OnPaletteChangeGesture?.Invoke();
        }

        private void OnDetectGestureStart_ChangePaletteColor()
        {
            Draw3D_Manager.DebugLogError("CHANGE PALETTE COLOR - ACTIVATED", this);

            OnPaletteColorChangeGesture?.Invoke();
        }

        // private void UpdatePaletteState()
        // {
        //     if (IsMakingChangePaletteColorGesture())
        //     {
        //     }
        // }

        // private bool IsMakingChangePaletteColorGesture()
        // {
        //     return _changePaletteColorGesture.IsUserMakingGestureStart();
        // }

        //@TEMP/@DEBUG
        // private void Update()
        // {
        //     var indexTip = _handFinder.rightHandIndexTip;
        //     if (indexTip != null)
        //     {
        //         var indexTipPosition = indexTip.position;
        //         Debug.DrawRay(indexTipPosition, indexTip.forward, Color.red);
        //         Debug.DrawRay(indexTipPosition, indexTip.up, Color.blue);
        //         Debug.DrawRay(indexTipPosition, indexTip.right, Color.white);
        //     }
        // }
    }
}
