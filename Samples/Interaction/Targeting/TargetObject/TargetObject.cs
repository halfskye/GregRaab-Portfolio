using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction
{
    public class TargetObject : MonoBehaviour
    {
        [SerializeField] private TargetObjectData targetObjectData = null;
        public TargetObjectData TargetObjectData => targetObjectData;

        //@TODO: Re-add subTargetOverrides w/ different Serialized Dictionary solution or something similar:
        // [SerializeField] private SerializableDictionary<SubTargetType, Transform> subTargetOverrides = null;

        private Transform Transform { set; get; }

        public enum SubTargetType
        {
            DEFAULT = 0,
            CHEST = 1,
            LEFT_HAND = 2,
            RIGHT_HAND = 3,
            HANDS = 4,
        }

        public bool IsSelf = false;

        private Dictionary<SubTargetType, Transform> subTargets = null;

        // Priority order of fallback Default SubTargetType (from highest to lowest):
        private static List<SubTargetType> SubTargetTypeDefaultPriority = new List<SubTargetType>()
        {
            SubTargetType.CHEST,
            SubTargetType.RIGHT_HAND,
            SubTargetType.LEFT_HAND,
        };

        private void Awake()
        {
            Transform = this.transform;

            // if (subTargetOverrides != null && subTargetOverrides.Count > 0)
            // {
            //     subTargets = subTargetOverrides.ToDictionary(x => x.Key, x => x.Value);
            //     ValidateSubTargetsAndSetDefault();
            // }
            // else
            {
                SetDefaultSubTarget(Transform);
            }
        }

        private void Start()
        {
            TargetManager.Get().RegisterTarget(this);
        }

        private void OnDestroy()
        {
            if (TargetManager.Instance != null)
            {
                TargetManager.Get().UnregisterTarget(this);
            }
        }

        //@TODO: Extend this to account for target types, etc.
        public virtual bool IsValid()
        {
            return isActiveAndEnabled;
        }

        private void InitializeSubTargets()
        {
            subTargets ??= new Dictionary<SubTargetType, Transform>();
        }

        private void SetDefaultSubTarget(Transform subTargetTransform)
        {
            InitializeSubTargets();
            subTargets[SubTargetType.DEFAULT] = subTargetTransform;
        }

        public Vector3 GetPosition()
        {
            return GetDefaultSubTargetPosition();
        }

        private Vector3 GetDefaultSubTargetPosition()
        {
            return subTargets[SubTargetType.DEFAULT].position;
        }

        public Vector3 GetPositionBySubTarget(SubTargetType subTargetType)
        {
            if (TryGetSpecialSubTargetTypePosition(subTargetType, out Vector3 position))
            {
                return position;
            }

            if (subTargets.TryGetValue(subTargetType, out var subTargetTransform))
            {
                return subTargetTransform.position;
            }
            else
            {
                return GetDefaultSubTargetPosition();
            }
        }

        private bool TryGetSpecialSubTargetTypePosition(SubTargetType subTargetType, out Vector3 position)
        {
            if (subTargetType == SubTargetType.HANDS)
            {
                position = GetPositionBySubTargetHands(source: TargetManager.Instance.CenterEyeCamera.position);
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        public Vector3 GetPositionBySubTargetHands(Vector3 source)
        {
            return GetPositionByClosestSubTarget(source, new List<SubTargetType>() {SubTargetType.LEFT_HAND, SubTargetType.RIGHT_HAND});
        }

        private Vector3 GetPositionByClosestSubTarget(Vector3 source, List<SubTargetType> subTargetTypes)
        {
            var subTargetMap = subTargetTypes.ToDictionary(x => x, x => Vector3.SqrMagnitude(source - GetPositionBySubTarget(x)));
            var closestSubTarget = subTargetMap.Aggregate((l,r) => l.Value < r.Value ? l : r).Key;

            return GetPositionBySubTarget(closestSubTarget);
        }

        // public void SetSubTargets(Dictionary<SubTargetType, Transform> subTargets)
        // {
        //     if (subTargetOverrides == null || subTargetOverrides.Count < 1)
        //     {
        //         this.subTargets = new Dictionary<SubTargetType, Transform>(subTargets);
        //
        //         ValidateSubTargetsAndSetDefault();
        //     }
        // }

        private void ValidateSubTargetsAndSetDefault()
        {
            InitializeSubTargets();

            if (!subTargets.ContainsKey(SubTargetType.DEFAULT))
            {
                if (SubTargetTypeDefaultPriority.Any(x => subTargets.ContainsKey(x)))
                {
                    var defaultSubTypeTarget = SubTargetTypeDefaultPriority.First(x => subTargets.ContainsKey(x));
                    SetDefaultSubTarget(subTargets[defaultSubTypeTarget]);
                }
                else
                {
                    SetDefaultSubTarget(Transform);
                }
            }
        }
    }
}
