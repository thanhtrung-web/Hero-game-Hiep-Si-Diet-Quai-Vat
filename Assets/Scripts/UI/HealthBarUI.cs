using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private KnightController knight;

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
        FindPlayer();
        RefreshBar();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The persistent Canvas can outlive the Player. Drop its old reference
        // and look for the player belonging to the newly loaded scene.
        playerHealth = null;
        knight = null;
        RefreshBar();
    }

    private void LateUpdate()
    {
        // Keep trying while a Player is being created by a spawn/persistence script.
        if (playerHealth == null && knight == null)
            FindPlayer();

        RefreshBar();
    }

    private void FindPlayer()
    {
        knight = FindAnyObjectByType<KnightController>();
        if (knight == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    private void RefreshBar()
    {
        if (slider == null)
            return;

        if (knight != null)
        {
            float ratio = knight.MaxHealth > 0
                ? (float)knight.CurrentHealth / knight.MaxHealth
                : 0f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
            return;
        }

        if (playerHealth != null)
        {
            float ratio = playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
                : 0f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
            return;
        }

        slider.SetValueWithoutNotify(0f);
    }
}
