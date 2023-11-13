using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    [RequireComponent(typeof(Animator))]
    public class LabelingAnimationsTag: LabeledMetadataTag 
    {
        Animator tempAnimator;
        AnimatorClipInfo[] m_CurrentClipInfo;

        protected override string key => "animation_info";

        protected override void GetReportedValues(IMessageBuilder builder)
        {
            tempAnimator = gameObject.GetComponent<Animator>();
            m_CurrentClipInfo = tempAnimator.GetCurrentAnimatorClipInfo(0);

            string[] words = m_CurrentClipInfo[0].clip.name.Split('-');
            string clipName = words[0];
            
            builder.AddFloat("animation_length", m_CurrentClipInfo[0].clip.length);
            builder.AddString("animation_name", clipName);
            builder.AddString("clip_name", m_CurrentClipInfo[0].clip.name);
        }
    }
}