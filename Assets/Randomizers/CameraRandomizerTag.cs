using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

// Add this Component to any GameObject that you would like to be randomized. This class must have an identical name to
// the .cs file it is defined in.
[RequireComponent(typeof(Camera))]
public class CameraRandomizerTag : RandomizerTag {}

[Serializable]
[AddRandomizerMenu("CameraRandomizer")]
public class CameraRandomizer : Randomizer
{
    public Vector3Parameter targetPosition;
    public Vector3Parameter targetShift;
    public FloatParameter cameraPitch;
    public FloatParameter cameraRoll;
    public FloatParameter distanceFromTarget;

    protected override void OnIterationStart()
    {
        var tags = tagManager.Query<CameraRandomizerTag>();
        foreach (var tag in tags)
        {
            Vector3 tempTarget = targetPosition.Sample() + targetShift.Sample();
            tag.transform.position = new Vector3(0, 0, distanceFromTarget.Sample()) + tempTarget;
            tag.transform.RotateAround(tempTarget, Vector3.left, cameraPitch.Sample());
            tag.transform.LookAt(tempTarget);
            tag.transform.Rotate(new Vector3(0, 0, cameraRoll.Sample()), Space.Self);
        }

    }
}
