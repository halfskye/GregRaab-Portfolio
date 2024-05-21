using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Context;
using Scripts.Utils;
using States;
using Tracking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Interaction
{
    public class TargetManager : MonoBehaviour
    {
        public enum TargetingType
        {
            CAMERA = 0,
        }

        [SerializeField] private TargetingType targetingType = TargetingType.CAMERA;

        [SerializeField] private TargetObjectData defaultTargetObjectData = null;

        // Debug settings:
        [SerializeField] private bool isDebugModeOn = false;
        [SerializeField] private float debugUpdateRate = .1f;
        private float _debugUpdateTimer = 0f;
        private LineRenderer _debugLineTarget = null;
        private List<LineRenderer> _debugLineRenderers = null;

        public Transform CenterEyeCamera { get; private set; } = null;

        private List<TargetObject> _registeredTargets;

        private bool _isApplicationContextValid = false;

        public delegate bool TargetPredicate(TargetObject target);

        #region Singleton

        private static TargetManager _instance;
        public static TargetManager Instance => _instance;

        public static TargetManager Get()
        {
            if (!_instance.IsNullOrDestroyed()) return _instance;
            
            var go = new GameObject("TargetManager");
            _instance = go.AddComponent<TargetManager>();
            return _instance;
        }

        #endregion //Singleton

        private void Awake()
        {
            //@TODO: Singleton behavior is probably messed up for scene reload, etc. Readdress.

            #region Singleton

            if (_instance.IsNotNull())
            {
                DebugLog("There should only ever be one TargetManager.", DebugLogUtilities.DebugLogType.ERROR);
                Destroy(this.gameObject);
                return;
            }

            _instance = this;

            #endregion Singleton

            CenterEyeCamera = Camera.main.transform;

            _registeredTargets = new List<TargetObject>();
            
            ApplicationContext.OnDidEnterState += OnApplicationContextDidEnterState;
        }

        private void OnDestroy()
        {
            ApplicationContext.OnDidEnterState -= OnApplicationContextDidEnterState;
        }

        private void OnApplicationContextDidEnterState(ApplicationState oldState, ApplicationState newState)
        {
            _isApplicationContextValid = newState == ApplicationState.Meditation;
        }

        //@DEBUG-ONLY
        private void Update()
        {
            UpdateDebugMode();
        }

        public void RegisterTarget(TargetObject target)
        {
            _registeredTargets.Add(target);
        }

        public void UnregisterTarget(TargetObject target)
        {
            _registeredTargets.Remove(target);
        }

        private List<TargetObject> GetTargets(Vector3 srcPosition, Vector3 srcDirection,
            int numTargets, TargetingType targetingType,
            TargetObject[] excludes = null,
            TargetPredicate targetPredicate = null)
        {
            if (targetingType == TargetingType.CAMERA)
            {
                srcPosition = CenterEyeCamera.position;
                srcDirection = CenterEyeCamera.forward;
            }

            //@TODO: Probably need to fix this for non-player.
            if (isDebugModeOn)
            {
                if (_debugLineTarget.IsNullOrDestroyed())
                {
                    LineRendererUtilities.CreateBasicDebugLineRenderer(
                        lineRenderer: ref _debugLineTarget,
                        colors: new[] {Color.green, Color.red},
                        width: 0.02f,
                        parent: this.transform
                    );
                }

                var positions = new[] {srcPosition, srcPosition + srcDirection.normalized * 2f};
                _debugLineTarget.SetPositions(positions);
                _debugLineTarget.enabled = true;

                DebugDrawPossibleTargets(srcPosition, srcDirection);
            }

            // Validate possible targets first:
            var targetObjects = GetValidRegisteredTargets(srcPosition, srcDirection, excludes, targetPredicate);
            SortTargetsByAimAngle(ref targetObjects, srcPosition, srcDirection);

            return targetObjects?.Count <= numTargets ? targetObjects : targetObjects?.GetRange(0, numTargets);
        }

        public List<TargetObject> GetTargets(Vector3 srcPosition, Vector3 srcDirection, int numTargets)
        {
            return GetTargets(srcPosition, srcDirection, numTargets, targetingType);
        }

        public TargetObject GetTarget(Vector3 srcPosition, Vector3 srcDirection, TargetingType targetingType,
            TargetObject[] excludes = null)
        {
            var targets = GetTargets(srcPosition, srcDirection, 1, targetingType, excludes);
            return targets?.Count > 0 ? targets[0] : null;
        }

        public TargetObject GetTarget(Vector3 srcPosition, Vector3 srcDirection, TargetObject[] excludes = null)
        {
            return GetTarget(srcPosition, srcDirection, targetingType, excludes);
        }

        public TargetObject GetTarget(TargetPredicate targetPredicate = null)
        {
            var targets = GetTargets(
                CenterEyeCamera.position,
                CenterEyeCamera.forward,
                1,
                TargetingType.CAMERA,
                excludes: null,
                targetPredicate
            );
            return targets?.Count > 0 ? targets[0] : null;
        }

        private List<TargetObject> GetValidRegisteredTargets(
            Vector3 srcPosition, Vector3 srcDirection,
            TargetObject[] excludes = null,
            TargetPredicate targetPredicate = null)
        {
            var targetObjects = new List<TargetObject>();

            if (!_isApplicationContextValid) return targetObjects;
            
            // Validate possible targets first:
            foreach (var target in _registeredTargets)
            {
                if (targetPredicate != null && !targetPredicate(target)) continue;
                if (!IsTargetIncluded(target, excludes)) continue;

                if (target.IsValid() &&
                    IsWithinMaxDistance(target, srcPosition) &&
                    IsWithinMaxAimAngle(target, srcPosition, srcDirection))
                {
                    targetObjects.Add(target);
                }
            }

            return targetObjects;
        }

        private bool IsTargetIncluded(TargetObject target, TargetObject[] excludedTargets)
        {
            return target != null && !target.IsSelf && (excludedTargets == null || !excludedTargets.Contains(target));
        }

        private void SortTargetsByAimAngle(
            ref List<TargetObject> targetObjects,
            Vector3 srcPosition,
            Vector3 srcDirection)
        {
            if (targetObjects?.Count > 0)
            {
                // Sort by aim angle:
                targetObjects = targetObjects.OrderBy(target =>
                {
                    var toTarget = target.GetPosition() - srcPosition;
                    return Vector3.Angle(toTarget, srcDirection);
                }).ToList();
            }
        }

        private bool IsWithinMaxAimAngle(TargetObject target, Vector3 srcPosition, Vector3 srcDirection)
        {
            var toTarget = target.GetPosition() - srcPosition;
            // if (defaultTargetObjectData.AimAngleCollapseYAxis)
            if (GetAimAngleCollapseYAxis(target))
            {
                toTarget.y = srcDirection.y = 0f;
            }

            return Vector3.Angle(toTarget, srcDirection) <= GetMaxAimAngle(target);
        }

        private bool IsWithinMaxDistance(TargetObject target, Vector3 srcPosition)
        {
            return (target.GetPosition() - srcPosition).sqrMagnitude <= GetMaxDistanceSqr(target);
        }

        private bool GetAimAngleCollapseYAxis(TargetObject target)
        {
            var targetObjectData = target.TargetObjectData;
            if (targetObjectData.IsNullOrDestroyed())
            {
                targetObjectData = defaultTargetObjectData;
            }

            return targetObjectData.AimAngleCollapseYAxis;
        }

        private float GetMaxAimAngle(TargetObject target)
        {
            var targetObjectData = target.TargetObjectData;
            if (targetObjectData.IsNullOrDestroyed())
            {
                targetObjectData = defaultTargetObjectData;
            }

            return targetObjectData.MaxAimAngle;
        }

        private float GetMaxDistanceSqr(TargetObject target)
        {
            var targetObjectData = target.TargetObjectData;
            if (targetObjectData.IsNullOrDestroyed())
            {
                targetObjectData = defaultTargetObjectData;
            }

            return targetObjectData.MaxDistanceSqr;
        }

        #region Debug

        [Conditional(DebugLogUtilities.DefaultDebugDefine)]
        private void DebugLog(string message, DebugLogUtilities.DebugLogType logType = DebugLogUtilities.DebugLogType.LOG, Object context = null)
        {
            DebugLogUtilities.Log(DebugLogUtilities.DebugInfoType.TARGETING, $"[SeatingManager] {message}", logType, context != null ? context : this);
        }

        private void UpdateDebugMode()
        {
            //@NOTE: Currently only have continuously updating debug visualizations for Camera mode.
            if (isDebugModeOn)
            {
                if (targetingType == TargetingType.CAMERA)
                {
                    UpdateDebugMode_TargetingTypeCamera();
                }
            }
            else
            {
                DisableAllDebugLineRenderers();
            }
        }

        private void UpdateDebugMode_TargetingTypeCamera()
        {
            _debugUpdateTimer += Time.deltaTime;
            if (_debugUpdateTimer >= debugUpdateRate)
            {
                _debugUpdateTimer = 0f;
                DebugDrawPossibleTargets(CenterEyeCamera.position, CenterEyeCamera.forward);
            }
        }

        private void DebugDrawPossibleTargets(Vector3 srcPosition, Vector3 srcDirection)
        {
            var targetObjects = GetValidRegisteredTargets(srcPosition, srcDirection);
            if (targetObjects?.Count > 0)
            {
                _debugLineRenderers ??= new List<LineRenderer>();
                var colors = new Color[] {Color.cyan, Color.blue};
                while (_debugLineRenderers.Count < targetObjects.Count)
                {
                    _debugLineRenderers.Add(
                        LineRendererUtilities.CreateBasicDebugLineRenderer(
                            colors: colors,
                            width: 0.01f,
                            parent: this.transform
                        )
                    );
                }

                for (var i = 0; i < _debugLineRenderers.Count; i++)
                {
                    if (i < targetObjects.Count)
                    {
                        LineRenderer lineRenderer = _debugLineRenderers[i];
                        var positions = new[] {srcPosition, targetObjects[i].GetPosition()};
                        lineRenderer.SetPositions(positions);
                        lineRenderer.enabled = true;
                    }
                    else
                    {
                        _debugLineRenderers[i].enabled = false;
                    }
                }
            }
            else
            {
                DebugLog("No Valid Targets");
                DisableAllDebugLineRenderers();
            }
        }

        private void DisableAllDebugLineRenderers()
        {
            DisablePossibleTargetDebugLineRenderers();

            if (!_debugLineTarget.IsNullOrDestroyed())
            {
                _debugLineTarget.enabled = false;
            }
        }

        private void DisablePossibleTargetDebugLineRenderers()
        {
            if (_debugLineRenderers?.Count > 0)
            {
                foreach (LineRenderer lineRenderer in _debugLineRenderers)
                {
                    lineRenderer.enabled = false;
                }
            }
        }

        #endregion Debug
    }
}