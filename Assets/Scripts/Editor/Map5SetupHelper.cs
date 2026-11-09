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
        AssetDatabase.SaveAssets();
        Debug.Log("✅ [Map5SetupHelper] Đã hoàn tất cài đặt toàn bộ Cổng chuyển cảnh, Boss và Game_End cho Map 5!");
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
}
#endif
