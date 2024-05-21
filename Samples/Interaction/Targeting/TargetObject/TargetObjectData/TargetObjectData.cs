using UnityEngine;

namespace Interaction
{
    [CreateAssetMenu(fileName = "TargetObjectData", menuName = "Targeting/new TargetObjectData", order = 0)]
    public class TargetObjectData : ScriptableObject
    {
        [SerializeField, Range(0f, 90f)] private float maxAimAngle = 15f;
        public float MaxAimAngle => maxAimAngle;
        
        [SerializeField] private bool aimAngleCollapseYAxis = true;
        public bool AimAngleCollapseYAxis => aimAngleCollapseYAxis;
        
        [SerializeField] private float maxDistance = 5f;
        public float MaxDistance => maxDistance;
        public float MaxDistanceSqr => maxDistance * maxDistance;
    }
}