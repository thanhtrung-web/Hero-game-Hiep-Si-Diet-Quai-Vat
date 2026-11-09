using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deals damage using PlayerAttack.FlameDamage, not a separate hitbox value.
/// Each EnemyHealth can be damaged once per Flame hitbox activation.
/// </summary>
public class FlameHitboxDamage : MonoBehaviour
{
    [Header("Damage Source")]
    [Tooltip("PlayerAttack of the knight casting Flame. Leave empty for automatic lookup in a single-player scene.")]
    [SerializeField] private PlayerAttack attackSource;

    private readonly HashSet<EnemyHealth> hitEnemies = new();

    // Recommended for Flame prefabs instantiated separately from the Knight.
    public void SetAttackSource(PlayerAttack source)
    {
        attackSource = source;
    }

    private void Awake()
    {
        ResolveAttackSource();
    }

    private void OnEnable()
    {
        hitEnemies.Clear();
        ResolveAttackSource();
    }

    private PlayerAttack ResolveAttackSource()
    {
        if (attackSource != null)
            return attackSource;

        attackSource = GetComponentInParent<PlayerAttack>();

        // Fallback for a spawned Flame prefab that is not parented to the Knight.
        // For multiplayer/multiple knights, call SetAttackSource when spawning instead.
        if (attackSource == null)
        {
#if UNITY_2022_2_OR_NEWER
            attackSource = FindFirstObjectByType<PlayerAttack>();
#else
            attackSource = FindObjectOfType<PlayerAttack>();
#endif
        }

        return attackSource;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null)
            return;

        PlayerAttack source = ResolveAttackSource();
        if (source == null)
        {
            Debug.LogWarning("FlameHitboxDamage: Cannot find PlayerAttack to read FlameDamage.", this);
            return;
        }

        if (hitEnemies.Add(enemy))
            enemy.TakeDamage(source.FlameDamage);
    }
}
