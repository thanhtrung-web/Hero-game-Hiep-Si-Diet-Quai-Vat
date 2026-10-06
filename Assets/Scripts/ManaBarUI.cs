using UnityEngine;
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

    private void LateUpdate()
    {
        if (playerMana == null)
            return;

        float ratio = playerMana.MaxMana > 0f
            ? playerMana.CurrentMana / playerMana.MaxMana
            : 0f;

        slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
    }
}