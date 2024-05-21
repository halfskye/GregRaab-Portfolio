using Tracking;
using Utils;
using UnityEngine;

namespace Draw3D.Brushes
{
    public class Draw3D_Pen : MonoBehaviour
    {
        [SerializeField] private Transform visual = null;
        public Transform Visual => visual;
        [SerializeField] private Transform tip = null;

        [SerializeField] private TactileCluster tactileCluster = null;

        [SerializeField] private Chirality chirality = Chirality.Right;

        [SerializeField] private float moveIncrement = 0.01f;
        [SerializeField] private float moveMaxExtent = 0.1f;
        private float moveCurrentPosition = 0.0f;

        private float _activeTactilityTimer = 0f;

        private bool IsValidApplicationState { get; set; }
        private bool IsDrawing { get; set; }

        private Draw3D_Brush Brush => Draw3D_BrushManager.Instance.SelectedBrush;

        public Transform DrawPoint => tip;

        private bool ShouldShowUp => Draw3D_Manager.IsDraw3DValid &&
                                     IsDrawing &&
                                     IsValidApplicationState &&
                                     !ApplicationManager.Instance.FusionSceneManager.IsLoadingScene &&
                                     HandsManagerHelper.Instance.HandPresenceCheck(Chirality.Right) &&
                                     ApplicationManager.CurrentState == ApplicationState.Labs;

        //@TEMP/@DEBUG:
        private bool _eraserMode = true;

        private void Awake()
        {
            ApplicationManager.OnDidEnterState += OnApplicationStateChanged;

            Draw3D_Manager.OnStrokeStart += OnStrokeStart;
            Draw3D_Manager.OnStrokeUpdate += OnStrokeUpdate;
            Draw3D_Manager.OnStrokeEnd += OnStrokeEnd;
        }

        private void OnDestroy()
        {
            ApplicationManager.OnDidEnterState -= OnApplicationStateChanged;

            Draw3D_Manager.OnStrokeStart -= OnStrokeStart;
            Draw3D_Manager.OnStrokeUpdate -= OnStrokeUpdate;
            Draw3D_Manager.OnStrokeEnd -= OnStrokeEnd;
        }

        private void Update()
        {
            UpdateTactility();
            UpdateVisibility();

            #if UNITY_EDITOR
            UpdateDebugInput();
            #endif
        }

        private void OnApplicationStateChanged(ApplicationState oldState, ApplicationState newState)
        {
            IsValidApplicationState = newState switch
            {
                ApplicationState.Center => false,
                ApplicationState.Labs => true,
                ApplicationState.FloatingIsland => true,
                _ => false
            };
        }

        private void OnStrokeStart()
        {
            IsDrawing = true;

            if (!Draw3D_BrushManager.IsEraserActive)
            {
                StartActiveTactility();
            }
            //@NOTE: Disable initial Eraser tactility for now:
            // else
            // {
            //     StartEraserTactility();
            // }
        }

        private void OnStrokeUpdate()
        {
            if (!Draw3D_BrushManager.IsEraserActive)
            {
                _activeTactilityTimer = Brush.ActiveTactilityDuration;

                if (!tactileCluster.TactilityEnabled)
                {
                    tactileCluster.TactilityEnabled = true;
                }
            }
            else
            {
                StartEraserTactility();
            }
        }

        private void OnStrokeEnd()
        {
            IsDrawing = false;

            //@NOTE: Tactility disabling is handled by Update
        }

        private void StartActiveTactility()
        {
            if (!Brush.ActiveTactility.IsNullOrDestroyed())
            {
                Brush.ActiveTactility.StartTactility(tactileCluster, chirality);

                _activeTactilityTimer = Brush.ActiveTactilityDuration;
            }
            else if(tactileCluster.TactilityEnabled)
            {
                tactileCluster.TactilityEnabled = false;
            }
        }

        private void StartIdleTactility()
        {
            if (!Brush.IdleTactility.IsNullOrDestroyed())
            {
                Brush.IdleTactility.StartTactility(tactileCluster, chirality);
            }
            else if(tactileCluster.TactilityEnabled)
            {
                tactileCluster.TactilityEnabled = false;
            }
        }

        private void StartEraserTactility()
        {
            TactilePulseController.Instance.PulseTravelDistance = 0.1f;
            TactilePulseController.Instance.StartTactilePulseJointTowardsJoint(chirality,
                _eraserMode ? AbstractHandPartIdentifier.HandPartType.IndexTip : AbstractHandPartIdentifier.HandPartType.IndexJointMCP,
                _eraserMode ? AbstractHandPartIdentifier.HandPartType.MiddleJointMCP : AbstractHandPartIdentifier.HandPartType.IndexTip);
        }

        private void UpdateTactility()
        {
            if (!Draw3D_BrushManager.IsEraserActive)
            {
                if (_activeTactilityTimer > 0f)
                {
                    _activeTactilityTimer -= Time.deltaTime;

                    if (_activeTactilityTimer < 0f)
                    {
                        StartIdleTactility();
                    }
                    else
                    {
                        UpdateActiveTactility();
                    }
                }
                else if (IsDrawing)
                {
                    UpdateIdleTactility();
                }
                else
                {
                    tactileCluster.TactilityEnabled = false;
                }
            }
        }

        private void UpdateActiveTactility()
        {
            if (!Brush.ActiveTactility.IsNullOrDestroyed())
            {
                Brush.ActiveTactility.UpdateTactility();
            }
            else if(tactileCluster.TactilityEnabled)
            {
                tactileCluster.TactilityEnabled = false;
            }
        }

        private void UpdateIdleTactility()
        {
            if (!Brush.IdleTactility.IsNullOrDestroyed())
            {
                Brush.IdleTactility.UpdateTactility();
            }
            else if(tactileCluster.TactilityEnabled)
            {
                tactileCluster.TactilityEnabled = false;
            }
        }

        private void UpdateVisibility()
        {
            visual.gameObject.SetActive(ShouldShowUp);

            // animator.SetBool(AnimatorIsVisibleKeyHash, ShouldShowUp);
        }

        private void Move(float distance)
        {
            var newPosition = distance + moveCurrentPosition;
            if (newPosition < 0f || newPosition > moveMaxExtent)
            {
                return;
            }

            var penTransform = Visual.transform;
            penTransform.position -= (penTransform.forward * distance);
            moveCurrentPosition = newPosition;
        }

        public void MoveForward()
        {
            Move(moveIncrement);
        }

        public void MoveBackward()
        {
            Move(-moveIncrement);
        }

        #if UNITY_EDITOR
        private void UpdateDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                tactileCluster.UseVisualization = !tactileCluster.UseVisualization;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                _eraserMode = !_eraserMode;
            }
        }
        #endif //UNITY_EDITOR
    }
}
