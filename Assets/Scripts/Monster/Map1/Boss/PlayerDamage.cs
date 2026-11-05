using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cách duy nhất để quái/boss gây sát thương lên hero.
/// - Knight: đi qua KnightController.TakeDamage để khiên đỡ đòn / vỡ khiên hoạt động.
/// - Mage: đi thẳng PlayerHealth.TakeDamage.
/// </summary>
public static class PlayerDamage
{
    public static bool TryDamage(Collider2D collider, int damage)
    {
        return TryDamage(collider, damage, null);
    }

    /// <summary>Trả về true nếu collider thuộc về hero. alreadyHit dùng để mỗi hero chỉ trúng 1 lần.</summary>
    public static bool TryDamage(Collider2D collider, int damage, HashSet<Component> alreadyHit)
    {
        if (collider == null)
            return false;

        KnightController knight = collider.GetComponentInParent<KnightController>();
        if (knight != null)
        {
            if (alreadyHit == null || alreadyHit.Add(knight))
                knight.TakeDamage(damage);
            return true;
        }

        PlayerHealth health = collider.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            if (alreadyHit == null || alreadyHit.Add(health))
                health.TakeDamage(damage);
            return true;
        }

        return false;
    }

    /// <summary>Quét vùng chữ nhật, mỗi hero chỉ nhận sát thương 1 lần. Trả về số hero trúng.</summary>
    public static int DamageInBox(Vector2 center, Vector2 size, float angle, int damage)
    {
        HashSet<Component> damaged = new HashSet<Component>();

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, angle))
            TryDamage(hit, damage, damaged);

        return damaged.Count;
    }
}
