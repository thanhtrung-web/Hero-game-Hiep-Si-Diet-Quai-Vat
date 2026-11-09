#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[InitializeOnLoad]
public static class Map5AudioSetup
{
    static Map5AudioSetup()
    {
        EditorApplication.delayCall += SetupAudio;
    }

    [MenuItem("Tools/Map 5/Setup Monster Audio")]
    public static void ManualSetup()
    {
        SetupAudio();
        Debug.Log("<color=green>[Map5AudioSetup] Thiết lập âm thanh toàn bộ quái Map 5 thành công!</color>");
    }

    public static void SetupAudio()
    {
        // 1. Tải AudioMixer và SFX Group
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/SFX/GameAudioMixer.mixer");
        AudioMixerGroup sfxGroup = null;
        if (mixer != null)
        {
            var groups = mixer.FindMatchingGroups("SFX");
            if (groups != null && groups.Length > 0)
                sfxGroup = groups[0];
        }

        // 2. Tải 5 file âm thanh đã chốt
        AudioClip skeletonNormalClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_Skeleton_Normal.wav");
        AudioClip skeletonBossClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_Skeleton_Boss.wav");
        AudioClip deathKnightClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_DeathKnight.wav");
        AudioClip wraithMageClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_WraithMage.wav");
        AudioClip lichBossClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_LichBoss.wav");
        AudioClip lichDashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_LichBoss_ShadowDash.wav");
        AudioClip lichTeleportClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map5/SFX_LichBoss_Teleport.wav");

        // 3. Cập nhật từng Prefab quái
        ConfigurePrefab<SkeletonController>("Assets/Prefabs/Monster/Map5/Skeleton_White.prefab", skeletonNormalClip, "attackSound", sfxGroup);
        ConfigurePrefab<SkeletonController>("Assets/Prefabs/Monster/Map5/Skeleton_Yellow.prefab", skeletonNormalClip, "attackSound", sfxGroup);
        ConfigurePrefab<SkeletonBossController>("Assets/Prefabs/Monster/Map5/Skeleton_Boss.prefab", skeletonBossClip, "attackSound", sfxGroup);
        ConfigurePrefab<DeathKnightController>("Assets/Prefabs/Monster/Map5/DeathKnight_Mob.prefab", deathKnightClip, "attackSound", sfxGroup);
        ConfigurePrefab<WraithMageController>("Assets/Prefabs/Monster/Map5/WraithMage_Mob.prefab", wraithMageClip, "castSound", sfxGroup);
        ConfigureLichBossPrefab(lichBossClip, lichDashClip, lichTeleportClip, sfxGroup);

        // 4. Cập nhật Knight (Hiệp Sĩ người chơi)
        ConfigureKnightPrefab(sfxGroup);
    }

    private static void ConfigureLichBossPrefab(AudioClip skillClip, AudioClip dashClip, AudioClip teleportClip, AudioMixerGroup sfxGroup)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Map5/Lich_Boss.prefab");
        if (prefab == null) return;

        bool isModified = false;
        AudioSource audioSource = prefab.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = prefab.AddComponent<AudioSource>();
            isModified = true;
        }

        if (audioSource.playOnAwake) { audioSource.playOnAwake = false; isModified = true; }
        if (audioSource.loop) { audioSource.loop = false; isModified = true; }
        if (audioSource.spatialBlend != 0f) { audioSource.spatialBlend = 0f; isModified = true; }
        if (sfxGroup != null && audioSource.outputAudioMixerGroup != sfxGroup)
        {
            audioSource.outputAudioMixerGroup = sfxGroup;
            isModified = true;
        }

        LichBossController controller = prefab.GetComponent<LichBossController>();
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            AssignProp(so, "audioSource", audioSource, ref isModified);
            AssignProp(so, "skillSound", skillClip, ref isModified);
            AssignProp(so, "dashSound", dashClip, ref isModified);
            AssignProp(so, "teleportSound", teleportClip, ref isModified);
            AssignProp(so, "sfxMixerGroup", sfxGroup, ref isModified);

            if (isModified) so.ApplyModifiedProperties();
        }

        if (isModified)
        {
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    private static void ConfigureKnightPrefab(AudioMixerGroup sfxGroup)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero/Knight/Knight.prefab");
        if (prefab == null) return;

        bool isModified = false;
        AudioSource audioSource = prefab.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = prefab.AddComponent<AudioSource>();
            isModified = true;
        }

        if (audioSource.playOnAwake) { audioSource.playOnAwake = false; isModified = true; }
        if (audioSource.loop) { audioSource.loop = false; isModified = true; }
        if (audioSource.spatialBlend != 0f) { audioSource.spatialBlend = 0f; isModified = true; }
        if (sfxGroup != null && audioSource.outputAudioMixerGroup != sfxGroup)
        {
            audioSource.outputAudioMixerGroup = sfxGroup;
            isModified = true;
        }

        KnightController controller = prefab.GetComponent<KnightController>();
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            AssignProp(so, "audioSource", audioSource, ref isModified);
            AudioClip knightAtkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Knight/SFX_Knight_Attack.wav");
            AssignProp(so, "attack1Sound", knightAtkClip, ref isModified);
            AssignProp(so, "attack2Sound", knightAtkClip, ref isModified);
            AssignProp(so, "attack3Sound", knightAtkClip, ref isModified);
            AssignProp(so, "runAttackSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map3/25_Wind_01.wav"), ref isModified);
            AssignProp(so, "blockSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map3/39_Block_03.wav"), ref isModified);
            AssignProp(so, "hitSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map3/61_Hit_03.wav"), ref isModified);
            AssignProp(so, "sfxMixerGroup", sfxGroup, ref isModified);

            if (isModified) so.ApplyModifiedProperties();
        }

        if (isModified)
        {
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    private static void AssignProp(SerializedObject so, string propName, Object val, ref bool isModified)
    {
        if (val == null) return;
        SerializedProperty p = so.FindProperty(propName);
        if (p != null && p.objectReferenceValue != val)
        {
            p.objectReferenceValue = val;
            isModified = true;
        }
    }

    private static void ConfigurePrefab<T>(string prefabPath, AudioClip clip, string soundPropName, AudioMixerGroup sfxGroup) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        bool isModified = false;

        // Đảm bảo có AudioSource
        AudioSource audioSource = prefab.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = prefab.AddComponent<AudioSource>();
            isModified = true;
        }

        if (audioSource.playOnAwake)
        {
            audioSource.playOnAwake = false;
            isModified = true;
        }
        if (audioSource.loop)
        {
            audioSource.loop = false;
            isModified = true;
        }
        if (audioSource.spatialBlend != 0f)
        {
            audioSource.spatialBlend = 0f;
            isModified = true;
        }
        if (sfxGroup != null && audioSource.outputAudioMixerGroup != sfxGroup)
        {
            audioSource.outputAudioMixerGroup = sfxGroup;
            isModified = true;
        }

        // Cập nhật Controller
        T controller = prefab.GetComponent<T>();
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty audioSourceProp = so.FindProperty("audioSource");
            SerializedProperty soundProp = so.FindProperty(soundPropName);
            SerializedProperty mixerProp = so.FindProperty("sfxMixerGroup");

            if (audioSourceProp != null && audioSourceProp.objectReferenceValue != audioSource)
            {
                audioSourceProp.objectReferenceValue = audioSource;
                isModified = true;
            }
            if (soundProp != null && clip != null && soundProp.objectReferenceValue != clip)
            {
                soundProp.objectReferenceValue = clip;
                isModified = true;
            }
            if (mixerProp != null && sfxGroup != null && mixerProp.objectReferenceValue != sfxGroup)
            {
                mixerProp.objectReferenceValue = sfxGroup;
                isModified = true;
            }

            if (isModified)
            {
                so.ApplyModifiedProperties();
            }
        }

        if (isModified)
        {
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }
}
#endif
