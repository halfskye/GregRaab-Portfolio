using Tracking;
using Utils;
using System.Collections;
using UnityEngine;

namespace Draw3D.UI
{
    public class Draw3D_WatchUI : MonoBehaviour
    {
        private const string AnimatorIsVisibleKey = "IsVisible";

        [SerializeField]
        private float showUIAngleThreshold = 45f;

        [SerializeField]
        public Animator animator;

        private Transform _cameraTransform;
        private static readonly int AnimatorIsVisibleKeyHash = Animator.StringToHash(AnimatorIsVisibleKey);
        private bool IsValidApplicationState { get; set; }

        private bool IsFacingCamera
        {
            get
            {
                var thisTransform = this.transform;
                var direction = (_cameraTransform.position - thisTransform.position).normalized;
                return Vector3.Angle(-1 * thisTransform.forward, direction) < showUIAngleThreshold;
            }
        }

        private bool ShouldShowUp => Draw3D_Manager.IsDraw3DValid &&
                                     IsValidApplicationState &&
                                     IsFacingCamera &&
                                     !ApplicationManager.Instance.FusionSceneManager.IsLoadingScene &&
                                     HandsManagerHelper.Instance.HandPresenceCheck(Chirality.Right);

        private void Awake()
        {
            ApplicationManager.OnDidEnterState += OnApplicationStateChanged;
        }

        private void OnDestroy()
        {
            ApplicationManager.OnDidEnterState -= OnApplicationStateChanged;
        }

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            animator.SetBool(AnimatorIsVisibleKeyHash, ShouldShowUp);
        }

        private void OnApplicationStateChanged(ApplicationState oldState, ApplicationState newState)
        {
            if (ApplicationManager.CurrentState == ApplicationState.Center)
            {
                StartCoroutine(WaitForSceneChange(oldState, newState, 0f));
            }
            else
            {
                StartCoroutine(WaitForSceneChange(oldState, newState, 2f));
            }
        }

        private IEnumerator WaitForSceneChange(ApplicationState oldState, ApplicationState newState, float delay)
        {
            yield return new WaitForSeconds(delay);
            IsValidApplicationState = newState switch
            {
                ApplicationState.Center => false,
                ApplicationState.Labs => true,
                ApplicationState.FloatingIsland => false,
                _ => false
            };
            StopCoroutine("WaitForSceneChange");
        }
    }
}
