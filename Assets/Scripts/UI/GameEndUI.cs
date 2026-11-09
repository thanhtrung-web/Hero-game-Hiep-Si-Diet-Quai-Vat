using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameWin;
    [SerializeField] private GameObject gameLose;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuScene = "ChoiceMapSceen";
    [SerializeField] private string nextMapScene = "";

    private bool gameEnded;
    public static bool IsGameEnded { get; private set; }

    private void Awake()
    {
        gameEnded = false;
        IsGameEnded = false;

        ResolvePanels();
        SetupCanvas();

        // Luôn luôn ẩn các panel kết thúc game ngay lập tức khi Scene vừa nạp
        if (gameWin != null)
            gameWin.SetActive(false);
        if (gameLose != null)
            gameLose.SetActive(false);
    }

    private void Start()
    {
        // Kiểm tra an toàn một lần nữa tại Start để không bị bất kỳ script nào khác bật nhầm
        if (!gameEnded)
        {
            if (gameWin != null && gameWin.activeSelf)
                gameWin.SetActive(false);
            if (gameLose != null && gameLose.activeSelf)
                gameLose.SetActive(false);
        }
    }

    private void ResolvePanels()
    {
        if (gameLose == null)
        {
            Transform t = transform.Find("GameLose");
            if (t == null && transform.parent != null) t = transform.parent.Find("GameLose");
            if (t != null) gameLose = t.gameObject;
        }

        if (gameWin == null)
        {
            Transform t = transform.Find("GameWin");
            if (t == null && transform.parent != null) t = transform.parent.Find("GameWin");
            if (t != null) gameWin = t.gameObject;
        }
    }

    private void SetupCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        }

        // Nếu Canvas đang nằm trong một GameObject rỗng (ví dụ "UI" trong Map 1), đưa ra Root
        if (canvas.transform.parent != null && canvas.transform.parent.name == "UI")
        {
            canvas.transform.SetParent(null, false);
        }

        if (!canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);

        canvas.enabled = true;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.pivot = new Vector2(0.5f, 0.5f);
            canvasRt.anchoredPosition = Vector2.zero;
            canvasRt.sizeDelta = Vector2.zero;
            canvasRt.localScale = Vector3.one;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void OnDestroy()
    {
        IsGameEnded = false;
        if (gameEnded)
            Time.timeScale = 1f;
    }

    public void ShowWin()
    {
        if (gameEnded) return;

        gameEnded = true;
        IsGameEnded = true;

        ResolvePanels();
        SetupCanvas();
        HideHUD();

        if (gameWin != null)
        {
            RectTransform winRt = gameWin.GetComponent<RectTransform>();
            if (winRt != null)
            {
                winRt.anchorMin = Vector2.zero;
                winRt.anchorMax = Vector2.one;
                winRt.pivot = new Vector2(0.5f, 0.5f);
                winRt.anchoredPosition = Vector2.zero;
                winRt.sizeDelta = Vector2.zero;
                winRt.localScale = Vector3.one;
            }
            gameWin.SetActive(true);
        }

        if (gameLose != null)
            gameLose.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ShowLose()
    {
        if (gameEnded) return;

        gameEnded = true;
        IsGameEnded = true;

        ResolvePanels();
        SetupCanvas();
        HideHUD();

        if (gameLose != null)
        {
            RectTransform loseRt = gameLose.GetComponent<RectTransform>();
            if (loseRt != null)
            {
                loseRt.anchorMin = Vector2.zero;
                loseRt.anchorMax = Vector2.one;
                loseRt.pivot = new Vector2(0.5f, 0.5f);
                loseRt.anchoredPosition = Vector2.zero;
                loseRt.sizeDelta = Vector2.zero;
                loseRt.localScale = Vector3.one;
            }
            gameLose.SetActive(true);
        }

        if (gameWin != null)
            gameWin.SetActive(false);

        Time.timeScale = 0f;
    }

    private void HideHUD()
    {
        // 1. Tắt Canvas của PauseMenu/HUD để không bị đè lên Game Over UI
        PauseMenuController pauseMenu = FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            Canvas canvas = pauseMenu.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
                return;
            }
        }

        // 2. Tìm theo tên GameObject "HUD" nếu có
        GameObject hud = GameObject.Find("HUD");
        if (hud != null)
        {
            Canvas c = hud.GetComponent<Canvas>();
            if (c != null)
                c.enabled = false;
            else
                hud.SetActive(false);
        }
    }

    public void Replay()
    {
        gameEnded = false;
        IsGameEnded = false;
        Time.timeScale = 1f;

        CleanUpPersistentPlayer();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void NextMap()
    {
        if (string.IsNullOrEmpty(nextMapScene))
        {
            Debug.LogWarning("Chua cau hinh Next Map Scene!");
            return;
        }

        gameEnded = false;
        IsGameEnded = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(nextMapScene);
    }

    public void ExitToMenu()
    {
        gameEnded = false;
        IsGameEnded = false;
        Time.timeScale = 1f;

        CleanUpPersistentPlayer();

        string targetScene = string.IsNullOrEmpty(mainMenuScene) ? "ChoiceMapSceen" : mainMenuScene;
        SceneManager.LoadScene(targetScene);
    }

    private void CleanUpPersistentPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.scene.name == "DontDestroyOnLoad")
        {
            Destroy(player.transform.root.gameObject);
        }
        else
        {
            var health = FindAnyObjectByType<PlayerHealth>();
            if (health != null && health.gameObject.scene.name == "DontDestroyOnLoad")
            {
                Destroy(health.transform.root.gameObject);
            }
        }
    }
}
