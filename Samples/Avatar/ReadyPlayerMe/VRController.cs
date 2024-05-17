using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Emerge.Connect.Avatar.ReadyPlayerMe
{
    public enum HandType
    {
        Left,
        Right
    }

    [RequireComponent(typeof(Animator))]
    public class VRController : MonoBehaviour
    {
        [SerializeField] private HandType _handType;
        [SerializeField] private float _thumbSpeed = 0.1f;

        private Animator _animator;
        private InputDevice _inputDevice;

        private float _indexValue;
        private float _thumbValue;
        private float _threeFingersValue;
        private static readonly int IndexAnimatorKey = Animator.StringToHash("Index");
        private static readonly int ThreeFingersAnimatorKey = Animator.StringToHash("ThreeFingers");
        private static readonly int ThumbAnimatorKey = Animator.StringToHash("Thumb");
        private bool _isInitialized;

        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // If the user is using Hand Tracking, disable the animation controller & return
            var isUsingHandTracking = VRRigController.IsUsingHandTracking;
            _animator.enabled = !isUsingHandTracking;
            if (isUsingHandTracking)
                return;

            AnimateHand();
        }

        private InputDevice GetInputDevice()
        {
            var controllerCharacteristic = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;

            if (_handType == HandType.Left)
            {
                controllerCharacteristic |= InputDeviceCharacteristics.Left;
            }
            else
            {
                controllerCharacteristic |= InputDeviceCharacteristics.Right;
            }

            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristic, inputDevices);

            if (inputDevices.Count == 0)
                return new InputDevice();

            _isInitialized = true;
            return inputDevices[0];
        }

        private void AnimateHand()
        {
            if (_isInitialized == false)
            {
                _inputDevice = GetInputDevice();
                return;
            }

            _inputDevice.TryGetFeatureValue(CommonUsages.trigger, out _indexValue);
            _inputDevice.TryGetFeatureValue(CommonUsages.grip, out _threeFingersValue);

            _inputDevice.TryGetFeatureValue(CommonUsages.primaryTouch, out bool primaryTouched);
            _inputDevice.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool secondaryTouched);

            if (primaryTouched || secondaryTouched)
            {
                _thumbValue += _thumbSpeed;
            }
            else
            {
                _thumbValue -= _thumbSpeed;
            }

            _thumbValue = Mathf.Clamp(_thumbValue, 0, 1);

            _animator.SetFloat(IndexAnimatorKey, _indexValue);
            _animator.SetFloat(ThreeFingersAnimatorKey, _threeFingersValue);
            _animator.SetFloat(ThumbAnimatorKey, _thumbValue);
        }
    }
}
