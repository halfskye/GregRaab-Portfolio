using UnityEngine;

namespace Draw3D.GestureDetection.Gestures
{
    public class BaseGestureDetectorDraw3D : BaseGestureDetector
    {
        protected override bool ShouldUpdate()
        {
            var shouldUpdate = Draw3D_Manager.IsDraw3DValid &&
                               base.ShouldUpdate();
            // Debug.LogError($"BaseGestureDetector::Update - ShouldUpdate: {shouldUpdate}");

            return shouldUpdate;
        }
    }
}
