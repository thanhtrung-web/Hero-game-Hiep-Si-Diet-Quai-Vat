using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 20;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Mage và Knight đều dùng PlayerHealth
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage);
    }
}