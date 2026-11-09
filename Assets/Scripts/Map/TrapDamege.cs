using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float damageCooldown = 0.8f;

    private float lastDamageTime = -999f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        // Mage và Knight đều dùng PlayerHealth (tìm ở cả chính nó hoặc cha)
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        lastDamageTime = Time.time;
        playerHealth.TakeDamage(damage);
    }
}
