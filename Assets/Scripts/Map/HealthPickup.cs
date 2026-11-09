using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private int healAmount = 25;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Mage và Knight đều dùng PlayerHealth
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.Heal(healAmount))
        {
            Destroy(gameObject);
        }
    }
}