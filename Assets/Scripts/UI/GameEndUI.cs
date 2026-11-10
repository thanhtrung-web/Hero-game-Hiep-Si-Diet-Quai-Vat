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

        AutoDetectNextMap();
        ResolvePanels();
        SetupCanvas();

        if (gameWin != null)
            gameWin.SetActive(false);
        if (gameLose != null)
            gameLose.SetActive(false);
    }

    private void AutoDetectNextMap()
    {
        if (!string.IsNullOrEmpty(nextMapScene)) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.Contains("Map4"))
        {
            nextMapScene = "Map5";
        }
        else if (currentScene.Contains("Map1"))
        {
            nextMapScene = "The Grave";
        }
        else if (currentScene.Contains("Map2") || currentScene.Contains("Grave") || currentScene.Contains("BossRoom"))
        {
            nextMapScene = "Map3";
        }
        else if (currentScene.Contains("Map3"))
        {
            nextMapScene = "Map4";
        }
        else if (currentScene.Equals("Map5", StringComparison.OrdinalIgnoreCase))
        {
            nextMapScene = "Map5_Phase2";
        }
        else if (currentScene.Contains("Map5_Phase2"))
        {
            nextMapScene = "ChoiceMapSceen";
        }
    }

    private void Start()
    {
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
            if (t != null) gameLose = t.gameObject;
        }

        if (gameWin == null)
        {
            Transform t = transform.Find("GameWin");
            if (t != null) gameWin = t.gameObject;
        }
    }

    private void SetupCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

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

        AutoDetectNextMap();
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
        PersistentHud persistentHud = FindAnyObjectByType<PersistentHud>();
        if (persistentHud != null)
        {
            Canvas c = persistentHud.GetComponent<Canvas>();
            if (c != null)
                c.enabled = false;
            else
                persistentHud.gameObject.SetActive(false);
        }

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
        AutoDetectNextMap();

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
            KnightController knight = FindFirstObjectByType<KnightController>();
            if (knight != null && knight.gameObject.scene.name == "DontDestroyOnLoad")
            {
                Destroy(knight.transform.root.gameObject);
            }
        }
    }
}
