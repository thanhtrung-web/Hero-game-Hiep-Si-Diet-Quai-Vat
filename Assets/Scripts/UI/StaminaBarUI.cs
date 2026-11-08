using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh stamina trên HUD. Tự tìm Player được spawn lúc runtime (Mage hoặc Knight)
/// rồi đọc stamina của nhân vật đó mỗi frame.
/// Dùng được với Slider, Image (Fill Amount), hoặc cả hai - gán cái nào dùng cái đó.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("UI (gán Slider hoặc Image đều được)")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Tìm Player")]
    [SerializeField] private float searchInterval = 0.25f;

    private PlayerStamina mageStamina;   // Mage dùng PlayerStamina
    private KnightController knight;     // Knight hiện vẫn tự giữ stamina trong KnightController
    private float nextSearchTime;

    private void Awake()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
        Apply(0f);
    }

    private void Update()
    {
        // Player chưa spawn (hoặc đã bị hủy khi đổi scene) -> thử tìm lại theo chu kỳ
        if (mageStamina == null && knight == null)
        {
            if (Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + searchInterval;
                FindTarget();
            }
            return;
        }

        float current;
        float max;

        if (mageStamina != null)
        {
            current = mageStamina.CurrentStamina;
            max = mageStamina.MaxStamina;
        }
        else
        {
            current = knight.CurrentStamina;
            max = knight.MaxStamina;
        }

        Apply(max > 0f ? current / max : 0f);
    }

    private void FindTarget()
    {
        mageStamina = FindFirstObjectByType<PlayerStamina>();

        if (mageStamina == null)
            knight = FindFirstObjectByType<KnightController>();
    }

    private void Apply(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        if (slider != null)
            slider.value = ratio;

        if (fillImage != null)
            fillImage.fillAmount = ratio;
    }
}