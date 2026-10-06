using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void TryDamage(Collider2D target)
    {
        EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();

        if (enemy == null) return;

        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}