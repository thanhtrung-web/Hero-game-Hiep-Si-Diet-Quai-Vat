#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Map5MonsterSetup
{
    private struct MonsterSpawnData
    {
        public string prefabPath;
        public string instanceName;
        public Vector3 position;
        public Vector3 localScale;

        public MonsterSpawnData(string path, string name, float x, float y, float scale = 1f)
        {
            prefabPath = path;
            instanceName = name;
            position = new Vector3(x, y, 0f);
            localScale = new Vector3(scale, scale, 1f);
        }
    }

    [MenuItem("Tools/Map 5/Setup 30 Monsters (Map5_Phase2)")]
    public static void SetupMonstersMap5Phase2()
    {
        string scenePath = "Assets/Scenes/Map5/Map5_Phase2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Tìm hoặc tạo GameObject cha "Enemies"
        GameObject enemiesParent = GameObject.Find("Enemies");
        if (enemiesParent == null)
        {
            enemiesParent = new GameObject("Enemies");
            Undo.RegisterCreatedObjectUndo(enemiesParent, "Create Enemies Parent");
        }

        // Xóa các quái cũ trong Enemies nếu có (để tránh trùng lặp)
        var children = new List<GameObject>();
        foreach (Transform child in enemiesParent.transform)
        {
            children.Add(child.gameObject);
        }
        foreach (var c in children)
        {
            Undo.DestroyObjectImmediate(c);
        }

        // Xóa bất kỳ instance Lich_Boss / Monster nào nằm ngoài Enemies nếu có
        var existingBosses = Object.FindObjectsByType<LichBossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in existingBosses)
        {
            Undo.DestroyObjectImmediate(b.gameObject);
        }

        // 2. Danh sách Prefab Paths
        string skelWhitePath  = "Assets/Prefabs/Monster/Map5/Skeleton_White.prefab";
        string skelYellowPath = "Assets/Prefabs/Monster/Map5/Skeleton_Yellow.prefab";
        string deathKnightPath = "Assets/Prefabs/Monster/Map5/DeathKnight_Mob.prefab";
        string wraithMagePath  = "Assets/Prefabs/Monster/Map5/WraithMage_Mob.prefab";
        string lichBossPath    = "Assets/Prefabs/Monster/Map5/Lich_Boss.prefab";

        // 3. Danh sách 30 quái phân bổ theo thiết kế
        var monsterList = new List<MonsterSpawnData>
        {
            // === KHU VỰC 1: BÃI CỎ NGOÀI TRỜI (6 quái) ===
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Grass_1",   -6.0f,  -2.3f),
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Grass_2",   -1.0f,  -2.3f),
            new MonsterSpawnData(skelYellowPath, "Skeleton_Yellow_Grass_1",   4.0f,  -2.3f),
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Grass_3",    9.0f,  -2.3f),
            new MonsterSpawnData(skelYellowPath, "Skeleton_Yellow_Grass_2",  14.0f,  -2.3f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_Gate_Guard",   20.0f,  -2.0f),

            // === KHU VỰC 2: TẦNG TRỆT & SẢNH DƯỚI (8 quái) ===
            new MonsterSpawnData(deathKnightPath,"DeathKnight_LowerHall_1",  30.0f,  -4.0f),
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Lower_1",   60.0f,  -4.3f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_LowerHall_2",  72.0f,  -5.0f),
            new MonsterSpawnData(wraithMagePath, "WraithMage_LowerPerch_1",  82.0f,  -4.5f),
            new MonsterSpawnData(skelYellowPath, "Skeleton_Yellow_Lower_1",  97.0f,  -5.3f),
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Dungeon_1", 20.0f,  -8.3f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_Dungeon_1",    26.0f,  -8.0f),
            new MonsterSpawnData(wraithMagePath, "WraithMage_Dungeon_1",     33.0f,  -7.5f),

            // === KHU VỰC 3: TẦNG LẦU & THÁP CANH (8 quái) ===
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Upper_1",   22.0f,   4.7f),
            new MonsterSpawnData(skelYellowPath, "Skeleton_Yellow_Upper_1",  30.0f,   4.7f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_UpperHall_1",  65.0f,   4.0f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_UpperHall_2", 105.0f,   4.0f),
            new MonsterSpawnData(wraithMagePath, "WraithMage_UpperPerch_1", 122.0f,   4.5f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_UpperHall_3",  92.0f,  10.0f),
            new MonsterSpawnData(wraithMagePath, "WraithMage_UpperPerch_2", 106.0f,  10.5f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_TowerSentinel",75.0f,  17.0f),

            // === KHU VỰC 4: HÀNH LANG NGẦM & CẦU THANG ĐÁ (7 quái) ===
            new MonsterSpawnData(skelWhitePath,  "Skeleton_White_Basement_1", 125.0f, -15.3f),
            new MonsterSpawnData(skelYellowPath, "Skeleton_Yellow_Basement_1",112.0f, -15.3f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_Basement_1",     98.0f, -15.0f),
            new MonsterSpawnData(wraithMagePath, "WraithMage_StairsGuard_1",   84.0f, -14.5f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_StairsGuard_1",  70.0f, -20.0f),
            new MonsterSpawnData(skelYellowPath, "Skeleton_Yellow_Stairs_1",   60.0f, -25.3f),
            new MonsterSpawnData(deathKnightPath,"DeathKnight_ArenaGate_1",    48.0f, -30.0f),

            // === KHU VỰC 5: ĐẠI SẢNH ĐẤU TRƯỜNG ĐÁY TẦNG HẦM (1 BOSS TRÙM CUỐI) ===
            new MonsterSpawnData(lichBossPath,   "Lich_Boss",                 31.0f, -31.5f)
        };

        // 4. Instantiate từng quái vào Scene
        int count = 0;
        foreach (var data in monsterList)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"❌ [Map5MonsterSetup] Không tìm thấy Prefab tại: {data.prefabPath}");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemiesParent.transform);
            if (instance != null)
            {
                instance.name = data.instanceName;
                instance.transform.position = data.position;
                instance.transform.localScale = data.localScale;

                // Đảm bảo Layer và Tag là Enemy
                instance.layer = LayerMask.NameToLayer("Enemy");
                if (instance.layer < 0) instance.layer = 7;
                instance.tag = "Enemy";

                // Đảm bảo con của prefab (nếu có collider/hitbox) cũng kế thừa layer Enemy
                foreach (Transform sub in instance.transform)
                {
                    if (sub.name.ToLower().Contains("hitbox") || sub.name.ToLower().Contains("body"))
                    {
                        sub.gameObject.layer = instance.layer;
                        sub.tag = "Enemy";
                    }
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Spawn {data.instanceName}");
                count++;
            }
        }

        // Lưu scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ [Map5MonsterSetup] ĐÃ SETUP THÀNH CÔNG {count}/30 QUÁI VÀO Map5_Phase2.unity! Boss tọa lạc tại Đáy hầm x:31, y:-31.5!");
    }
}
#endif
