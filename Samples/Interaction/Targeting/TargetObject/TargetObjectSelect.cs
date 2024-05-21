using Tracking;
using UnityEngine;
using UnityEngine.Events;

namespace Interaction
{
    public class TargetObjectSelect : TargetObject
    {
        [SerializeField] private TargetObjectSelectData targetObjectSelectData = null;
        private TargetObjectSelectData _defaultTargetObjectSelectData = null;

        [SerializeField] private Transform selectTransformOverride = null;
        
        private float _stateTimer = 0f;
        
        public UnityEvent onSelect;

        private GameObject _fx = null;
        
        private enum State
        {
            IDLE = 0,
            FOCUS = 1,
            UNFOCUS = 2,
            SELECT = 3,
        };
        private State _state = State.IDLE;
        
        private void Update()
        {
            switch (_state)
            {
                case State.IDLE:
                    break;
                case State.FOCUS:
                    UpdateFocus();
                    break;
                case State.UNFOCUS:
                    UpdateUnfocus();
                    break;
                case State.SELECT:
                    UpdateSelect();
                    break;
            }
        }
        
        public void Focus(TargetObjectSelectData defaultTargetObjectSelectData = null)
        {
            Reset();
            
            _defaultTargetObjectSelectData = defaultTargetObjectSelectData;

            _state = State.FOCUS;
            _stateTimer = GetFocusTime();
            
            InstantiateFX(GetFocusFX());
        }

        private void UpdateFocus()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                Select();
            }
        }

        public void Unfocus(TargetObjectSelectData defaultTargetObjectSelectData = null)
        {
            var wasSelected = _state == State.SELECT;
            Reset();
            if (wasSelected) return;
            
            _defaultTargetObjectSelectData = defaultTargetObjectSelectData;

            _state = State.UNFOCUS;
            _stateTimer = GetUnfocusTime();

            InstantiateFX(GetUnfocusFX());
        }

        private void UpdateUnfocus()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                Reset();
            }
        }

        public void Select(TargetObjectSelectData defaultTargetObjectSelectData = null)
        {
            Reset();
            
            _defaultTargetObjectSelectData = defaultTargetObjectSelectData;

            _state = State.SELECT;
            
            onSelect?.Invoke();
            
            InstantiateFX(GetSelectFX());
        }

        private void UpdateSelect()
        {
        }

        private float GetFocusTime()
        {
            if (targetObjectSelectData != null)
            {
                return targetObjectSelectData.FocusTime;
            }
            
            return _defaultTargetObjectSelectData != null ? _defaultTargetObjectSelectData.FocusTime : 0f;
        }

        private float GetUnfocusTime()
        {
            if (targetObjectSelectData != null)
            {
                return targetObjectSelectData.UnfocusTime;
            }
            
            return _defaultTargetObjectSelectData != null ? _defaultTargetObjectSelectData.UnfocusTime : 0f;
        }
        
        private void Reset()
        {
            _state = State.IDLE;
            
            if (!_fx.IsNullOrDestroyed())
            {
                GameObject.Destroy(_fx);
                _fx = null;
            }
        }

        #region FX

        private void InstantiateFX(GameObject fx)
        {
            if (!fx.IsNullOrDestroyed())
            {
                _fx = GameObject.Instantiate(fx, GetSelectTransform());
            }
        }

        private Transform GetSelectTransform()
        {
            return selectTransformOverride != null ? selectTransformOverride : this.transform;
        }
        
        private GameObject GetFocusFX()
        {
            if (targetObjectSelectData != null && targetObjectSelectData.FocusFX != null)
            {
                return targetObjectSelectData.FocusFX;
            }
            
            return _defaultTargetObjectSelectData != null ? _defaultTargetObjectSelectData.FocusFX : null;
        }

        private GameObject GetUnfocusFX()
        {
            if (targetObjectSelectData != null && targetObjectSelectData.UnfocusFX != null)
            {
                return targetObjectSelectData.UnfocusFX;
            }
            
            return _defaultTargetObjectSelectData != null ? _defaultTargetObjectSelectData.UnfocusFX : null;
        }

        private GameObject GetSelectFX()
        {
            if (targetObjectSelectData != null && targetObjectSelectData.SelectFX != null)
            {
                return targetObjectSelectData.SelectFX;
            }
            
            return _defaultTargetObjectSelectData != null ? _defaultTargetObjectSelectData.SelectFX : null;
        }

        #endregion FX
    }
}