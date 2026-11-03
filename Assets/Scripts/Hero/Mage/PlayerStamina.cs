using UnityEngine;

/// <summary>
/// Thể lực dùng chung (thay thế PlayerMana).
/// - Chạy nước rút tiêu tốn thể lực, hết thể lực thì khóa chạy (kiểu Knight).
/// - Skill của Mage (fireball, flame) cũng trừ vào thể lực qua TrySpend().
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenPerSecond = 4f;        // Giống Knight (4/s)
    [SerializeField] private float sprintCostPerSecond = 10f;  // Giống Knight (10/s)

    private float currentStamina;
    private bool isExhausted;   // Khóa chạy khi cạn sạch thể lực
    private bool regenBlocked;  // Đang chạy nước rút thì không hồi

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => isExhausted;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    // LateUpdate chạy sau mọi Update nên cờ regenBlocked luôn được set kịp trong cùng frame
    private void LateUpdate()
    {
        if (!regenBlocked)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
        }
        regenBlocked = false;
    }

    /// <summary>Trừ thể lực cho skill. Không đủ thì trả về false và không trừ gì.</summary>
    public bool TrySpend(float amount)
    {
        if (amount < 0f || currentStamina < amount)
            return false;

        currentStamina -= amount;
        return true;
    }

    /// <summary>
    /// Gọi mỗi frame khi đang chạy nước rút.
    /// Trả về true nếu vẫn được chạy, false nếu đã kiệt sức (bị khóa chạy).
    /// </summary>
    public bool DrainSprint(float deltaTime)
    {
        if (isExhausted || currentStamina <= 0f)
        {
            isExhausted = true;
            return false;
        }

        regenBlocked = true;
        currentStamina = Mathf.Max(0f, currentStamina - sprintCostPerSecond * deltaTime);

        if (currentStamina <= 0f)
        {
            isExhausted = true;
            return false;
        }

        return true;
    }

    /// <summary>Gọi khi nhả Shift: phải nhả rồi bấm lại mới chạy được tiếp.</summary>
    public void ClearExhausted()
    {
        isExhausted = false;
    }
}
