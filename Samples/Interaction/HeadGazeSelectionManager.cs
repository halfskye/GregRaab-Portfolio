using Tracking;
using UnityEngine;

namespace Interaction
{
    public class HeadGazeSelectionManager : MonoBehaviour
    {
        [SerializeField] private TargetObjectSelectData defaultTargetObjectSelectData = null;
        
        [SerializeField] private float updateTargetRate = 0.01f;
        private float _updateTargetTimer = 0f;

        private TargetObjectSelect _currentTarget = null;

        //@TODO: Events for NewTargetSelectionFocus, UpdateTargetSelectionFocus, TargetSelected, TargetDeselected, etc

        private void Update()
        {
            UpdateTargeting();
        }

        private void UpdateTargeting()
        {
            // Throttled polling of TargetingManager for Selection TargetObjects
            _updateTargetTimer += Time.deltaTime;
            if (_updateTargetTimer < updateTargetRate) return;
            
            _updateTargetTimer = 0f;

            var target = TargetManager.Instance.GetTarget(
                targetPredicate: o => o is TargetObjectSelect select && !select.IsNullOrDestroyed()
            );
            if (!target.IsNullOrDestroyed())
            {
                if (!_currentTarget.IsNullOrDestroyed())
                {
                    if (target != _currentTarget)
                    {
                        UnfocusCurrentTarget();
                        
                        FocusOnTarget(target as TargetObjectSelect);
                    }
                }
                else
                {
                    FocusOnTarget(target as TargetObjectSelect);
                }
            }
            else
            {
                UnfocusCurrentTarget();
            }
        }

        private void FocusOnTarget(TargetObjectSelect target)
        {
            _currentTarget = target;
            _currentTarget.Focus(defaultTargetObjectSelectData);
        }

        private void UnfocusCurrentTarget()
        {
            if (_currentTarget.IsNullOrDestroyed()) return;
            
            _currentTarget.Unfocus(defaultTargetObjectSelectData);
            _currentTarget = null;
        }
    }
}