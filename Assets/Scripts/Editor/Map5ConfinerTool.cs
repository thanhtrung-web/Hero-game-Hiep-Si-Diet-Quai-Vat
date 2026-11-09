using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

public static class Map5ConfinerTool
{
    [MenuItem("Tools/Map5/1. Áp dụng Camera Confiner chữ L (Cách 1)")]
    public static void ApplyPolygonConfiner()
    {
        // 1. Tìm GameObject CameraConfiner
        GameObject confinerObj = GameObject.Find("CameraConfiner");
        if (confinerObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject CameraConfiner trong scene!", "OK");
            return;
        }

        // Hỗ trợ Undo nếu bấm Ctrl+Z
        Undo.RegisterFullObjectHierarchyUndo(confinerObj, "Setup L-Shape Confiner");

        // 2. Tắt BoxCollider2D cũ (giữ lại dự phòng)
        BoxCollider2D boxCol = confinerObj.GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.enabled = false;
        }

        // 3. Thêm hoặc lấy PolygonCollider2D
        PolygonCollider2D polyCol = confinerObj.GetComponent<PolygonCollider2D>();
        if (polyCol == null)
        {
            polyCol = Undo.AddComponent<PolygonCollider2D>(confinerObj);
        }

        polyCol.enabled = true;
        polyCol.isTrigger = true;
        polyCol.offset = Vector2.zero;

        // Đưa CameraConfiner về vị trí gốc (0, 0, 0) để tọa độ điểm trùng khớp 100% với World Scene
        Undo.RecordObject(confinerObj.transform, "Reset Confiner Position");
        confinerObj.transform.position = Vector3.zero;

        // 6 điểm tọa độ chữ L bao trọn map:
        // Điểm 1: (-60.5, 21.0)  - Góc trên cùng bên trái (trời bãi cỏ)
        // Điểm 2: (139.2, 21.0)  - Góc trên cùng bên phải (nóc thành)
        // Điểm 3: (139.2, -20.5) - Góc dưới cùng bên phải (sàn hầm)
        // Điểm 4: (12.0, -20.5)  - Góc dưới đáy tường hầm bên trái (chặn camera ở tường hầm)
        // Điểm 5: (12.0, -6.0)   - Góc vuông giao giữa tường hầm và bãi cỏ ngoài trời
        // Điểm 6: (-60.5, -6.0)  - Góc đáy bãi cỏ ngoài trời (chặn camera ngang bãi cỏ, không nhìn xuống dưới lòng đất)
        Vector2[] points = new Vector2[]
        {
            new Vector2(-60.5f, 21.0f),
            new Vector2(139.2f, 21.0f),
            new Vector2(139.2f, -20.5f),
            new Vector2(12.0f, -20.5f),
            new Vector2(12.0f, -6.0f),
            new Vector2(-60.5f, -6.0f)
        };

        polyCol.SetPath(0, points);

        // 4. Gán PolygonCollider2D mới vào CinemachineConfiner2D của CinemachineCamera
        GameObject vcamObj = GameObject.Find("CinemachineCamera");
        if (vcamObj != null)
        {
            CinemachineConfiner2D confiner2D = vcamObj.GetComponent<CinemachineConfiner2D>();
            if (confiner2D != null)
            {
                Undo.RecordObject(confiner2D, "Set BoundingShape2D");
                confiner2D.BoundingShape2D = polyCol;
                confiner2D.InvalidateBoundingShapeCache();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Thành công", "Đã cấu hình xong CameraConfiner chữ L (PolygonCollider2D)!\n\nBạn hãy kiểm tra viền vàng trên Scene hoặc bấm Play để thử nhé.", "OK");
    }

    [MenuItem("Tools/Map5/2. Khôi phục lại Box Collider (Bản cũ)")]
    public static void RestoreBoxConfiner()
    {
        GameObject confinerObj = GameObject.Find("CameraConfiner");
        if (confinerObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject CameraConfiner trong scene!", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(confinerObj, "Restore Box Confiner");

        PolygonCollider2D polyCol = confinerObj.GetComponent<PolygonCollider2D>();
        if (polyCol != null)
        {
            polyCol.enabled = false;
        }

        BoxCollider2D boxCol = confinerObj.GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.enabled = true;
            boxCol.offset = new Vector2(44.7f, 0.52f);
            boxCol.size = new Vector2(199.5f, 41.0f);
        }

        confinerObj.transform.position = new Vector3(-3.95f, -0.02f, 0f);

        GameObject vcamObj = GameObject.Find("CinemachineCamera");
        if (vcamObj != null)
        {
            CinemachineConfiner2D confiner2D = vcamObj.GetComponent<CinemachineConfiner2D>();
            if (confiner2D != null && boxCol != null)
            {
                Undo.RecordObject(confiner2D, "Set BoundingShape2D");
                confiner2D.BoundingShape2D = boxCol;
                confiner2D.InvalidateBoundingShapeCache();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Đã khôi phục", "Đã khôi phục lại BoxCollider2D về bản chuẩn cũ!", "OK");
    }
}
