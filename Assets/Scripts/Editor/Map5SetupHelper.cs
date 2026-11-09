#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Map5SetupHelper
{
    [MenuItem("Tools/Map 5/Setup Portals and GameEnd")]
    public static void SetupAll()
    {
        SetupMap5Phase1();
        SetupMap5Phase2();
        SetupAbyss();
        SetupBossRoomBarrierForMap5Phase2();
        SetupQuestUI();
        SetupMonsterItemDrops();
        AssetDatabase.SaveAssets();
        Debug.Log("✅ [Map5SetupHelper] Đã hoàn tất cài đặt toàn bộ Cổng chuyển cảnh, Boss, Vực Thẳm, Phong ấn, Quest UI, Rơi Bình Máu và Game_End cho Map 5!");
    }

    [MenuItem("Tools/Map 5/Setup Monster Health Potion Drops (Cài đặt Tỉ lệ Rơi Bình Máu)")]
    public static void SetupMonsterItemDropsMenu()
    {
        SetupMonsterItemDrops();
        AssetDatabase.SaveAssets();
        Debug.Log("🧪 [Map5SetupHelper] Đã thiết lập hoàn tất tỉ lệ rơi bình máu cho tất cả quái vật Map 5!");
    }

    public static void SetupMonsterItemDrops()
    {
        var healSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/HealSmall.prefab");
        var healBigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/HealBig.prefab");

        // 1. Skeleton White: 10% HealSmall
        ConfigurePrefabDrop("Assets/Prefabs/Monster/Map5/Skeleton_White.prefab", healSmallPrefab, 0.10f);

        // 2. Skeleton Yellow: 20% HealSmall
        ConfigurePrefabDrop("Assets/Prefabs/Monster/Map5/Skeleton_Yellow.prefab", healSmallPrefab, 0.20f);

        // 3. Skeleton Boss: 100% HealBig (hoặc HealSmall)
        ConfigurePrefabDrop("Assets/Prefabs/Monster/Map5/Skeleton_Boss.prefab", healBigPrefab != null ? healBigPrefab : healSmallPrefab, 1.0f);

        // 4. Wraith Mage: 30% HealSmall
        ConfigurePrefabDrop("Assets/Prefabs/Monster/Map5/WraithMage_Mob.prefab", healSmallPrefab, 0.30f);

        // 5. Death Knight: 30% HealSmall
        ConfigurePrefabDrop("Assets/Prefabs/Monster/Map5/DeathKnight_Mob.prefab", healSmallPrefab, 0.30f);
    }

    private static void ConfigurePrefabDrop(string prefabPath, GameObject itemPrefab, float chance)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        var dropper = prefab.GetComponent<EnemyItemDropper>();
        if (dropper == null)
        {
            dropper = prefab.AddComponent<EnemyItemDropper>();
        }

        var so = new SerializedObject(dropper);
        var itemProp = so.FindProperty("dropItemPrefab");
        var chanceProp = so.FindProperty("dropChance");

        if (itemProp != null) itemProp.objectReferenceValue = itemPrefab;
        if (chanceProp != null) chanceProp.floatValue = chance;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(prefab);
        PrefabUtility.RecordPrefabInstancePropertyModifications(dropper);

        Debug.Log($"🧪 [SetupDrop] {prefab.name} -> Item: {(itemPrefab != null ? itemPrefab.name : "null")}, Tỉ lệ: {chance * 100:0}%");
    }

    [MenuItem("Tools/Map 5/Setup Quest UI (Thanh Nhiệm Vụ)")]
    public static void SetupQuestUIMenu()
    {
        SetupQuestUI();
        AssetDatabase.SaveAssets();
        Debug.Log("📜 [Map5SetupHelper] Đã thiết lập hoàn chỉnh Thanh Nhiệm Vụ (Quest UI) cho cả 2 Phase Map 5!");
    }

    public static void SetupQuestUI()
    {
        SetupQuestUIForMap5Phase1();
        SetupQuestUIForMap5Phase2();
    }

    [MenuItem("Tools/Map 5/Setup Boss Room Barrier (Phong Ấn Phòng Boss)")]
    public static void SetupBossRoomBarrierMenu()
    {
        SetupBossRoomBarrierForMap5Phase2();
        AssetDatabase.SaveAssets();
        Debug.Log("🛡️ [Map5SetupHelper] Đã thiết lập hoàn chỉnh Phong Ấn Phòng Boss (BossBarrier) cho Map 5 Phase 2!");
    }

    [MenuItem("Tools/Map 5/Setup Abyss (Vực Thẳm & Rơi Cook)")]
    public static void SetupAbyss()
    {
        SetupAbyssForMap5Phase1();
        SetupAbyssForMap5Phase2();
        AssetDatabase.SaveAssets();
        Debug.Log("🌌 [Map5SetupHelper] Đã thiết lập hoàn chỉnh Vực Thẳm (Abyss Kill Zone & Mảng tối) cho cả 2 Phase Map 5!");
    }

    private static void SetupAbyssForMap5Phase1()
    {
        string scenePath = "Assets/Scenes/Map5/Map5.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var existing = Object.FindAnyObjectByType<AbyssKillZone>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var abyssObj = AbyssKillZone.EnsureAbyssInMap5Phase1();
            Undo.RegisterCreatedObjectUndo(abyssObj, "Add AbyssKillZone to Map5");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupAbyssForMap5Phase2()
    {
        string scenePath = "Assets/Scenes/Map5/Map5_Phase2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var existing = Object.FindAnyObjectByType<AbyssKillZone>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var abyssObj = AbyssKillZone.EnsureAbyssInMap5Phase2();
            Undo.RegisterCreatedObjectUndo(abyssObj, "Add AbyssKillZone to Map5_Phase2");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupMap5Phase1()
    {
        string scenePath = "Assets/Scenes/Map5/Map5.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Kiểm tra / Thêm Game_End Prefab
        var gameEnd = Object.FindAnyObjectByType<GameEndUI>(FindObjectsInactive.Include);
        if (gameEnd == null)
        {
            var gameEndPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Game_End.prefab");
            if (gameEndPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(gameEndPrefab);
                instance.name = "Game_End";
                Undo.RegisterCreatedObjectUndo(instance, "Add Game_End to Map5");
                Debug.Log("➕ [Map5] Đã thêm Game_End Prefab vào Map5.unity");
            }
        }

        // 2. Kiểm tra / Tạo Map5_ExitPortal
        GameObject portalObj = GameObject.Find("Map5_ExitPortal");
        if (portalObj == null)
        {
            var scenePortal = Object.FindAnyObjectByType<ScenePortal>(FindObjectsInactive.Include);
            if (scenePortal != null)
            {
                portalObj = scenePortal.gameObject;
            }
        }

        if (portalObj == null)
        {
            var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Map5/Map5_ExitPortal.prefab");
            if (portalPrefab != null)
            {
                portalObj = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab);
                portalObj.name = "Map5_ExitPortal";
                portalObj.transform.position = new Vector3(84.6f, 2.764f, 0f);
                portalObj.transform.localScale = new Vector3(2f, 2f, 1f);
                Undo.RegisterCreatedObjectUndo(portalObj, "Add Map5_ExitPortal to Map5");
                Debug.Log("🌀 [Map5] Đã thêm Map5_ExitPortal từ Prefab vào Map5.unity");
            }
            else
            {
                portalObj = new GameObject("Map5_ExitPortal");
                portalObj.transform.position = new Vector3(84.6f, 2.764f, 0f);
                portalObj.transform.localScale = new Vector3(2f, 2f, 1f);

                var sr = portalObj.AddComponent<SpriteRenderer>();
                var portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/TempPortal/Portal 1.png");
                if (portalSprite != null) sr.sprite = portalSprite;
                sr.sortingOrder = 5;

                var col = portalObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.2f, 1.8f);

                var sp = portalObj.AddComponent<ScenePortal>();
                sp.TargetSceneName = "Map5_Phase2";
            }
        }

        if (portalObj != null)
        {
            var sp = portalObj.GetComponent<ScenePortal>();
            if (sp != null)
            {
                var spSo = new SerializedObject(sp);
                var targetProp = spSo.FindProperty("targetSceneName");
                if (targetProp != null) targetProp.stringValue = "Map5_Phase2";
                spSo.ApplyModifiedProperties();
            }

            // Mặc định ẩn portal đi trong Phase 1, khi Boss chết sẽ hiện lên
            portalObj.SetActive(false);
        }

        // 3. Gán exitPortal vào Skeleton_Boss
        var boss = Object.FindAnyObjectByType<SkeletonBossController>(FindObjectsInactive.Include);
        if (boss != null && portalObj != null)
        {
            var bossSo = new SerializedObject(boss);
            var exitPortalProp = bossSo.FindProperty("exitPortal");
            if (exitPortalProp != null)
            {
                exitPortalProp.objectReferenceValue = portalObj;
                bossSo.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(boss);
                EditorUtility.SetDirty(boss);
                Debug.Log("🔗 [Map5] Đã kết nối Skeleton_Boss.exitPortal -> Map5_ExitPortal!");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupMap5Phase2()
    {
        string scenePath = "Assets/Scenes/Map5/Map5_Phase2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Vector3 spawnCoords = new Vector3(-56.35f, 1.91f, 0f);

        // 1. Kiểm tra / Tạo PlayerSpawn tại x: -56.35, y: 1.91
        var playerSpawn = GameObject.Find("PlayerSpawn");
        if (playerSpawn == null)
        {
            playerSpawn = new GameObject("PlayerSpawn");
            Undo.RegisterCreatedObjectUndo(playerSpawn, "Create PlayerSpawn in Map5_Phase2");
        }
        playerSpawn.transform.position = spawnCoords;

        // 2. Cấu hình Cổng dịch chuyển vào Screen 2 (tại x: -56.35, y: 1.91)
        var portalObj = GameObject.Find("Map5_ExitPortal") ?? GameObject.Find("Map5_EntrancePortal");
        if (portalObj == null)
        {
            var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Map5/Map5_ExitPortal.prefab");
            if (portalPrefab != null)
            {
                portalObj = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab);
                portalObj.name = "Map5_ExitPortal";
                Undo.RegisterCreatedObjectUndo(portalObj, "Add Map5_ExitPortal to Map5_Phase2");
            }
        }

        if (portalObj != null)
        {
            portalObj.name = "Map5_ExitPortal";
            portalObj.transform.position = spawnCoords;
            portalObj.transform.localScale = new Vector3(2f, 2f, 1f);
            portalObj.SetActive(true);

            var sr = portalObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 2; // Hiển thị phía sau hiệp sĩ
            }

            var sp = portalObj.GetComponent<ScenePortal>();
            if (sp != null)
            {
                sp.TargetSceneName = "Map5"; // Cho phép bước ngược lại nếu cần
                sp.IsOneWay = false;
            }
            Debug.Log($"🌀 [Map5_Phase2] Đã định vị cổng dịch chuyển tại {spawnCoords}!");
        }

        // 3. Đảm bảo Knight và Camera tracking đúng vị trí
        var knight = GameObject.Find("Knight");
        if (knight != null)
        {
            knight.transform.position = spawnCoords;
        }

        var cinemachineCamera = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include);
        if (cinemachineCamera != null && knight != null)
        {
            cinemachineCamera.Target.TrackingTarget = knight.transform;
        }

        // 4. Kiểm tra / Thêm Game_End Prefab
        var gameEnd = Object.FindAnyObjectByType<GameEndUI>(FindObjectsInactive.Include);
        if (gameEnd == null)
        {
            var gameEndPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Game_End.prefab");
            if (gameEndPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(gameEndPrefab);
                instance.name = "Game_End";
                Undo.RegisterCreatedObjectUndo(instance, "Add Game_End to Map5_Phase2");
                Debug.Log("➕ [Map5_Phase2] Đã thêm Game_End Prefab vào Map5_Phase2.unity");
            }
        }

        // 5. Kiểm tra / Thêm Lich_Boss vào Map5_Phase2 nếu chưa có
        var lichBoss = Object.FindAnyObjectByType<LichBossController>(FindObjectsInactive.Include);
        if (lichBoss == null)
        {
            var lichBossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Map5/Lich_Boss.prefab");
            if (lichBossPrefab != null)
            {
                var enemiesParent = GameObject.Find("Enemies");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(lichBossPrefab, enemiesParent != null ? enemiesParent.transform : null);
                instance.name = "Lich_Boss";
                instance.transform.position = new Vector3(31f, -31.5f, 0f);
                Undo.RegisterCreatedObjectUndo(instance, "Add Lich_Boss to Map5_Phase2");
                Debug.Log("💀👑 [Map5_Phase2] Đã thêm Lich_Boss Prefab vào Map5_Phase2.unity tại đáy hầm x:31, y:-31.5!");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupBossRoomBarrierForMap5Phase2()
    {
        string scenePath = "Assets/Scenes/Map5/Map5_Phase2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var existing = Object.FindAnyObjectByType<BossBarrier>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var barrierObj = GameObject.Find("Map5_BossBarrier");
            if (barrierObj == null)
            {
                barrierObj = new GameObject("Map5_BossBarrier");
                barrierObj.transform.position = new Vector3(45.5f, -29.2f, 0f);
                barrierObj.transform.localScale = new Vector3(1.2f, 3.5f, 1f);

                var sr = barrierObj.AddComponent<SpriteRenderer>();
                var portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/TempPortal/Portal 1.png");
                if (portalSprite != null) sr.sprite = portalSprite;
                sr.color = new Color(0.7f, 0.25f, 1f, 0.85f);
                sr.sortingOrder = 5;

                // Solid collider chặn người chơi đi qua
                var col = barrierObj.AddComponent<BoxCollider2D>();
                col.isTrigger = false;
                col.size = new Vector2(1f, 1.8f);

                var barrier = barrierObj.AddComponent<BossBarrier>();

                // Gán SFX
                var hitSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map3/39_Block_03.wav");
                var unlockSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Preview/Lich_Teleport_OptA_VoidBlink.wav");

                var so = new SerializedObject(barrier);
                so.FindProperty("autoTrackAllMonsters").boolValue = true;
                so.FindProperty("barrierTitle").stringValue = "Phong ấn phòng Đại Trùm Lich";
                if (hitSfx != null) so.FindProperty("barrierHitSound").objectReferenceValue = hitSfx;
                if (unlockSfx != null) so.FindProperty("barrierUnlockSound").objectReferenceValue = unlockSfx;
                so.ApplyModifiedProperties();

                // Tạo Prefab tái sử dụng
                string prefabPath = "Assets/Prefabs/Map/Map5/Map5_BossBarrier.prefab";
                PrefabUtility.SaveAsPrefabAsset(barrierObj, prefabPath);

                Undo.RegisterCreatedObjectUndo(barrierObj, "Add BossBarrier to Map5_Phase2");
                Debug.Log("🛡️ [Map5_Phase2] Đã tạo rào chắn phong ấn ma thuật Map5_BossBarrier tại vị trí cửa phòng Boss (x: 45.5, y: -29.2)!");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupQuestUIForMap5Phase1()
    {
        string scenePath = "Assets/Scenes/Map5/Map5.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var existing = Object.FindAnyObjectByType<QuestUIController>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var questPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Quest.prefab");
            if (questPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(questPrefab);
                instance.name = "Quest";
                Undo.RegisterCreatedObjectUndo(instance, "Add Quest UI to Map5");
                Debug.Log("📜 [Map5] Đã thêm Quest.prefab vào Map5.unity!");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupQuestUIForMap5Phase2()
    {
        string scenePath = "Assets/Scenes/Map5/Map5_Phase2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var existing = Object.FindAnyObjectByType<QuestUIController>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var questPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Quest.prefab");
            if (questPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(questPrefab);
                instance.name = "Quest";
                Undo.RegisterCreatedObjectUndo(instance, "Add Quest UI to Map5_Phase2");
                Debug.Log("📜 [Map5_Phase2] Đã thêm Quest.prefab vào Map5_Phase2.unity!");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
