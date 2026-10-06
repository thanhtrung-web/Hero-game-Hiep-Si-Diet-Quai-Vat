using UnityEngine;

public class SlimeContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f;

    private float nextDamageTime;

    private void OnCollisionStay2D(Collision2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void TryDamagePlayer(GameObject other)
    {
        if (Time.time < nextDamageTime) return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage);
        nextDamageTime = Time.time + damageInterval;
    }
}