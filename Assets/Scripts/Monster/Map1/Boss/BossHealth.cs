using System;
using UnityEngine;

/// <summary>
/// Máu của Boss. EnemyDamage (Knight + Mage) tự nhận diện component này,
/// nên Boss KHÔNG cần EnemyHealth nữa.
/// </summary>
public class BossHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 500;
    [SerializeField, Range(0.05f, 0.95f)] private float phase2Threshold = 0.5f;

    private int currentHealth;
    private bool isDead;
    private bool inPhase2;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float Ratio => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;
    public bool InPhase2 => inPhase2;

    public event Action<int, int> OnHealthChanged; // (current, max) - dùng cho thanh máu Boss
    public event Action<int> OnDamaged;            // sát thương vừa nhận (chưa chết)
    public event Action OnPhase2;                  // xuống ngưỡng máu Phase 2
    public event Action OnDied;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            isDead = true;
            OnDied?.Invoke();
            return;
        }

        OnDamaged?.Invoke(amount);

        if (!inPhase2 && Ratio <= phase2Threshold)
        {
            inPhase2 = true;
            OnPhase2?.Invoke();
        }
    }
}
