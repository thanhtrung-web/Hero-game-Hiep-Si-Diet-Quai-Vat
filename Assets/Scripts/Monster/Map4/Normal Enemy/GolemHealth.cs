using UnityEngine;

public class GolemHealth : EnemyHealth
{
    private GroupNormalEnemy controller;

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<GroupNormalEnemy>();
        if (controller != null)
        {
            maxHealth = Mathf.RoundToInt(controller.maxHealth);
            currentHealth = maxHealth;
        }
    }

    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        if (controller != null)
        {
            controller.TakeDamage(damage);
            currentHealth = Mathf.Max(0, Mathf.RoundToInt(controller.CurrentHealth));
            isDead = controller.IsDead;

            NotifyDamageTaken(damage);
            NotifyHealthChanged(currentHealth, maxHealth);
            SpawnDamagePopup(damage);

            if (isDead)
            {
                NotifyDeath();
            }
        }
        else
        {
            base.TakeDamage(damage);
        }
    }
}
