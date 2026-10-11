using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ManaBarUI : MonoBehaviour
{
    [SerializeField] private PlayerMana playerMana;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        playerMana = FindAnyObjectByType<PlayerMana>();
        RefreshBar();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The persistent Canvas can outlive the Player. Drop its old reference
        // and look for the PlayerMana belonging to the newly loaded scene.
        playerMana = null;
        RefreshBar();
    }

    private void LateUpdate()
    {
        // Keep trying while a Player is being created by a spawn/persistence script.
        if (playerMana == null)
            playerMana = FindAnyObjectByType<PlayerMana>();

        RefreshBar();
    }

    private void RefreshBar()
    {
        if (slider == null)
            return;

        if (playerMana == null)
        {
            slider.SetValueWithoutNotify(0f);
            return;
        }

        float ratio = playerMana.MaxMana > 0f
            ? playerMana.CurrentMana / playerMana.MaxMana
            : 0f;

        slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
    }
}
