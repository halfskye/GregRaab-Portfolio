using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Avatar.ReadyPlayerMe
{
    public class VRRigHelper : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController animatorController;

        public RuntimeAnimatorController AnimatorController => animatorController;
        
        [SerializeField] private MultiParentConstraint head;
        [SerializeField] private TwoBoneIKConstraint leftArm;
        [SerializeField] private TwoBoneIKConstraint rightArm;

        public MultiParentConstraint Head => head;
        public TwoBoneIKConstraint LeftArm => leftArm;
        public TwoBoneIKConstraint RightArm => rightArm;

        [SerializeField] private VRHandTracking vrHandTrackingLeft;
        [SerializeField] private VRHandTracking vrHandTrackingRight;

        public VRHandTracking VRHandTrackingLeft => vrHandTrackingLeft;
        public VRHandTracking VRHandTrackingRight => vrHandTrackingRight;
    }
}
