using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
    }

    private void LateUpdate()
    {
        if (playerHealth == null)
            return;

        float ratio = playerHealth.MaxHealth > 0
            ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;

        slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
    }
}