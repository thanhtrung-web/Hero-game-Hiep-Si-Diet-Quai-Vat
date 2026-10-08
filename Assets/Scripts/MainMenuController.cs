using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mapChoiceSceneName = "ChoiceMap";
    [SerializeField] private string firstSceneName = "Map01_Sceen1";
    [SerializeField] private string map2SceneName = "";
    [SerializeField] private string map3SceneName = "";
    [SerializeField] private string map4SceneName = "";
    [SerializeField] private string map5SceneName = "";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Settings Controls")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Audio")]
    [SerializeField] private BackgroundMusic backgroundMusic;
    [SerializeField] private AudioMixer sfxMixer;
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SfxVolumeKey = "Settings_SFXVolume";
    private const string FullscreenKey = "Settings_Fullscreen";
    private const float DefaultMusicVolume = 0.25f;
    private const float DefaultSfxVolume = 0.8f;
    private bool warnedAboutSfxMixer;

    private void Start()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (backgroundMusic == null)
            backgroundMusic = BackgroundMusic.Instance;

        if (backgroundMusic == null)
            backgroundMusic = FindAnyObjectByType<BackgroundMusic>();

        InitializeSettingsControls();
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
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
        SetFullscreen(fullscreen);
    }

    public void PlayGame()
    {
        OpenMapChoice();
    }

    public void OpenMapChoice()
    {
        LoadSceneByName(mapChoiceSceneName);
    }

    public void LoadMap1()
    {
        LoadSceneByName(firstSceneName);
    }

    public void LoadMap2()
    {
        LoadSceneByName(map2SceneName);
    }

    public void LoadMap3()
    {
        LoadSceneByName(map3SceneName);
    }

    public void LoadMap4()
    {
        LoadSceneByName(map4SceneName);
    }

    public void LoadMap5()
    {
        LoadSceneByName(map5SceneName);
    }

    public void BackToMainMenu()
    {
        LoadSceneByName(mainMenuSceneName);
    }

    private void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MainMenuController: Tên scene chưa được nhập trong Inspector.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"MainMenuController: Không thể load scene '{sceneName}'. " +
                "Kiểm tra tên scene và bảo đảm scene được thêm vào File > Build Profiles > Scene List.",
                this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void OpenOptions()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (optionsPanel == null)
        {
            Debug.LogWarning("MainMenuController: Chưa gán Settings Panel trong Inspector.", this);
            return;
        }

        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (backgroundMusic == null)
            backgroundMusic = BackgroundMusic.Instance;

        if (backgroundMusic != null)
            backgroundMusic.SetVolume(volume);
        else
            Debug.LogWarning("MainMenuController: Không tìm thấy BackgroundMusic trong scene.", this);

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
                Debug.LogWarning("MainMenuController: Chưa gán Audio Mixer để chỉnh âm lượng hiệu ứng.", this);
                warnedAboutSfxMixer = true;
            }
        }
        else
        {
            float decibels = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
            if (!sfxMixer.SetFloat(sfxVolumeParameter, decibels) && !warnedAboutSfxMixer)
            {
                Debug.LogWarning(
                    $"MainMenuController: Không tìm thấy tham số '{sfxVolumeParameter}'. " +
                    "Hãy expose Volume của nhóm SFX trong Audio Mixer với đúng tên này.",
                    this);
                warnedAboutSfxMixer = true;
            }
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        PlayerPrefs.Save();
    }

    // Giữ lại tên hàm cũ để các sự kiện UI cũ không bị mất liên kết.
    // Nút chỉnh nhạc nên dùng SetMusicVolume; nút chỉnh hiệu ứng dùng SetSfxVolume.
    public void SetMasterVolume(float volume)
    {
        SetMusicVolume(volume);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game được gọi. Nút này chỉ đóng game trong bản build.", this);
#else
        Application.Quit();
#endif
    }
}