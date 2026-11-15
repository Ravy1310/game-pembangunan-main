#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CozyGlade.EditorTools
{
    /// <summary>Builds the Floreswa villagers: humanoid avatars, an animator controller using Kevin Iglesias clips, prefabs, and scene placement.</summary>
    public static class NpcVillageSetup
    {
        const string CharacterFolder = "Assets/_Project/Art/Characters/NPC";
        const string AnimationFolder = "Assets/_Project/Art/Animations/NPC";
        const string ClipFolder = AnimationFolder + "/Clips";
        const string PrefabFolder = "Assets/_Project/Prefabs/NPC";
        const string ControllerPath = AnimationFolder + "/NPC_Villager.controller";

        const string Idle01Source = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx";
        const string Idle02Source = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle02.fbx";
        const string WalkSource = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx";
        const string TalkSource = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Social/Conversation/HumanM@Talk01.fbx";

        static readonly (string model, string prefabName, string instanceName, Vector3 position)[] Villagers =
        {
            ("Assets/Floreswa/Prefabs/male01_1.prefab", "NPC_Villager_Male01", "Villager_Male01", new Vector3(-1.5f, 0f, -11.5f)),
            ("Assets/Floreswa/Prefabs/male02_2.prefab", "NPC_Villager_Male02", "Villager_Male02", new Vector3(2.5f, 0f, -12.5f)),
            ("Assets/Floreswa/Prefabs/male03_3.prefab", "NPC_Villager_Male03", "Villager_Male03", new Vector3(-6f, 0f, -14f)),
        };

        [MenuItem("Tools/Cozy Glade/Setup Villagers (Floreswa + Kevin Animations)")]
        public static void Setup()
        {
            EnsureFolder("Assets/_Project/Art/Characters");
            EnsureFolder(CharacterFolder);
            EnsureFolder("Assets/_Project/Art/Animations");
            EnsureFolder(AnimationFolder);
            EnsureFolder(ClipFolder);
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PrefabFolder);

            AnimationClip idle01 = CopyClip(Idle01Source, "NPC_Idle01", ClipFolder + "/NPC_Idle01.anim", true);
            AnimationClip idle02 = CopyClip(Idle02Source, "NPC_Idle02", ClipFolder + "/NPC_Idle02.anim", true);
            AnimationClip walk = CopyClip(WalkSource, "NPC_Walk01", ClipFolder + "/NPC_Walk01.anim", true);
            AnimationClip talk = CopyClip(TalkSource, "NPC_Talk01", ClipFolder + "/NPC_Talk01.anim", true);

            AnimatorController controller = BuildController(ControllerPath, idle01, idle02, walk, talk);

            var prefabs = new List<GameObject>();
            for (int i = 0; i < Villagers.Length; i++)
            {
                var entry = Villagers[i];
                string avatarPath = CharacterFolder + "/" + entry.prefabName + "_Avatar.asset";
                Avatar avatar = BuildAvatar(entry.model, avatarPath);

                string prefabPath = PrefabFolder + "/" + entry.prefabName + ".prefab";
                prefabs.Add(BuildPrefab(entry.model, avatar, controller, prefabPath));
            }

            PlaceInScene(prefabs);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Debug.Log("[NPC] Villagers ready: " + string.Join(", ", Villagers.Select(v => v.instanceName)));
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static AnimationClip CopyClip(string sourcePath, string clipName, string destination, bool loop)
        {
            AnimationClip source = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                .OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview__"));

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destination) != null)
                AssetDatabase.DeleteAsset(destination);

            AnimationClip copy = Object.Instantiate(source);
            copy.name = clipName;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(copy, settings);

            AssetDatabase.CreateAsset(copy, destination);
            return copy;
        }

        static AnimatorController BuildController(string path, AnimationClip idle01, AnimationClip idle02, AnimationClip walk, AnimationClip talk)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IdleVariant", AnimatorControllerParameterType.Int);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState stateIdle1 = machine.AddState("Idle01", new Vector3(280f, 0f));
            stateIdle1.motion = idle01;
            AnimatorState stateIdle2 = machine.AddState("Idle02", new Vector3(280f, 110f));
            stateIdle2.motion = idle02;
            AnimatorState stateWalk = machine.AddState("Walk", new Vector3(550f, 55f));
            stateWalk.motion = walk;
            stateWalk.speed = 1.1f;
            AnimatorState stateTalk = machine.AddState("Talk", new Vector3(550f, 190f));
            stateTalk.motion = talk;

            machine.defaultState = stateIdle1;

            Link(stateIdle1, stateWalk, 0.2f, false, Cond(AnimatorConditionMode.Greater, 0.1f, "Speed"));
            Link(stateIdle2, stateWalk, 0.2f, false, Cond(AnimatorConditionMode.Greater, 0.1f, "Speed"));

            Link(stateWalk, stateIdle1, 0.2f, false,
                Cond(AnimatorConditionMode.Less, 0.1f, "Speed"),
                Cond(AnimatorConditionMode.Equals, 0f, "IdleVariant"));
            Link(stateWalk, stateIdle2, 0.2f, false,
                Cond(AnimatorConditionMode.Less, 0.1f, "Speed"),
                Cond(AnimatorConditionMode.Equals, 1f, "IdleVariant"));

            Link(stateIdle1, stateIdle2, 0.35f, true, Cond(AnimatorConditionMode.Equals, 1f, "IdleVariant"));
            Link(stateIdle2, stateIdle1, 0.35f, true, Cond(AnimatorConditionMode.Equals, 0f, "IdleVariant"));

            Link(stateIdle1, stateTalk, 0.2f, false, Cond(AnimatorConditionMode.If, 1f, "Talking"));
            Link(stateIdle2, stateTalk, 0.2f, false, Cond(AnimatorConditionMode.If, 1f, "Talking"));
            Link(stateWalk, stateTalk, 0.2f, false, Cond(AnimatorConditionMode.If, 1f, "Talking"));

            Link(stateTalk, stateIdle1, 0.2f, false,
                Cond(AnimatorConditionMode.IfNot, 0f, "Talking"),
                Cond(AnimatorConditionMode.Equals, 0f, "IdleVariant"));
            Link(stateTalk, stateIdle2, 0.2f, false,
                Cond(AnimatorConditionMode.IfNot, 0f, "Talking"),
                Cond(AnimatorConditionMode.Equals, 1f, "IdleVariant"));
            Link(stateTalk, stateWalk, 0.2f, false, Cond(AnimatorConditionMode.Greater, 0.1f, "Speed"));

            return controller;
        }

        static AnimatorCondition Cond(AnimatorConditionMode mode, float threshold, string parameter) =>
            new AnimatorCondition { mode = mode, threshold = threshold, parameter = parameter };

        static void Link(AnimatorState from, AnimatorState to, float duration, bool exitTime, params AnimatorCondition[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = exitTime;
            transition.exitTime = exitTime ? 0.9f : 0f;
            transition.duration = duration;
            transition.hasFixedDuration = true;
            foreach (AnimatorCondition condition in conditions)
                transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        static Avatar BuildAvatar(string modelPath, string avatarPath)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject instance = Object.Instantiate(model, Vector3.zero, Quaternion.identity);
            instance.name = "AvatarBuildTemp";

            Transform metarig = instance.transform.Find("metarig");
            if (metarig == null)
            {
                Object.DestroyImmediate(instance);
                throw new System.InvalidOperationException("[NPC] No 'metarig' found in " + modelPath);
            }

            HumanBone[] human = BuildHumanBones();
            Transform[] bones = metarig.GetComponentsInChildren<Transform>(true);

            HumanDescription description = new HumanDescription
            {
                human = human,
                skeleton = bones.Select(t => new SkeletonBone
                {
                    name = t.name,
                    position = t.localPosition,
                    rotation = t.localRotation,
                    scale = t.localScale
                }).ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };

            Avatar avatar = AvatarBuilder.BuildHumanAvatar(instance, description);
            avatar.name = Path.GetFileNameWithoutExtension(avatarPath);

            if (!avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError("[NPC] Avatar for " + modelPath + " is invalid. Unity may be missing bone mappings.");
            }

            if (AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath) != null)
                AssetDatabase.DeleteAsset(avatarPath);

            AssetDatabase.CreateAsset(avatar, avatarPath);
            Object.DestroyImmediate(instance);
            return avatar;
        }

        static HumanBone[] BuildHumanBones()
        {
            return new[]
            {
                Bone("Hips", "spine"),
                Bone("Spine", "spine.001"),
                Bone("Chest", "spine.002"),
                Bone("UpperChest", "spine.003"),
                Bone("Neck", "spine.004"),
                Bone("Head", "spine.005"),
                Bone("LeftShoulder", "shoulder.L"),
                Bone("LeftUpperArm", "upper_arm.L"),
                Bone("LeftLowerArm", "forearm.L"),
                Bone("LeftHand", "hand.L"),
                Bone("RightShoulder", "shoulder.R"),
                Bone("RightUpperArm", "upper_arm.R"),
                Bone("RightLowerArm", "forearm.R"),
                Bone("RightHand", "hand.R"),
                Bone("LeftUpperLeg", "thigh.L"),
                Bone("LeftLowerLeg", "shin.L"),
                Bone("LeftFoot", "foot.L"),
                Bone("LeftToes", "toe.L"),
                Bone("RightUpperLeg", "thigh.R"),
                Bone("RightLowerLeg", "shin.R"),
                Bone("RightFoot", "foot.R"),
                Bone("RightToes", "toe.R"),
            };
        }

        static HumanBone Bone(string humanName, string boneName) => new HumanBone
        {
            humanName = humanName,
            boneName = boneName,
            limit = new HumanLimit { useDefaultValues = true }
        };

        static GameObject BuildPrefab(string modelPath, Avatar avatar, AnimatorController controller, string prefabPath)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            instance.name = Path.GetFileNameWithoutExtension(prefabPath);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 0.8f;

            Animator animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Npc.NpcActor actor = instance.AddComponent<Npc.NpcActor>();
            actor.walkSpeed = 1.15f;
            actor.wanderRadius = 3.5f;
            actor.socialRadius = 12f;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return saved;
        }

        static void PlaceInScene(List<GameObject> prefabs)
        {
            GameObject existing = GameObject.Find("NPCs");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject root = new GameObject("NPCs");

            for (int i = 0; i < prefabs.Count; i++)
            {
                var entry = Villagers[i];
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                instance.name = entry.instanceName;
                instance.transform.SetParent(root.transform);
                instance.transform.position = FindGround(entry.position);
                instance.transform.rotation = Quaternion.LookRotation(new Vector3(2f, 0f, 1f) - entry.position);
            }
        }

        static Vector3 FindGround(Vector3 point)
        {
            Vector3 origin = point + Vector3.up * 5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f)) return hit.point;
            return point;
        }
    }
}
#endif
