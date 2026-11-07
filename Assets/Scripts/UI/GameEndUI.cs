
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameEndUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameWin;
    [SerializeField] private GameObject gameLose;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuScene = "GameMenuSceen";
    [SerializeField] private string nextMapScene = "";

    private bool gameEnded;

    private void Awake()
    {
        gameEnded = false;

        Debug.Log("[GameEndUI] Awake - Khoi tao UI", this);

        if (gameWin != null)
            gameWin.SetActive(false);
        else
            Debug.LogError("[GameEndUI] Chua gan GameWin!", this);

        if (gameLose != null)
            gameLose.SetActive(false);
        else
            Debug.LogError("[GameEndUI] Chua gan GameLose!", this);

        Debug.Log(
            "[GameEndUI] Awake xong | GameLose Active = " +
            (gameLose != null && gameLose.activeInHierarchy),
            this
        );
    }

    private void Start()
    {
        Debug.Log(
            "[GameEndUI] Start | GameLose Active = " +
            (gameLose != null && gameLose.activeInHierarchy),
            this
        );
    }

    private void Update()
    {
        if (gameLose != null &&
            gameLose.activeInHierarchy &&
            !gameEnded)
        {
            Debug.LogError(
                "[GameEndUI] GameLose bi bat tu ben ngoai! " +
                "ShowLose chua duoc goi.",
                this
            );

            // Tam thoi tat de tranh spam log
            gameLose.SetActive(false);
        }
    }

    public void ShowWin()
    {
        Debug.LogWarning(
            "[GameEndUI] ShowWin duoc goi!\n" +
            Environment.StackTrace,
            this
        );

        if (gameEnded) return;

        gameEnded = true;
        gameWin.SetActive(true);
        gameLose.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ShowLose()
    {
   

        if (gameEnded) return;

        gameEnded = true;
        gameLose.SetActive(true);
        gameWin.SetActive(false);

        Time.timeScale = 0f;
    }

    public void Replay()
    {
        Time.timeScale = 1f;
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

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextMapScene);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
