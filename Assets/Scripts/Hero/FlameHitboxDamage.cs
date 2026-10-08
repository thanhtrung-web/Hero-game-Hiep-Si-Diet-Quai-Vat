using System.Collections.Generic;
using UnityEngine;

public class FlameHitboxDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    private readonly HashSet<EnemyHealth> hitEnemies = new();

    private void OnEnable()
    {
        hitEnemies.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy != null && hitEnemies.Add(enemy))
            enemy.TakeDamage(damage);
    }
}