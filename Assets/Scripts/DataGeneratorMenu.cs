using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class DataGeneratorMenu : MonoBehaviour
{
    [MenuItem("Data Generator/Load Animations")]
    static void LoadAnimations()
    {
        string path = EditorUtility.OpenFolderPanel("Select the directory with the animation txt files", Application.dataPath, "");
        SaveMultipleClips(path);
    }

    private static string clipName =  "null";
    private static HumanPoseHandler humanPoseHandler;
    private static HumanPose humanPose = new HumanPose();
    private static Dictionary<int, AnimationCurve> muscleCurves = new Dictionary<int, AnimationCurve>();
    private static Dictionary<string, AnimationCurve> rootCurves = new Dictionary<string, AnimationCurve>();
    static Vector3 rootOffset = new Vector3();
    private static List<jFrame> frames = new List<jFrame>();

    static List<HumanBodyBones> bonesList_ = new List<HumanBodyBones> {
        HumanBodyBones.Hips, // hips = 0
        HumanBodyBones.UpperChest, // upper chest = 54
        HumanBodyBones.Head, // head = 10
        HumanBodyBones.LeftShoulder, // left shoulder = 11
        HumanBodyBones.LeftUpperArm, // left upper arm = 13
        HumanBodyBones.LeftLowerArm, // left lower arm = 15
        HumanBodyBones.LeftHand, // left hand = 17
        HumanBodyBones.RightShoulder, // right shoulder = 12
        HumanBodyBones.RightUpperArm, // right upper arm = 14
        HumanBodyBones.RightLowerArm, // right lower arm = 16
        HumanBodyBones.RightHand, // right hand = 18 
        HumanBodyBones.LeftUpperLeg, // left upper leg = 1
        HumanBodyBones.LeftLowerLeg, // left lower leg = 3
        HumanBodyBones.LeftFoot, // left foot = 5 
        HumanBodyBones.RightUpperLeg, // right upper leg = 2
        HumanBodyBones.RightLowerLeg, // right lower leg = 4
        HumanBodyBones.RightFoot, // right foot = 6
        HumanBodyBones.Spine, // spine = 7
    };

    private static Dictionary<int, int> joint_bone = new Dictionary<int, int> {
        [0] = -2, // root
        [1] = (int)HumanBodyBones.Hips, // hips = 0
        [2] = (int)HumanBodyBones.UpperChest, // upper chest = 54
        [3] = (int)HumanBodyBones.Head, // head = 10
        [4] = (int)HumanBodyBones.LeftShoulder, // left shoulder = 11
        [5] = (int)HumanBodyBones.LeftUpperArm, // left upper arm = 13
        [6] = (int)HumanBodyBones.LeftLowerArm, // left lower arm = 15
        [7] = (int)HumanBodyBones.LeftHand, // left hand = 17
        [8] = (int)HumanBodyBones.RightShoulder, // right shoulder = 12
        [9] = (int)HumanBodyBones.RightUpperArm, // right upper arm = 14
        [10] = (int)HumanBodyBones.RightLowerArm, // right lower arm = 16
        [11] = (int)HumanBodyBones.RightHand, // right hand = 18
        [12] = -1, 
        [13] = (int)HumanBodyBones.LeftUpperLeg, // left upper leg = 1
        [14] = (int)HumanBodyBones.LeftLowerLeg, // left lower leg = 3
        [15] = (int)HumanBodyBones.LeftFoot, // left foot = 5 
        [16] = -1,
        [17] = (int)HumanBodyBones.RightUpperLeg, // right upper leg = 2
        [18] = (int)HumanBodyBones.RightLowerLeg, // right lower leg = 4
        [19] = (int)HumanBodyBones.RightFoot, // right foot = 6
        [20] = (int)HumanBodyBones.Spine, // spine = 7
        [21] = -1, 
        [22] = -1, 
        [23] = -1, 
        [24] = -1 
    };

    public static AnimationClip generateClip ()
    {
        AnimationClip clip = new AnimationClip();
        clip.name =  clipName;

        string modelName = "Ch37_nonPBR";
        GameObject loadedModel = GameObject.Instantiate(Resources.Load("characters/" + modelName) as GameObject);
        if(loadedModel.GetComponent <Animator> () == null)
            loadedModel.AddComponent<Animator> ();
        Animator anim = loadedModel.GetComponent <Animator> ();

        rootOffset = anim.transform.position;
        humanPoseHandler = new HumanPoseHandler(anim.avatar, anim.transform);

        initializeCurves(bonesList_);
        int feindex = 0;

        // avatar proportions
        Transform hips = anim.GetBoneTransform(UnityEngine.HumanBodyBones.Hips);
        Transform upperLeg = anim.GetBoneTransform(UnityEngine.HumanBodyBones.LeftUpperLeg);
        Transform lowerLeg = anim.GetBoneTransform(UnityEngine.HumanBodyBones.LeftLowerLeg);
        float avatarLeftLeg = hips.localPosition.magnitude;//  + upperLeg.localPosition.magnitude + lowerLeg.localPosition.magnitude;

        foreach (jFrame tempFrame in frames)
        {
            // Debug.Log("Timestamp: " + tempFrame.timeStamp);
            foreach (jBone tempBone in tempFrame.bones)
            {
                Transform t = anim.GetBoneTransform((UnityEngine.HumanBodyBones)tempBone.boneID);
                t.rotation = new Quaternion(tempBone.rotation[0], tempBone.rotation[1], tempBone.rotation[2], tempBone.rotation[3]);
                
            }


            float propCoef = avatarLeftLeg / tempFrame.legLength;
            anim.rootPosition = new Vector3(tempFrame.rootPosition[0], tempFrame.rootPosition[1], tempFrame.rootPosition[2]) * propCoef;
            anim.rootRotation = new Quaternion(tempFrame.rootRotation[0], tempFrame.rootRotation[1], tempFrame.rootRotation[2], tempFrame.rootRotation[3]);
        
            humanPoseHandler.GetHumanPose(ref humanPose);

            foreach (KeyValuePair<int, AnimationCurve> data in muscleCurves)
            {
                Keyframe key = new Keyframe(tempFrame.timeStamp, humanPose.muscles[data.Key]);
                data.Value.AddKey(key);
            }

            Vector3 rootPosition = anim.rootPosition - rootOffset;

            myAddKey("RootT.x", rootPosition.x, tempFrame.timeStamp);
            myAddKey("RootT.y", rootPosition.y, tempFrame.timeStamp);
            myAddKey("RootT.z", rootPosition.z, tempFrame.timeStamp);

            myAddKey("RootQ.x", anim.rootRotation.x, tempFrame.timeStamp);
            myAddKey("RootQ.y", anim.rootRotation.y, tempFrame.timeStamp);
            myAddKey("RootQ.z", anim.rootRotation.z, tempFrame.timeStamp);
            myAddKey("RootQ.w", anim.rootRotation.w, tempFrame.timeStamp);
            feindex++;
            //Debug.Log("feindex: " + feindex);
        }

        foreach (KeyValuePair<int, AnimationCurve> data in muscleCurves)
        {
            clip.SetCurve("", typeof(Animator), HumanTrait.MuscleName[data.Key], data.Value);
        }

        foreach (KeyValuePair<string, AnimationCurve> data in rootCurves)
        {
            clip.SetCurve("", typeof(Animator), data.Key, data.Value);
        }

        GameObject.DestroyImmediate(loadedModel);
        return clip;
    }

    public static void collectFramesMediapipe (string path)
    {
        Debug.Log(path);

        frames = new List<jFrame>();
        clipName = Path.GetFileName(path).Replace(".txt", "");
        Debug.Log(clipName);
        //clipName = Path.GetFileName(path).Substring(16, 4);
        int[] iArray;
        float[] fArray;
        List<float[]> jointPosConf = new List<float[]>();

        if(File.Exists(path))
        {
            using (StreamReader sr = File.OpenText(path))
            {
                string line;
                line = sr.ReadLine();
                iArray = System.Array.ConvertAll(line.Split(' '), int.Parse);
                int numFrames = iArray[0];
                Debug.Log("Num Frames: " + numFrames);
                int numJoints = iArray[1];
                Debug.Log("Num Joints: " + numJoints);
                int width = iArray[2];
                int height = iArray[3];
                float imageRatio = (float)width / (float)height;
                float dist3D = 1f;
                float dist2D = 1f;
                float[] feetPos = new float[]{0f, 0f, 0f};

                Vector3 upperleftleg = Vector3.zero;
                Vector3 lowerleftleg = Vector3.zero;
                Vector3 leftFoot = Vector3.zero;

                for (int i = 0; i < numFrames; i++)
                {
                    jFrame tempFrame = new jFrame();
                    tempFrame.modelName = Path.GetFileName(path);
                    tempFrame.rootPosition = new float[]{0f, 1f, 0f};
                    tempFrame.rootRotation = new float[]{0f, 0f, 0f, 0f};
                    tempFrame.bones = new List<jBone>();
                    tempFrame.timeStamp = 1f / 30f * (float)i;
                
                    jointPosConf = new List<float[]>();
                    line = sr.ReadLine(); //empty line
                    for (int j = 0; j < numJoints; j++)
                    {
                        //Debug.Log("joint: " + j);
                        line = sr.ReadLine();
                        fArray = System.Array.ConvertAll(line.Split(' '), float.Parse);
                        // jointPosConf.Add(new float[]{-fArray[1], fArray[2], fArray[0], fArray[3]});
                        jointPosConf.Add(new float[]{-fArray[0], -fArray[1], -fArray[2], fArray[3], fArray[4], fArray[5], fArray[6]});
                    }

                    // Proportional coefficient 2D vs 3D 
                    if (i == 0)
                    {
                        Vector3 vectorA3D = new Vector3((jointPosConf[29][0] + jointPosConf[30][0]) / 2f, 
                                                        (jointPosConf[29][1] + jointPosConf[30][1]) / 2f, 
                                                        (jointPosConf[29][2] + jointPosConf[30][2]) / 2f);
                        Vector3 vectorB3D = new Vector3((jointPosConf[23][0] + jointPosConf[24][0]) / 2f, 
                                                        (jointPosConf[23][1] + jointPosConf[24][1]) / 2f, 
                                                        (jointPosConf[23][2] + jointPosConf[24][2]) / 2f);
                        dist3D  = Mathf.Abs(vectorB3D.y - vectorA3D.y);

                        Vector3 vectorA2D = new Vector3((jointPosConf[29][4] + jointPosConf[30][4]) / 2f, 
                                                        (jointPosConf[29][5] + jointPosConf[30][5]) / 2f, 
                                                        (jointPosConf[29][6] + jointPosConf[30][6]) / 2f);
                        Vector3 vectorB2D = new Vector3((jointPosConf[23][4] + jointPosConf[24][4]) / 2f, 
                                                        (jointPosConf[23][5] + jointPosConf[24][5]) / 2f, 
                                                        (jointPosConf[23][6] + jointPosConf[24][6]) / 2f);
                        dist2D  = Mathf.Abs(vectorB2D.y - vectorA2D.y);

                        feetPos = new float[]{vectorA2D.x, vectorA2D.y, vectorA2D.z};
                    }
                    
                    tempFrame.rootPosition[0] = -(((jointPosConf[23][4] + jointPosConf[24][4]) / 2f) - feetPos[0]) * dist3D / dist2D * imageRatio;
                    tempFrame.rootPosition[1] = -(((jointPosConf[23][5] + jointPosConf[24][5]) / 2f) - feetPos[1]) * dist3D / dist2D;
                    tempFrame.rootPosition[2] = -(((jointPosConf[23][6] + jointPosConf[24][6]) / 2f) - feetPos[2]) * dist3D / dist2D * imageRatio;

                    // Avoiding clipping the floor
                    
                    // float minPosHeight = 0f;
                    // foreach(float[] joint in jointPosConf)
                    // {
                    //     if(joint[1] + tempFrame.rootPosition[1] < minPosHeight)
                    //         minPosHeight = joint[1] + tempFrame.rootPosition[1];
                    // }
                    // tempFrame.rootPosition[1] -= minPosHeight;

                    // leg length
                    if (i == 0)
                    {
                        upperleftleg = new Vector3(jointPosConf[25][0], jointPosConf[25][1], jointPosConf[25][2]) - 
                                       new Vector3(jointPosConf[23][0], jointPosConf[23][1], jointPosConf[23][2]);
                        lowerleftleg = new Vector3(jointPosConf[27][0], jointPosConf[27][1], jointPosConf[27][2]) - 
                                       new Vector3(jointPosConf[25][0], jointPosConf[25][1], jointPosConf[25][2]);
                        leftFoot = new Vector3(jointPosConf[29][0], jointPosConf[29][1], jointPosConf[29][2]) - 
                                   new Vector3(jointPosConf[27][0], jointPosConf[27][1], jointPosConf[27][2]);
                    }
                    tempFrame.legLength = upperleftleg.magnitude + lowerleftleg.magnitude +  leftFoot.magnitude;
                    // Debug.Log("upperleftleg: " + upperleftleg + " - lowerleftleg: " + upperleftleg + " - Animation leg length: " + tempFrame.legLength);
                    // Debug.Log("3D hip height: " + dist3D + " - 2D hip height: " + dist2D +  " - proportional hip height: " + tempFrame.rootPosition[1]);

                    //Debug.Log("Frame: " + i);
                    int[] temp_bones = {1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 17, 18, 19, 20};
                    foreach (int j in temp_bones)
                    {
                        if (bonesList_.Exists(x => x == (HumanBodyBones)joint_bone[j]))
                        {
                            // modificare l'abbinamento joint/pair e confidence
                            jBone tempBone = new jBone();
                            tempBone.boneID = joint_bone[j];
                            tempBone.name = "mediapipe_bone";
                            Vector3 newRotDir = rotDirVector(j, jointPosConf);
                            Vector3 newRotOrt = rotOrtVector(j, jointPosConf);
                            Quaternion newRotation = Quaternion.LookRotation(newRotDir, newRotOrt) * Quaternion.AngleAxis(90, Vector3.forward) * Quaternion.AngleAxis(90, Vector3.right);
                            tempBone.rotation = new float[]{newRotation[0], newRotation[1], newRotation[2], newRotation[3]};
                            tempBone.position = new float[]{0f, 0f, 0f};
                            tempFrame.bones.Add(tempBone);
                            if (j == 1)
                            {
                                tempFrame.rootRotation = new float[]{newRotation[0], newRotation[1], newRotation[2], newRotation[3]};
                            }
                        }
                    }
                    frames.Add(tempFrame);
                }
            }
        }
        else
        {
            Debug.LogError("Frames file does not exist!");
        }
    }

    private static void myAddKey(string property, float value, float time)
    {
        Keyframe key = new Keyframe(time, value);
        rootCurves[property].AddKey(key);
    }

    private static void initializeCurves(List<HumanBodyBones> bonesList)
    {
        muscleCurves = new Dictionary<int, AnimationCurve>();
        rootCurves = new Dictionary<string, AnimationCurve>();

        foreach (HumanBodyBones tempBoneID in bonesList)
        {
            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
            {
                int muscle = HumanTrait.MuscleFromBone((int)tempBoneID, dofIndex);

                if (muscle != -1)
                    muscleCurves.Add(muscle, new AnimationCurve());
            }
        }

        rootCurves.Add("RootT.x", new AnimationCurve());
        rootCurves.Add("RootT.y", new AnimationCurve());
        rootCurves.Add("RootT.z", new AnimationCurve());

        rootCurves.Add("RootQ.x", new AnimationCurve());
        rootCurves.Add("RootQ.y", new AnimationCurve());
        rootCurves.Add("RootQ.z", new AnimationCurve());
        rootCurves.Add("RootQ.w", new AnimationCurve());
    }

    private static void SaveMultipleClips(string path)
    {
        int count = 0;
        Debug.Log("Loading mediapipe");
        foreach (string file in Directory.EnumerateFiles(path))
        {
            Debug.Log(count++);
            string tempFile = file.Replace("\\", "/");
            Debug.Log(Path.GetFileName(tempFile));
            collectFramesMediapipe (tempFile);
            AnimationClip tempClip = generateClip();
            
            string fileName = tempClip.name;
            AssetDatabase.CreateAsset(tempClip, "Assets/Animations/" + fileName + ".anim");
        }
    }
    
    private static Vector3 rotOrtVector(int index, List<float[]> jointPosConf) {
        Vector3 result = new Vector3(0f, 0f, 0f);
        // head
        if(index == 3)
        {
            Vector3 vectorA = new Vector3(jointPosConf[7][0], jointPosConf[7][1], jointPosConf[7][2]);
            Vector3 vectorB = new Vector3(jointPosConf[8][0], jointPosConf[8][1], jointPosConf[8][2]);
            result = vectorB - vectorA;

        }
        // hips
        if(index == 1)
        {
            Vector3 vectorA = new Vector3(jointPosConf[23][0], jointPosConf[23][1], jointPosConf[23][2]);
            Vector3 vectorB = new Vector3(jointPosConf[24][0], jointPosConf[24][1], jointPosConf[24][2]);
            result = vectorB - vectorA;
        }
        // upper chest, spine
        if(index == 2 || index == 20)
        {
            Vector3 vectorA = new Vector3(jointPosConf[11][0], jointPosConf[11][1], jointPosConf[11][2]);
            Vector3 vectorB = new Vector3(jointPosConf[12][0], jointPosConf[12][1], jointPosConf[12][2]);
            result = vectorB - vectorA;
        }
        // left shoulder, upper arm, lower arm, hand
        if(index == 4)
        {
            Vector3 vectorA = new Vector3(jointPosConf[13][0], jointPosConf[13][1], jointPosConf[13][2]);
            Vector3 vectorB = new Vector3(jointPosConf[11][0], jointPosConf[11][1], jointPosConf[11][2]);
            result = vectorB - vectorA;
        }
        if(index == 5)
        {
            Vector3 vectorA = new Vector3(jointPosConf[13][0], jointPosConf[13][1], jointPosConf[13][2]);
            Vector3 vectorB = new Vector3(jointPosConf[11][0], jointPosConf[11][1], jointPosConf[11][2]);
            Vector3 vectorC = new Vector3(jointPosConf[15][0], jointPosConf[15][1], jointPosConf[15][2]);
            result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);
            // Vector3 vectorA = new Vector3(jointPosConf[15][0], jointPosConf[15][1], jointPosConf[15][2]);
            // Vector3 vectorB = new Vector3(jointPosConf[13][0], jointPosConf[13][1], jointPosConf[13][2]);
            result = vectorB - vectorA;
        }
        if(index == 6)
        {
            Vector3 vectorA = new Vector3(jointPosConf[17][0], jointPosConf[17][1], jointPosConf[17][2]);
            Vector3 vectorB = new Vector3(jointPosConf[19][0], jointPosConf[19][1], jointPosConf[19][2]);
            result = vectorB - vectorA;
        }
        if(index == 7)
        {
            Vector3 vectorA = new Vector3(jointPosConf[17][0], jointPosConf[17][1], jointPosConf[17][2]);
            Vector3 vectorB = new Vector3(jointPosConf[19][0], jointPosConf[19][1], jointPosConf[19][2]);
            result = vectorB - vectorA;
        }
        // right shoulder, upper arm, lower arm, hand
        if(index == 8)
        {
            Vector3 vectorA = new Vector3(jointPosConf[12][0], jointPosConf[12][1], jointPosConf[12][2]);
            Vector3 vectorB = new Vector3(jointPosConf[14][0], jointPosConf[14][1], jointPosConf[14][2]);
            result = vectorB - vectorA;
        }
        if(index == 9)
        {
            Vector3 vectorA = new Vector3(jointPosConf[14][0], jointPosConf[14][1], jointPosConf[14][2]);
            Vector3 vectorB = new Vector3(jointPosConf[12][0], jointPosConf[12][1], jointPosConf[12][2]);
            Vector3 vectorC = new Vector3(jointPosConf[16][0], jointPosConf[16][1], jointPosConf[16][2]);
            result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);
        }
        if(index == 10)
        {
            Vector3 vectorA = new Vector3(jointPosConf[20][0], jointPosConf[20][1], jointPosConf[20][2]);
            Vector3 vectorB = new Vector3(jointPosConf[18][0], jointPosConf[18][1], jointPosConf[18][2]);
            result = vectorB - vectorA;
        }
        if(index == 11)
        {
            Vector3 vectorA = new Vector3(jointPosConf[20][0], jointPosConf[20][1], jointPosConf[20][2]);
            Vector3 vectorB = new Vector3(jointPosConf[18][0], jointPosConf[18][1], jointPosConf[18][2]);
            result = vectorB - vectorA;
        }
        // left hip, upper leg, lower leg, foot
        if(index == 12)
        {
            Vector3 vectorA = new Vector3(jointPosConf[25][0], jointPosConf[25][1], jointPosConf[25][2]);
            Vector3 vectorB = new Vector3(jointPosConf[23][0], jointPosConf[23][1], jointPosConf[23][2]);
            result = vectorB - vectorA;
        }
        if(index == 13)
        {
            Vector3 vectorA = new Vector3(jointPosConf[25][0], jointPosConf[25][1], jointPosConf[25][2]);
            Vector3 vectorB = new Vector3(jointPosConf[27][0], jointPosConf[27][1], jointPosConf[27][2]);
            Vector3 vectorC = new Vector3(jointPosConf[23][0], jointPosConf[23][1], jointPosConf[23][2]);
            result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);
        }
        if(index == 14)
        {
            Vector3 vectorA = new Vector3(jointPosConf[29][0], jointPosConf[29][1], jointPosConf[29][2]);
            Vector3 vectorB = new Vector3(jointPosConf[31][0], jointPosConf[31][1], jointPosConf[31][2]);
            Vector3 vectorC = new Vector3(jointPosConf[27][0], jointPosConf[27][1], jointPosConf[27][2]);
            Vector3 vectorD = new Vector3(jointPosConf[25][0], jointPosConf[25][1], jointPosConf[25][2]);
            result = Vector3.Cross(vectorB - vectorA, vectorD - vectorC);
        }
        if(index == 15)
        {
            Vector3 vectorA = new Vector3(jointPosConf[29][0], jointPosConf[29][1], jointPosConf[29][2]);
            Vector3 vectorB = new Vector3(jointPosConf[31][0], jointPosConf[31][1], jointPosConf[31][2]);
            Vector3 vectorC = new Vector3(jointPosConf[25][0], jointPosConf[25][1], jointPosConf[25][2]);
            //result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);
            result = Vector3.Cross(vectorB - vectorA, vectorC - vectorA);
        }
        // right hip, upper leg, lower leg, foot
        if(index == 16)
        {
            Vector3 vectorA = new Vector3(jointPosConf[24][0], jointPosConf[24][1], jointPosConf[24][2]);
            Vector3 vectorB = new Vector3(jointPosConf[26][0], jointPosConf[26][1], jointPosConf[26][2]);
            result = vectorB - vectorA;
        }
        if(index == 17)
        {
            Vector3 vectorA = new Vector3(jointPosConf[26][0], jointPosConf[26][1], jointPosConf[26][2]);
            Vector3 vectorB = new Vector3(jointPosConf[28][0], jointPosConf[28][1], jointPosConf[28][2]);
            Vector3 vectorC = new Vector3(jointPosConf[24][0], jointPosConf[24][1], jointPosConf[24][2]);
            result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);
        }
        if(index == 18)
        {
            Vector3 vectorA = new Vector3(jointPosConf[30][0], jointPosConf[30][1], jointPosConf[30][2]);
            Vector3 vectorB = new Vector3(jointPosConf[32][0], jointPosConf[32][1], jointPosConf[32][2]);
            Vector3 vectorC = new Vector3(jointPosConf[28][0], jointPosConf[28][1], jointPosConf[28][2]);
            Vector3 vectorD = new Vector3(jointPosConf[26][0], jointPosConf[26][1], jointPosConf[26][2]);
            result = Vector3.Cross(vectorB - vectorA, vectorD - vectorC);
        }
        if(index == 19)
        {
            Vector3 vectorA = new Vector3(jointPosConf[30][0], jointPosConf[30][1], jointPosConf[30][2]);
            Vector3 vectorB = new Vector3(jointPosConf[32][0], jointPosConf[32][1], jointPosConf[32][2]);
            Vector3 vectorC = new Vector3(jointPosConf[26][0], jointPosConf[26][1], jointPosConf[26][2]);
            //result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);
            result = Vector3.Cross(vectorB - vectorA, vectorC - vectorA);
        }
        return result;
    }

    private static Vector3 rotDirVector(int index, List<float[]> jointPosConf) {
        Vector3 result = new Vector3(0f, 0f, 0f);

        // head
        if(index == 3)
        {
            int jointA = 0;
            int jointB = 7;
            int jointC = 8;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            Vector3 vectorC = new Vector3(jointPosConf[jointC][0], jointPosConf[jointC][1], jointPosConf[jointC][2]);
            result = Vector3.Cross(vectorC - vectorA, vectorB - vectorA);

            // int jointA = 12;
            // int jointB = 8;
            // int jointC = 11;
            // int jointD = 7;
            // Vector3 vectorA = new Vector3((jointPosConf[jointA][0] + jointPosConf[jointC][0]) / 2f, 
            //                 (jointPosConf[jointA][1] + jointPosConf[jointC][1]) / 2f, 
            //                 (jointPosConf[jointA][2] + jointPosConf[jointC][2]) / 2f);
            // Vector3 vectorB = new Vector3((jointPosConf[jointB][0] + jointPosConf[jointD][0]) / 2f, 
            //                 (jointPosConf[jointB][1] + jointPosConf[jointD][1]) / 2f, 
            //                 (jointPosConf[jointB][2] + jointPosConf[jointD][2]) / 2f);
            // result = vectorB - vectorA;

        }
        // hips, upper chest, spine
        if(index == 1 || index == 2 || index == 20)
        {
            int jointA = 24;
            int jointB = 12;
            int jointC = 23;
            int jointD = 11;
            Vector3 vectorA = new Vector3((jointPosConf[jointA][0] + jointPosConf[jointC][0]) / 2f, 
                            (jointPosConf[jointA][1] + jointPosConf[jointC][1]) / 2f, 
                            (jointPosConf[jointA][2] + jointPosConf[jointC][2]) / 2f);
            Vector3 vectorB = new Vector3((jointPosConf[jointB][0] + jointPosConf[jointD][0]) / 2f, 
                            (jointPosConf[jointB][1] + jointPosConf[jointD][1]) / 2f, 
                            (jointPosConf[jointB][2] + jointPosConf[jointD][2]) / 2f);
            result = vectorB - vectorA;
        }
        // left shoulder, upper arm, lower arm, hand
        if(index == 4)
        {
            int jointA = 12;
            int jointB = 11;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 5)
        {
            int jointA = 11;
            int jointB = 13;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 6)
        {
            int jointA = 13;
            int jointB = 15;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 7)
        {
            int jointA = 15;
            int jointB = 17;
            int jointC = 15;
            int jointD = 19;
            Vector3 vectorA = new Vector3((jointPosConf[jointA][0] + jointPosConf[jointC][0]) / 2f, 
                            (jointPosConf[jointA][1] + jointPosConf[jointC][1]) / 2f, 
                            (jointPosConf[jointA][2] + jointPosConf[jointC][2]) / 2f);
            Vector3 vectorB = new Vector3((jointPosConf[jointB][0] + jointPosConf[jointD][0]) / 2f, 
                            (jointPosConf[jointB][1] + jointPosConf[jointD][1]) / 2f, 
                            (jointPosConf[jointB][2] + jointPosConf[jointD][2]) / 2f);
            result = vectorB - vectorA;
        }
        // right shoulder, upper arm, lower arm, hand
        if(index == 8)
        {
            int jointA = 11;
            int jointB = 12;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 9)
        {
            int jointA = 12;
            int jointB = 14;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 10)
        {
            int jointA = 14;
            int jointB = 16;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 11)
        {
            int jointA = 16;
            int jointB = 18;
            int jointC = 16;
            int jointD = 20;
            Vector3 vectorA = new Vector3((jointPosConf[jointA][0] + jointPosConf[jointC][0]) / 2f, 
                            (jointPosConf[jointA][1] + jointPosConf[jointC][1]) / 2f, 
                            (jointPosConf[jointA][2] + jointPosConf[jointC][2]) / 2f);
            Vector3 vectorB = new Vector3((jointPosConf[jointB][0] + jointPosConf[jointD][0]) / 2f, 
                            (jointPosConf[jointB][1] + jointPosConf[jointD][1]) / 2f, 
                            (jointPosConf[jointB][2] + jointPosConf[jointD][2]) / 2f);
            result = vectorB - vectorA;
        }
        // left hip, upper leg, lower leg, foot
        if(index == 12)
        {
            int jointA = 24;
            int jointB = 23;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 13)
        {
            int jointA = 23;
            int jointB = 25;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 14)
        {
            int jointA = 25;
            int jointB = 27;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 15)
        {
            int jointA = 29;
            int jointB = 31;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        // right hip, upper leg, lower leg, foot
        if(index == 16)
        {
            int jointA = 23;
            int jointB = 24;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 17)
        {
            int jointA = 24;
            int jointB = 26;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 18)
        {
            int jointA = 26;
            int jointB = 28;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        if(index == 19)
        {
            int jointA = 30;
            int jointB = 32;
            Vector3 vectorA = new Vector3(jointPosConf[jointA][0], jointPosConf[jointA][1], jointPosConf[jointA][2]);
            Vector3 vectorB = new Vector3(jointPosConf[jointB][0], jointPosConf[jointB][1], jointPosConf[jointB][2]);
            result = vectorB - vectorA;
        }
        return result;
    }
}

public class jBone
{
    public int boneID {get; set;}
    public string name {get; set;}
    public float[] position {get; set;}
    public float[] rotation {get; set;}
    public float[] eulerAngles {get; set;}
    public float[] muscle {get; set;}
}
public class jFrame
{
    public string modelName;
    public float timeStamp;
    public float legLength;
    public float[] rootPosition;
    public float[] rootRotation;
    public List<jBone> bones;
}
