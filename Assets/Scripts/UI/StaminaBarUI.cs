using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] private KnightController knight;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.3f, 1f); // Xanh lục tươi
    [SerializeField] private Color lowStaminaColor = new Color(0.95f, 0.25f, 0.2f, 1f); // Đỏ cảnh báo nguy cơ vỡ khiên

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;

        if (fillImage == null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindKnight();
        RefreshBar();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        knight = null;
        RefreshBar();
    }

    private void LateUpdate()
    {
        if (knight == null)
            FindKnight();

        RefreshBar();
    }

    private void FindKnight()
    {
        knight = FindAnyObjectByType<KnightController>();
    }

    private void RefreshBar()
    {
        if (slider == null)
            return;

        if (knight == null)
        {
            slider.SetValueWithoutNotify(0f);
            return;
        }

        float ratio = knight.MaxStamina > 0
            ? knight.CurrentStamina / knight.MaxStamina
            : 0f;

        slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));

        if (fillImage != null)
        {
            // Khi thể lực dưới 20% (nguy cơ bị guard-break vỡ khiên) -> đổi sang màu đỏ
            fillImage.color = ratio < 0.2f ? lowStaminaColor : normalColor;
        }
    }
}
