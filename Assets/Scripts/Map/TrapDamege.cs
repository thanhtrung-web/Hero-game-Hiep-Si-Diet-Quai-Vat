using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 20;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Mage
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            return;
        }

        // Knight: trap xuyên qua khiên (canBeBlocked = false)
        KnightController knight = other.GetComponent<KnightController>();
        if (knight != null)
        {
            knight.TakeDamage(damage, false);
        }
    }
}