using System.Collections.Generic;
using UnityEngine;
using Tracking;

public class GestureDetectionHandFinder : MonoBehaviour
{
    // Please note: this is an old, fragile class, and breaks easily when modified.
    // It's recommended to instead use HandPartManager.Instance.GetHandPart to get a reference to any hand part.
    // This script will be removed as part of ongoing Gesture/ParticleMessenger work.

    public enum FingerType
    {
        Thumb = 0,
        Index = 1,
        Middle = 2,
        Ring = 3,
        Little = 4,
    }

    public Transform face { get; private set; }

    public DiscreteHandModel leftHandModel { get; private set; } = null;
    public DiscreteHandModel rightHandModel { get; private set; } = null;

    public List<Transform> leftHandFingertips { get; private set; } = new List<Transform>();
    public List<Transform> rightHandFingertips { get; private set; } = new List<Transform>();

    public Transform FingerTip(Chirality chirality, FingerType type) =>
        chirality == Chirality.Left ?
            leftHandFingertips[(int)type] :
            rightHandFingertips[(int)type];

    public List<Transform> leftHandKnuckles { get; private set; } = new List<Transform>();
    public List<Transform> rightHandKnuckles { get; private set; } = new List<Transform>();

    public Transform Knuckle(Chirality chirality, FingerType type) =>
        chirality == Chirality.Left ?
            leftHandKnuckles[(int)type] :
            rightHandKnuckles[(int)type];

    public Transform leftHandPalm { get; private set; } = null;
    public Transform rightHandPalm { get; private set; } = null;
    public Transform HandPalm(Chirality chirality) => chirality == Chirality.Left ? leftHandPalm : rightHandPalm;

    public Transform leftHandWrist { get; private set; } = null;
    public Transform rightHandWrist { get; private set; } = null;
    public Transform HandWrist(Chirality chirality) => chirality == Chirality.Left ? leftHandWrist : rightHandWrist;

    private void Update()
    {
        FindNullHandsAndCamera(Chirality.Left);
        FindNullHandsAndCamera(Chirality.Right);
    }

    private void FindNullHandsAndCamera(Chirality chirality)
    {
        if (face == null)
        {
            face = Camera.main.transform;
        }

        if (chirality == Chirality.Left && leftHandModel == null || leftHandFingertips.Count == 0)
        {
            FindHandsInScene(chirality);
            FindHandPartsInScene(chirality);
        }

        if (chirality == Chirality.Right && rightHandModel == null || rightHandFingertips.Count == 0)
        {
            FindHandsInScene(chirality);
            FindHandPartsInScene(chirality);
        }
    }

    private void FindHandsInScene(Chirality chirality)
    {
        // This assumes either that only one graphics DiscreteHandModel per chirality will be in the scene, or that it won't matter if there are multiple
        var handModels = FindObjectsOfType<DiscreteHandModel>();

        if (chirality == Chirality.Left)
        {
            foreach (DiscreteHandModel handModel in handModels)
            {
                if (handModel.chirality == Chirality.Left && handModel.type == HandModelType.Graphics)
                {
                    leftHandModel = handModel;

                    // var leftHandState = new OVRPlugin.HandState(); // This can be used to get the hand size, but isn't currently being used
                    // OVRPlugin.GetHandState(0, OVRPlugin.Hand.HandLeft, ref leftHandState);
                }
            }
        }
        else
        {
            foreach (DiscreteHandModel handModel in handModels)
            {
                if (handModel.chirality == Chirality.Right && handModel.type == HandModelType.Graphics)
                {
                    rightHandModel = handModel;

                    // var rightHandState = new OVRPlugin.HandState(); // This can be used to get the hand size, but isn't currently being used
                    // OVRPlugin.GetHandState(0, OVRPlugin.Hand.HandRight, ref rightHandState);
                }
            }
        }
    }

    private void FindHandPartsInScene(Chirality chirality)
    {
        if (chirality == Chirality.Left)
        {
            if (leftHandModel)
            {
                leftHandFingertips.Add(leftHandModel.thumb.tip);
                leftHandFingertips.Add(leftHandModel.index.tip);
                leftHandFingertips.Add(leftHandModel.middle.tip);
                leftHandFingertips.Add(leftHandModel.ring.tip);
                leftHandFingertips.Add(leftHandModel.little.tip);

                leftHandKnuckles.Add(leftHandModel.thumb.middleJoint);
                leftHandKnuckles.Add(leftHandModel.index.middleJoint);
                leftHandKnuckles.Add(leftHandModel.middle.middleJoint);
                leftHandKnuckles.Add(leftHandModel.ring.middleJoint);
                leftHandKnuckles.Add(leftHandModel.little.middleJoint);

                leftHandPalm = leftHandModel.palmCenter.transform;
                leftHandWrist = leftHandModel.wristCenter.transform;
            }
        }
        else if (chirality == Chirality.Right)
        {
            if (rightHandModel)
            {
                rightHandFingertips.Add(rightHandModel.thumb.tip);
                rightHandFingertips.Add(rightHandModel.index.tip);
                rightHandFingertips.Add(rightHandModel.middle.tip);
                rightHandFingertips.Add(rightHandModel.ring.tip);
                rightHandFingertips.Add(rightHandModel.little.tip);

                rightHandKnuckles.Add(rightHandModel.thumb.middleJoint);
                rightHandKnuckles.Add(rightHandModel.index.middleJoint);
                rightHandKnuckles.Add(rightHandModel.middle.middleJoint);
                rightHandKnuckles.Add(rightHandModel.ring.middleJoint);
                rightHandKnuckles.Add(rightHandModel.little.middleJoint);

                rightHandPalm = rightHandModel.palmCenter.transform;
                rightHandWrist = rightHandModel.wristCenter.transform;
            }
        }
    }
}
