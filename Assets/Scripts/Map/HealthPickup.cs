using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private int healAmount = 25;

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool healed = false;

        // Mage
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            healed = playerHealth.Heal(healAmount);
        }
        else
        {
            // Knight
            KnightController knight = other.GetComponent<KnightController>();
            if (knight != null)
                healed = knight.Heal(healAmount);
        }

        // Chỉ hủy vật phẩm khi thật sự hồi được máu (đầy máu thì giữ lại)
        if (healed)
        {
            Destroy(gameObject);
        }
    }
}