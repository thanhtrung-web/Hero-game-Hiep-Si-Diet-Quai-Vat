using System.Runtime.CompilerServices;
using UnityEngine;

[DisallowMultipleComponent]
public class CaveTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageInterval = 1f;

    // Adjacent spike sprites may both touch the same Knight in one physics step.
    private sealed class Cooldown { public float nextHit; }
    private static readonly ConditionalWeakTable<KnightController, Cooldown> cooldowns = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        DealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DealDamage(other);
    }

    private void DealDamage(Collider2D other)
    {
        KnightController knight =
            other.GetComponentInParent<KnightController>();

        if (knight == null)
            return;

        if (!knight.CompareTag("Player") &&
            !other.CompareTag("Player"))
            return;

        var cooldown = cooldowns.GetValue(knight, _ => new Cooldown());
        if (Time.time < cooldown.nextHit)
            return;

        cooldown.nextHit = Time.time + damageInterval;
        knight.TakeDamage(damage);
    }
}
