using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using System.Collections.Generic;

// Add this Component to any GameObject that you would like to be randomized. This class must have an identical name to
// the .cs file it is defined in.
[RequireComponent(typeof(Animator))]
public class AnimationRandLezioneTag : RandomizerTag 
{
    [HideInInspector]
    public AnimatorOverrideController m_Controller;

    public CategoricalParameter<AnimationClip> animationClips;
    public BooleanParameter mirrorParameter;
    public FloatParameter speed;
    public bool applyRootMotion;

    private void Awake() {
        var animator = GetComponent<Animator>();
        RuntimeAnimatorController runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimatorControllers/MyHumanAnimatorController");
        m_Controller = new AnimatorOverrideController(runtimeAnimatorController);
        animator.runtimeAnimatorController = m_Controller;
    }

}

[Serializable]
[AddRandomizerMenu("Animation Randomizer Lezione")]
public class AnimationRandLezione : Randomizer
{
    private List<Animator> AnimatorList;

    protected override void OnIterationStart()
    {
        AnimatorList = new List<Animator>();
        var tags = tagManager.Query<AnimationRandLezioneTag>();
        foreach (var tag in tags)
            RandomizeAnimation(tag);
    }

    void RandomizeAnimation(AnimationRandLezioneTag tag)
    {
        if (!tag.gameObject.activeInHierarchy)
            return;

        var animator = tag.gameObject.GetComponent<Animator>();
        tag.m_Controller["PlayerIdle"] = tag.animationClips.Sample();
        
        animator.SetBool("Mirror", tag.mirrorParameter.Sample());
        animator.speed = tag.speed.Sample();
        animator.applyRootMotion = tag.applyRootMotion;

        AnimatorList.Add(animator);
    }

    public bool isAnimationComplete
    {
        get
        {
            bool animComplFlag = false;
            if(AnimatorList is not null)
            {
                animComplFlag = true;
                foreach(Animator animator in AnimatorList)
                {
                    animComplFlag = animComplFlag && (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
                }
            }
            return animComplFlag;
        }
    }
}
