#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class KnightHUDBuilder
{
    static KnightHUDBuilder()
    {
        EditorApplication.delayCall += CheckAndCreateHUD;
    }

    [MenuItem("Tools/Setup Knight HUD")]
    public static void ManualSetup()
    {
        BuildHUD(true);
    }

    private static void CheckAndCreateHUD()
    {
        // Chỉ chạy khi scene hiện tại là Map5
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Map5") return;

        // Nếu đã có Knight_HUD trong scene thì không tạo lại
        if (GameObject.Find("Knight_HUD") != null) return;

        BuildHUD(false);
    }

    private static void BuildHUD(bool force)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // 1. Kiểm tra hoặc tạo EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // 2. Tạo Canvas Knight_HUD
        GameObject canvasObj = new GameObject("Knight_HUD");
        canvasObj.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 3. Container góc trên bên trái
        GameObject container = new GameObject("BarsContainer");
        container.layer = LayerMask.NameToLayer("UI");
        container.transform.SetParent(canvasObj.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(40, -40);
        containerRect.sizeDelta = new Vector2(350, 80);

        // 4. Tạo Thanh Máu (HealthBar)
        GameObject hpBarObj = CreateBar(
            "HealthBar",
            container.transform,
            new Vector2(0, 0),
            new Vector2(320, 26),
            new Color(0.12f, 0.05f, 0.05f, 0.85f), // Nền đỏ đậm
            new Color(0.9f, 0.15f, 0.15f, 1f)      // Đỏ tươi
        );
        hpBarObj.AddComponent<HealthBarUI>();

        // 5. Tạo Thanh Thể Lực (StaminaBar)
        GameObject staBarObj = CreateBar(
            "StaminaBar",
            container.transform,
            new Vector2(0, -34),
            new Vector2(260, 18),
            new Color(0.05f, 0.15f, 0.05f, 0.85f), // Nền xanh rêu đậm
            new Color(0.2f, 0.8f, 0.3f, 1f)        // Xanh lá tươi
        );
        staBarObj.AddComponent<StaminaBarUI>();

        // 6. Lưu thành Prefab trong Assets/Prefabs/UI/
        string prefabDir = "Assets/Prefabs/UI";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
        string prefabPath = $"{prefabDir}/Knight_HUD.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObj, prefabPath, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("<color=green>[KnightHUDBuilder]</color> Đã tạo xong Canvas Knight_HUD (Máu + Thể lực) và lưu vào " + prefabPath);
    }

    private static GameObject CreateBar(string name, Transform parent, Vector2 pos, Vector2 size, Color bgColor, Color fillColor)
    {
        GameObject bar = new GameObject(name);
        bar.layer = LayerMask.NameToLayer("UI");
        bar.transform.SetParent(parent, false);

        RectTransform barRect = bar.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 1);
        barRect.anchoredPosition = pos;
        barRect.sizeDelta = size;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.layer = LayerMask.NameToLayer("UI");
        bgObj.transform.SetParent(bar.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = bgColor;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.layer = LayerMask.NameToLayer("UI");
        fillArea.transform.SetParent(bar.transform, false);

        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-4, -4); // Viền nhỏ 2px
        fillAreaRect.anchoredPosition = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.layer = LayerMask.NameToLayer("UI");
        fillObj.transform.SetParent(fillArea.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fillColor;

        // Slider Component
        Slider slider = bar.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return bar;
    }
}
#endif
