#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public class SkeletonBossGenerator
{
    private const string ANIM_DIR = "Assets/Animations/Monster/Map5/Skeleton_Boss";
    private const string PREFAB_DIR = "Assets/Prefabs/Monster/Map5";
    private const string ART_DIR = "Assets/Art/Monster/Map5/Skeleton_Boss";

    static SkeletonBossGenerator()
    {
        EditorApplication.delayCall += CheckAndGenerateBoss;
    }

    [MenuItem("Tools/Map 5/Generate Skeleton Boss Prefab & Animations")]
    public static void ManualGenerate()
    {
        GenerateBoss();
    }

    private static void CheckAndGenerateBoss()
    {
        // Tự động kiểm tra nếu chưa có prefab thì sinh ra 1 lần
        string prefabPath = $"{PREFAB_DIR}/Skeleton_Boss.prefab";
        if (File.Exists(prefabPath)) return;

        GenerateBoss();
    }

    public static void GenerateBoss()
    {
        if (!Directory.Exists(ANIM_DIR))
            Directory.CreateDirectory(ANIM_DIR);

        if (!Directory.Exists(PREFAB_DIR))
            Directory.CreateDirectory(PREFAB_DIR);

        Debug.Log("👑 [SkeletonBossGenerator] Đang tiến hành tạo Animations và Controller cho Skeleton Boss...");

        // 1. Tạo các AnimationClips
        var idleClip = CreateClip("Skeleton_Boss_Idle.png", "Skeleton_boss_idle.anim", 10f, true);
        var walkClip = CreateClip("Skeleton_Boss_Walk.png", "Skeleton_boss_walk.anim", 12f, true);
        var attack1Clip = CreateClip("Skeleton_Boss_Attack1.png", "Skeleton_boss_attack1.anim", 12f, false);
        var attack2Clip = CreateClip("Skeleton_Boss_Attack2.png", "Skeleton_boss_attack2.anim", 12f, false);
        var attack3Clip = CreateClip("Skeleton_Boss_Attack3.png", "Skeleton_boss_attack3.anim", 10f, false);
        var summonClip = CreateClip("Skeleton_Boss_Summon.png", "Skeleton_boss_summon.anim", 10f, false);
        var hurtClip = CreateClip("Skeleton_Boss_Hurt.png", "Skeleton_boss_hurt.anim", 12f, false);
        var dieClip = CreateClip("Skeleton_Boss_Die.png", "Skeleton_boss_die.anim", 10f, false);

        // 2. Tạo AnimatorController
        string controllerPath = $"{ANIM_DIR}/Skeleton_Boss.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Summon", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        var walkState = sm.AddState("Walk");
        walkState.motion = walkClip;

        var attack1State = sm.AddState("Attack1");
        attack1State.motion = attack1Clip;

        var attack2State = sm.AddState("Attack2");
        attack2State.motion = attack2Clip;

        var attack3State = sm.AddState("Attack3");
        attack3State.motion = attack3Clip;

        var summonState = sm.AddState("Summon");
        summonState.motion = summonClip;

        var hurtState = sm.AddState("Hurt");
        hurtState.motion = hurtClip;

        var dieState = sm.AddState("Die");
        dieState.motion = dieClip;

        // Transitions:
        // Idle <-> Walk
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;

        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;

        // Combo Attack1 -> Attack2 -> Idle
        var toAtk1FromIdle = idleState.AddTransition(attack1State);
        toAtk1FromIdle.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAtk1FromIdle.hasExitTime = false;

        var toAtk1FromWalk = walkState.AddTransition(attack1State);
        toAtk1FromWalk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAtk1FromWalk.hasExitTime = false;

        var atk1ToAtk2 = attack1State.AddTransition(attack2State);
        atk1ToAtk2.hasExitTime = true;
        atk1ToAtk2.exitTime = 0.85f;
        atk1ToAtk2.duration = 0.05f;

        var atk2ToIdle = attack2State.AddTransition(idleState);
        atk2ToIdle.hasExitTime = true;
        atk2ToIdle.exitTime = 0.9f;
        atk2ToIdle.duration = 0.05f;

        // Attack 3 (Heavy Smash)
        var toAtk3 = idleState.AddTransition(attack3State);
        toAtk3.AddCondition(AnimatorConditionMode.If, 0, "Attack3");
        toAtk3.hasExitTime = false;

        var atk3ToIdle = attack3State.AddTransition(idleState);
        atk3ToIdle.hasExitTime = true;
        atk3ToIdle.exitTime = 0.95f;
        atk3ToIdle.duration = 0.05f;

        // Summon
        var toSummon = idleState.AddTransition(summonState);
        toSummon.AddCondition(AnimatorConditionMode.If, 0, "Summon");
        toSummon.hasExitTime = false;

        var summonToIdle = summonState.AddTransition(idleState);
        summonToIdle.hasExitTime = true;
        summonToIdle.exitTime = 0.95f;
        summonToIdle.duration = 0.05f;

        // AnyState -> Hurt
        var anyToHurt = sm.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;

        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 0.9f;
        hurtToIdle.duration = 0.05f;

        // AnyState -> Die
        var anyToDie = sm.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        anyToDie.hasExitTime = false;

        // 3. Tạo GameObject và lưu thành Prefab
        GameObject go = new GameObject("Skeleton_Boss");
        go.tag = "Enemy";
        go.layer = 7; // Layer: Enemy
        go.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);

        var sr = go.AddComponent<SpriteRenderer>();
        var firstSprite = LoadSprites("Skeleton_Boss_Idle.png").FirstOrDefault();
        if (firstSprite != null) sr.sprite = firstSprite;
        sr.sortingOrder = 2;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.offset = new Vector2(0f, 0.75f);
        col.size = new Vector2(1.1f, 1.5f);

        var bossController = go.AddComponent<SkeletonBossController>();
        var so = new SerializedObject(bossController);
        
        var hpProp = so.FindProperty("maxHealth");
        if (hpProp != null) hpProp.intValue = 450;

        var animProp = so.FindProperty("animator");
        if (animProp != null) animProp.objectReferenceValue = anim;

        var groundProp = so.FindProperty("groundLayer");
        if (groundProp != null) groundProp.intValue = 1 << 3;

        var minionProp = so.FindProperty("minionPrefab");
        if (minionProp != null)
        {
            var whitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Map5/Skeleton_White.prefab");
            minionProp.objectReferenceValue = whitePrefab;
        }
        so.ApplyModifiedProperties();

        string prefabPath = $"{PREFAB_DIR}/Skeleton_Boss.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 [SkeletonBossGenerator] ĐÃ TẠO THÀNH CÔNG PREFAB: {prefabPath} VÀ BỘ HOẠT ẢNH HOÀN CHỈNH!");
    }

    private static AnimationClip CreateClip(string textureFileName, string clipFileName, float frameRate, bool loop)
    {
        Sprite[] sprites = LoadSprites(textureFileName);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[SkeletonBossGenerator] Không tìm thấy sprite frames trong: {textureFileName}");
            return null;
        }

        string clipPath = $"{ANIM_DIR}/{clipFileName}";
        AnimationClip clip = new AnimationClip
        {
            frameRate = frameRate
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = (float)i / frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static Sprite[] LoadSprites(string textureFileName)
    {
        string path = $"{ART_DIR}/{textureFileName}";
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<Sprite>()
            .OrderBy(s => GetSpriteIndex(s.name))
            .ToArray();
    }

    private static int GetSpriteIndex(string name)
    {
        int lastUnder = name.LastIndexOf('_');
        if (lastUnder >= 0 && int.TryParse(name.Substring(lastUnder + 1), out int idx))
            return idx;
        return 0;
    }
}
#endif
