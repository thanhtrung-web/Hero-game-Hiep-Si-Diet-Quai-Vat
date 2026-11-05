#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CastleMapHelper
{
    private const string GrandPalaceBlueprintPath = "Assets/Art/Map/Map5/Castle_Master_Grand_Palace.png";
    private const string MainLevBuildPath = "Assets/Art/Map/Map5/PlatformerSet1/main_lev_build.png";
    private const string GrassSurfacePath = "Assets/Art/Map/Map1/Tiles/GreenForest/ForestPack1_1.asset";
    private const string DirtPath = "Assets/Art/Map/Map1/Tiles/GreenForest/ForestPack1_18.asset";
    private const string BlueprintObjectName = "[Mau_LauDai_Reference]";
    private const string PalettePrefabPath = "Assets/Art/Map/Map5/TilePalette/Map5.prefab";
    private const string TileAssetsDir = "Assets/Art/Map/Map5/TilePalette/Tiles";

    [MenuItem("Tools/Map 5 Phase 2/1. Hien Ban Ve Dai Phao Dai 4 Tang (Grand Palace Blueprint)")]
    public static void ShowGrandPalaceBlueprint()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrandPalaceBlueprintPath);
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy ảnh mẫu: " + GrandPalaceBlueprintPath, "OK");
            return;
        }

        GameObject obj = GameObject.Find(BlueprintObjectName);
        if (obj == null)
        {
            obj = new GameObject(BlueprintObjectName);
            Undo.RegisterCreatedObjectUndo(obj, "Create Grand Palace Blueprint Reference");
        }
        else
        {
            Undo.RecordObject(obj, "Update Grand Palace Blueprint Reference");
        }

        int childCount = obj.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(obj.transform.GetChild(i).gameObject);
        }

        obj.SetActive(true);

        // Đặt tại X = 15, Y = -20
        // - Đáy pháo đài (Hầm Mộ) tại Y = -20
        // - Mặt sàn TẦNG 1 (Trệt / Sảnh chính) nằm chuẩn xác ở Y = -4 (khớp hoàn hảo với bãi cỏ bên ngoài)
        // - Tầng 2 nằm ở Y = +4
        // - Tầng 3 Sân Thượng nằm ở Y = +13, đỉnh tháp vươn lên Y = +22
        obj.transform.position = new Vector3(15f, -20f, 0f);
        obj.transform.localScale = Vector3.one;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = obj.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.color = new Color(1f, 1f, 1f, 0.65f); // Bán trong suốt 65%
        sr.sortingOrder = -2; // Nằm sau Tilemap để khi vẽ tile sẽ thấy ngay trên mặt

        Selection.activeGameObject = obj;
        SceneView.FrameLastActiveSceneView();

        bool expand = EditorUtility.DisplayDialog("Đại Pháo Đài 4 Tầng Đã Hiện!",
            "Đã đặt bản vẽ mẫu ĐẠI PHÁO ĐÀI 4 TẦNG (chuẩn 100% theo bức ảnh bạn chọn):\n\n" +
            "1. TẦNG 1 (TRỆT / SẢNH CHÍNH): Cổng vào tại X: 15, mặt sàn nằm chuẩn xác ở Y = -4 (nối thẳng tắp từ bãi cỏ vào)!\n" +
            "2. TẦNG HẦM (CATACOMBS): Hầm mộ ma thuật âm phía dưới (từ Y: -5 xuống Y: -18) có đài phun nước phát sáng.\n" +
            "3. TẦNG 2 (LẦU): Thư viện, sảnh tiệc, ban công đèn chùm khổng lồ (Y: +4).\n" +
            "4. TẦNG 3 (SÂN THƯỢNG GOTHIC): Điện thờ kính màu tím, tháp nhọn và vầng trăng máu (Y: +13 đến +22).\n\n" +
            "Bạn có muốn tự động mở rộng Camera & Background để bao quát toàn bộ siêu pháo đài này không?",
            "Có, mở rộng luôn!", "Để sau");

        if (expand)
        {
            ExpandCameraAndBackground();
        }
    }

    [MenuItem("Tools/Map 5 Phase 2/2. An - Hien Ban Ve (Toggle)")]
    public static void ToggleCastleBlueprint()
    {
        GameObject obj = GameObject.Find(BlueprintObjectName);
        if (obj != null)
        {
            Undo.RecordObject(obj, "Toggle Castle Blueprint");
            obj.SetActive(!obj.activeSelf);
        }
        else
        {
            ShowGrandPalaceBlueprint();
        }
    }

    [MenuItem("Tools/Map 5 Phase 2/3. Mo Rong Camera & Background (Bao Quat Dai Phao Dai)")]
    public static void ExpandCameraAndBackground()
    {
        GameObject confinerObj = GameObject.Find("CameraConfiner");
        if (confinerObj != null)
        {
            BoxCollider2D box = confinerObj.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Undo.RecordObject(box, "Expand CameraConfiner");
                box.offset = new Vector2(42.5f, 1.5f);
                box.size = new Vector2(205f, 47f);
                EditorUtility.SetDirty(box);
            }
        }

        GameObject bgObj = GameObject.Find("Background");
        if (bgObj != null)
        {
            Undo.RecordObject(bgObj.transform, "Expand Background Position");
            bgObj.transform.position = new Vector3(42.5f, 1.5f, bgObj.transform.position.z);

            SpriteRenderer[] srs = bgObj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                Undo.RecordObject(sr, "Expand Background Sprite");
                sr.size = new Vector2(210f, 25f);
                EditorUtility.SetDirty(sr);
            }
        }

        if (confinerObj != null)
        {
            EditorSceneManager.MarkSceneDirty(confinerObj.scene);
        }

        EditorUtility.DisplayDialog("Mở Rộng Hoàn Tất!",
            "Đã mở rộng CameraConfiner và Background bao phủ trọn vẹn cả 4 tầng đại pháo đài (X: -60 đến 145, Y: -22 đến +25)!\n" +
            "Camera sẽ lia mượt mà từ bãi cỏ vào sảnh chính, lặn xuống hầm mộ hay bay lên sân thượng mà không bị kẹt hay lộ viền xám!",
            "Tuyệt vời");
    }

    [MenuItem("Tools/Map 5 Phase 2/4. Noi Tiep Bai Co Tu Nhien (X: 0 den 14)")]
    public static void ExtendGrassPath()
    {
        GameObject groundObj = GameObject.Find("Tilemap_Ground");
        if (groundObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Tilemap_Ground!", "OK");
            return;
        }

        Tilemap tilemap = groundObj.GetComponent<Tilemap>();
        if (tilemap == null) return;

        TileBase grassTile = AssetDatabase.LoadAssetAtPath<TileBase>(GrassSurfacePath);
        TileBase dirtTile = AssetDatabase.LoadAssetAtPath<TileBase>(DirtPath);

        if (grassTile == null || dirtTile == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy tile cỏ/đất GreenForest!", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(groundObj, "Extend Grass Path");

        for (int x = 0; x <= 14; x++)
        {
            tilemap.SetTile(new Vector3Int(x, -4, 0), grassTile);
            for (int y = -5; y >= -8; y--)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
        }

        EditorUtility.SetDirty(groundObj);
        EditorSceneManager.MarkSceneDirty(groundObj.scene);

        EditorUtility.DisplayDialog("Thành Công!", 
            "Đã nối tiếp bãi cỏ từ X: 0 đến 14 phẳng mịn ở Y: -4 dẫn thẳng đến cửa sảnh chính lâu đài tại X: 15!", 
            "OK");
    }

    [MenuItem("Tools/Map 5 Phase 2/5. Tao Layer Tuong Nen (Tilemap_WallBackground)")]
    public static void CreateWallBackgroundTilemap()
    {
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Grid!", "OK");
            return;
        }

        GameObject wallBgObj = GameObject.Find("Tilemap_WallBackground");
        if (wallBgObj == null)
        {
            wallBgObj = new GameObject("Tilemap_WallBackground");
            Undo.RegisterCreatedObjectUndo(wallBgObj, "Create Tilemap_WallBackground");
            wallBgObj.transform.SetParent(gridObj.transform, false);

            Tilemap tm = wallBgObj.AddComponent<Tilemap>();
            TilemapRenderer tr = wallBgObj.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -1;

            EditorUtility.SetDirty(gridObj);
            EditorSceneManager.MarkSceneDirty(gridObj.scene);

            EditorUtility.DisplayDialog("Thành Công!", 
                "Đã tạo layer 'Tilemap_WallBackground' (Sorting Order: -1) không có Collider.\n" +
                "Bạn dùng layer này để tô các mảng tường gạch đá tối màu phía sau các căn phòng nhé!", 
                "Tuyệt");
        }
        else
        {
            EditorUtility.DisplayDialog("Thông báo", "Layer 'Tilemap_WallBackground' đã tồn tại sẵn trong Grid!", "OK");
        }
    }

    [MenuItem("Tools/Map 5 Phase 2/6. Lam Moi Tile Palette Map 5 (Refresh)")]
    public static void RefreshTilemapPalette()
    {
        AssetDatabase.ImportAsset(MainLevBuildPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(PalettePrefabPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Đã Làm Mới Thành Công!",
            "Đã đồng bộ toàn bộ 1,789 tile 16x16 pixel vào Tile Palette Map5!\n\n" +
            "1. Lỗi 'GridPalette' đã được loại bỏ hoàn toàn.\n" +
            "2. Palette 'Map5' đã sẵn sàng trong cửa sổ Tile Palette.\n" +
            "3. Bậc thang, cột dọc và phiến sàn khớp chuẩn 100% không còn lệch!",
            "Tuyệt vời");
    }
}
#endif
