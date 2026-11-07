using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseButtonsPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Settings Controls")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Audio")]
    [SerializeField] private BackgroundMusic backgroundMusic;
    [SerializeField] private AudioMixer sfxMixer;
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    [Header("Exit")]
    [SerializeField] private string mapChoiceSceneName = "ChoiceMap";

    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SfxVolumeKey = "Settings_SFXVolume";
    private const string FullscreenKey = "Settings_Fullscreen";
    private const float DefaultMusicVolume = 0.25f;
    private const float DefaultSfxVolume = 0.8f;

    private bool isPaused;
    private bool warnedAboutSfxMixer;
    private bool isExitingToChoiceMap;

    private void Awake()
    {
        // Ensures a scene loaded after leaving a paused game is not frozen.
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // This controller may live on a persistent Canvas. Allow Exit to work again
        // after returning to ChoiceMap and entering another gameplay scene.
        isExitingToChoiceMap = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (scene.name == mapChoiceSceneName)
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            if (pauseButtonsPanel != null)
                pauseButtonsPanel.SetActive(false);

            if (pausePanel != null)
                pausePanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseButtonsPanel != null)
            pauseButtonsPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (backgroundMusic == null)
            backgroundMusic = BackgroundMusic.Instance;

        if (backgroundMusic == null)
            backgroundMusic = FindAnyObjectByType<BackgroundMusic>();

        InitializeSettingsControls();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (!isPaused)
        {
            PauseGame();
        }
        else if (optionsPanel != null && optionsPanel.activeSelf)
        {
            BackToPauseMenu();
        }
        else
        {
            ResumeGame();
        }
    }

    private void InitializeSettingsControls()
    {
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
        SetFullscreen(fullscreen);
    }

    public void ResetSettings()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(DefaultMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(DefaultSfxVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(false);

        SetMusicVolume(DefaultMusicVolume);
        SetSfxVolume(DefaultSfxVolume);
        SetFullscreen(false);
    }
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (pauseButtonsPanel != null)
            pauseButtonsPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void OpenOptions()
    {
        if (!isPaused)
            return;

        if (pauseButtonsPanel != null)
            pauseButtonsPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
        else
            Debug.LogWarning("PauseMenuController: Chưa gán Options Panel trong Inspector.", this);
    }

    public void BackToPauseMenu()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (pauseButtonsPanel != null)
            pauseButtonsPanel.SetActive(true);
    }

    public void ExitToChoiceMap()
    {
        if (isExitingToChoiceMap)
            return;

        if (string.IsNullOrWhiteSpace(mapChoiceSceneName) ||
            !Application.CanStreamedLevelBeLoaded(mapChoiceSceneName))
        {
            Debug.LogError(
                $"PauseMenuController: Không thể load scene '{mapChoiceSceneName}'. " +
                "Kiểm tra tên scene và File > Build Profiles > Scene List.",
                this);
            return;
        }

        // The Canvas may persist between scenes, so clear the pause UI before loading ChoiceMap.
        isExitingToChoiceMap = true;
        isPaused = false;
        Time.timeScale = 1f;

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (pauseButtonsPanel != null)
            pauseButtonsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        StartCoroutine(DestroyPlayerAndLoadChoiceMap());
    }

    private IEnumerator DestroyPlayerAndLoadChoiceMap()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Destroy the root so a DontDestroyOnLoad Player is removed completely.
            Destroy(player.transform.root.gameObject);

            // Let Unity finish Destroy before the next map is selected and loaded.
            yield return null;
        }

        SceneManager.LoadScene(mapChoiceSceneName);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (backgroundMusic == null)
            backgroundMusic = BackgroundMusic.Instance;

        if (backgroundMusic != null)
            backgroundMusic.SetVolume(volume);

        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (sfxMixer == null)
        {
            if (!warnedAboutSfxMixer)
            {
                Debug.LogWarning("PauseMenuController: Chưa gán Audio Mixer SFX.", this);
                warnedAboutSfxMixer = true;
            }
        }
        else
        {
            float decibels = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
            if (!sfxMixer.SetFloat(sfxVolumeParameter, decibels) && !warnedAboutSfxMixer)
            {
                Debug.LogWarning(
                    $"PauseMenuController: Không tìm thấy parameter '{sfxVolumeParameter}'. " +
                    "Kiểm tra tên Exposed Parameter trong Audio Mixer.",
                    this);
                warnedAboutSfxMixer = true;
            }
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (isPaused)
            Time.timeScale = 1f;
    }
}
