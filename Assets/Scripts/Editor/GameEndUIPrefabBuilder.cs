#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public static class GameEndUIPrefabBuilder
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/Game_End.prefab";

    [MenuItem("Tools/UI/Rebuild GameEnd UI (Mau 2A - Pixel Medieval)")]
    public static void RebuildPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null)
        {
            Debug.LogError($"[GameEndUIPrefabBuilder] Không tìm thấy Prefab tại {PREFAB_PATH}!");
            return;
        }

        try
        {
            // 1. Đảm bảo Canvas & CanvasScaler chuẩn Full HD
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null) canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99; // Hiển thị trên mọi UI thường, dưới màn hình Pause

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();

            // 2. Tìm hoặc tạo manage GameObject chứa GameEndUI script
            Transform manageTr = root.transform.Find("manage");
            GameObject manageObj;
            if (manageTr == null)
            {
                manageObj = new GameObject("manage");
                manageObj.transform.SetParent(root.transform, false);
            }
            else
            {
                manageObj = manageTr.gameObject;
            }

            GameEndUI gameEndScript = manageObj.GetComponent<GameEndUI>();
            if (gameEndScript == null) gameEndScript = manageObj.AddComponent<GameEndUI>();

            // Lấy Font chữ chuẩn tiếng Việt
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 3. Xây dựng GameLose (Màn Thất Bại)
            Transform loseTr = root.transform.Find("GameLose");
            GameObject loseObj;
            if (loseTr == null)
            {
                loseObj = new GameObject("GameLose");
                loseObj.transform.SetParent(root.transform, false);
            }
            else
            {
                loseObj = loseTr.gameObject;
                // Dọn dẹp các con cũ
                while (loseObj.transform.childCount > 0)
                {
                    Object.DestroyImmediate(loseObj.transform.GetChild(0).gameObject);
                }
            }

            BuildLosePanel(loseObj, gameEndScript, font);
            loseObj.SetActive(false);

            // 4. Xây dựng GameWin (Màn Chiến Thắng)
            Transform winTr = root.transform.Find("GameWin");
            GameObject winObj;
            if (winTr == null)
            {
                winObj = new GameObject("GameWin");
                winObj.transform.SetParent(root.transform, false);
            }
            else
            {
                winObj = winTr.gameObject;
                // Dọn dẹp các con cũ
                while (winObj.transform.childCount > 0)
                {
                    Object.DestroyImmediate(winObj.transform.GetChild(0).gameObject);
                }
            }

            BuildWinPanel(winObj, gameEndScript, font);
            winObj.SetActive(false);

            // 5. Gán reference vào GameEndUI
            SerializedObject so = new SerializedObject(gameEndScript);
            so.FindProperty("gameLose").objectReferenceValue = loseObj;
            so.FindProperty("gameWin").objectReferenceValue = winObj;
            so.FindProperty("mainMenuScene").stringValue = "ChoiceMapSceen";
            so.ApplyModifiedProperties();

            // 6. Lưu lại Prefab
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Debug.Log("✅ [GameEndUIPrefabBuilder] Đã nướng hoàn tất giao diện Mẫu 2A (Pixel Medieval) vào Game_End.prefab!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildLosePanel(GameObject parent, GameEndUI gameEndScript, Font font)
    {
        // RectTransform toàn màn hình
        RectTransform parentRt = parent.GetComponent<RectTransform>() ?? parent.AddComponent<RectTransform>();
        parentRt.anchorMin = Vector2.zero;
        parentRt.anchorMax = Vector2.one;
        parentRt.sizeDelta = Vector2.zero;
        parentRt.anchoredPosition = Vector2.zero;

        // Overlay mờ tối u ám
        Image bgImg = parent.GetComponent<Image>() ?? parent.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.02f, 0.06f, 0.90f);

        // Khung lâu đài đá viền vàng kim 9-slice kích thước lớn chuẩn theo Map 5 Phase 2 (900x400)
        GameObject cardObj = CreateBox(parent.transform, "CardContainer", new Vector2(0f, 15f), new Vector2(900f, 400f), new Color(0.09f, 0.08f, 0.06f, 0.98f));
        var cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.85f, 0.63f, 0.29f, 0.95f); // Vàng kim
        cardOutline.effectDistance = new Vector2(4f, -4f);

        // Viền phụ màu đen bóng đổ
        var shadowOutline = cardObj.AddComponent<Shadow>();
        shadowOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadowOutline.effectDistance = new Vector2(6f, -6f);

        // Banner ruy băng trên đỉnh
        GameObject bannerObj = CreateBox(cardObj.transform, "Banner", new Vector2(0f, 200f), new Vector2(420f, 54f), new Color(0.47f, 0.09f, 0.11f, 1f));
        var bannerBorder = bannerObj.AddComponent<Outline>();
        bannerBorder.effectColor = new Color(1.0f, 0.48f, 0.51f, 0.95f);
        bannerBorder.effectDistance = new Vector2(3f, -3f);

        CreateText(bannerObj.transform, "BannerText", "☠️  BẠN ĐÃ TỬ TRẬN  ☠️", 20, Color.white, font, TextAnchor.MiddleCenter, true);

        // Tiêu đề chính theo yêu cầu người dùng: "Nhân vật đã bị gục ngã"
        CreateText(cardObj.transform, "SubTitle", "Nhân vật đã bị gục ngã", 28, new Color(0.97f, 0.44f, 0.44f, 1f), font, TextAnchor.MiddleCenter, true, new Vector2(0f, 106f), new Vector2(800f, 48f));

        // Dòng dẫn theo yêu cầu người dùng: "máu đã rơi, hãy đứng lên và phục thù.."
        CreateText(cardObj.transform, "Quote", "\"máu đã rơi, hãy đứng lên và phục thù..\"", 20, new Color(0.84f, 0.82f, 0.82f, 0.92f), font, TextAnchor.MiddleCenter, false, new Vector2(0f, 50f), new Vector2(800f, 36f), FontStyle.Italic);

        // Nút bấm: THỬ LẠI (Đỏ)
        GameObject btnReplay = CreateButton(cardObj.transform, "Button_Replay", new Vector2(-240f, -112f), new Vector2(240f, 58f),
            new Color(0.66f, 0.13f, 0.16f, 1f), new Color(0.85f, 0.22f, 0.25f, 1f),
            "⚔️  THỬ LẠI", font, 24);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnReplay.GetComponent<Button>().onClick, gameEndScript.Replay);

        // Nút bấm: BẢN ĐỒ (Vàng đồng)
        GameObject btnExit = CreateButton(cardObj.transform, "Button_Exit", new Vector2(240f, -112f), new Vector2(240f, 58f),
            new Color(0.65f, 0.49f, 0.18f, 1f), new Color(0.82f, 0.62f, 0.24f, 1f),
            "🗺️  BẢN ĐỒ", font, 24);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnExit.GetComponent<Button>().onClick, gameEndScript.ExitToMenu);
    }

    private static void BuildWinPanel(GameObject parent, GameEndUI gameEndScript, Font font)
    {
        RectTransform parentRt = parent.GetComponent<RectTransform>() ?? parent.AddComponent<RectTransform>();
        parentRt.anchorMin = Vector2.zero;
        parentRt.anchorMax = Vector2.one;
        parentRt.sizeDelta = Vector2.zero;
        parentRt.anchoredPosition = Vector2.zero;

        Image bgImg = parent.GetComponent<Image>() ?? parent.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.90f);

        // Khung lâu đài đá viền vàng kim tinh gọn
        GameObject cardObj = CreateBox(parent.transform, "CardContainer", new Vector2(0f, 15f), new Vector2(900f, 420f), new Color(0.09f, 0.08f, 0.06f, 0.98f));
        var cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.95f, 0.75f, 0.25f, 0.95f);
        cardOutline.effectDistance = new Vector2(4f, -4f);

        var shadowOutline = cardObj.AddComponent<Shadow>();
        shadowOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadowOutline.effectDistance = new Vector2(6f, -6f);

        // Banner ruy băng vàng hoàng gia
        GameObject bannerObj = CreateBox(cardObj.transform, "Banner", new Vector2(0f, 210f), new Vector2(460f, 54f), new Color(0.52f, 0.33f, 0.07f, 1f));
        var bannerBorder = bannerObj.AddComponent<Outline>();
        bannerBorder.effectColor = new Color(1.0f, 0.85f, 0.47f, 0.95f);
        bannerBorder.effectDistance = new Vector2(3f, -3f);

        CreateText(bannerObj.transform, "BannerText", "👑  CHIẾN THẮNG QUANG VINH  👑", 20, Color.white, font, TextAnchor.MiddleCenter, true);

        // Tiêu đề phụ
        CreateText(cardObj.transform, "SubTitle", "ĐÃ VƯỢT QUA CỬA ẢI!", 28, new Color(0.98f, 0.80f, 0.08f, 1f), font, TextAnchor.MiddleCenter, true, new Vector2(0f, 115f), new Vector2(800f, 48f));

        // Câu dẫn truyện
        CreateText(cardObj.transform, "Quote", "\"Đại trùm hắc ám đã bị tiêu diệt hoàn toàn!\"", 20, new Color(0.85f, 0.90f, 0.95f, 0.92f), font, TextAnchor.MiddleCenter, false, new Vector2(0f, 55f), new Vector2(800f, 36f), FontStyle.Italic);

        // Nút: MAP TIẾP THEO (Xanh lục bảo)
        GameObject btnNext = CreateButton(cardObj.transform, "Button_NextMap", new Vector2(0f, -15f), new Vector2(520f, 58f),
            new Color(0.11f, 0.48f, 0.24f, 1f), new Color(0.18f, 0.65f, 0.33f, 1f),
            "➡️  MAP TIẾP THEO", font, 24);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnNext.GetComponent<Button>().onClick, gameEndScript.NextMap);

        // Nút: CHƠI LẠI (Đỏ)
        GameObject btnReplay = CreateButton(cardObj.transform, "Button_Replay", new Vector2(-240f, -125f), new Vector2(240f, 56f),
            new Color(0.66f, 0.13f, 0.16f, 1f), new Color(0.85f, 0.22f, 0.25f, 1f),
            "⚔️  CHƠI LẠI", font, 22);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnReplay.GetComponent<Button>().onClick, gameEndScript.Replay);

        // Nút: BẢN ĐỒ (Vàng đồng)
        GameObject btnExit = CreateButton(cardObj.transform, "Button_Exit", new Vector2(240f, -125f), new Vector2(240f, 56f),
            new Color(0.65f, 0.49f, 0.18f, 1f), new Color(0.82f, 0.62f, 0.24f, 1f),
            "🗺️  BẢN ĐỒ", font, 22);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnExit.GetComponent<Button>().onClick, gameEndScript.ExitToMenu);
    }

    private static GameObject CreateBox(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = color;

        return go;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color, Font font, TextAnchor align, bool bold = false, Vector2 pos = default, Vector2 size = default, FontStyle style = FontStyle.Normal)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        if (size == default)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        Text txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = bold ? FontStyle.Bold : style;
        txt.alignment = align;
        txt.color = color;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, Color normalCol, Color hoverCol, string label, Font font, int fontSize = 20)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.color = normalCol;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = normalCol;
        cb.highlightedColor = hoverCol;
        cb.pressedColor = normalCol * 0.75f;
        cb.selectedColor = hoverCol;
        btn.colors = cb;

        // Viền đen đổ bóng nổi khối
        var btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        // Hiệu ứng âm thanh khi bấm nút
        btnObj.AddComponent<UIButtonSFX>();

        // Chữ trong nút
        CreateText(btnObj.transform, "BtnText", label, fontSize, Color.white, font, TextAnchor.MiddleCenter, true);

        return btnObj;
    }
}
#endif
