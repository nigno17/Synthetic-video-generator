using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.CV.SyntheticHumans.Randomizers;
//using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("MyScenarioLezione")]
    public class MyScenarioLezione : PerceptionScenario<MyScenarioLezione.Constants>
    {
        [Serializable]
        public class Constants : ScenarioConstants
        {
            [Tooltip("The number of iterations to run.")]
            public int iterationCount = 100;
        }

        [Tooltip("The number of frames to render per iteration.")]
        public int framesPerIteration = 1;


        /// <inheritdoc/>
        protected override bool isScenarioComplete
        {
            get
            {
                return currentIteration >= constants.iterationCount;
            }
        }

        /// <inheritdoc/>
        protected override bool isIterationComplete
        {
            get
            {
                AnimationRandLezione tempRand = GetRandomizer<AnimationRandLezione>();
                //SyntheticHumanAnimationRandomizer tempRand = GetRandomizer<SyntheticHumanAnimationRandomizer>();
                if(tempRand is not null)
                    return (currentIterationFrame >= framesPerIteration) || tempRand.isAnimationComplete;
                else
                    return currentIterationFrame >= framesPerIteration;
            }

        }
    }
}
