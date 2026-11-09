using UnityEngine;

public class BossMap4Health : EnemyHealth
{
    private BossMap4Controller bossController;

    public override bool IsBoss => true;
    public override string BossTitle => "Băng Quái Tuyết Sơn - Frost Behemoth";

    protected override void Awake()
    {
        base.Awake();

        bossController = GetComponent<BossMap4Controller>();
        if (bossController != null)
        {
            maxHealth = Mathf.RoundToInt(bossController.maxHealth);
            currentHealth = maxHealth;
        }
    }
 

    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        if (bossController != null)
        {
            bossController.TakeDamage((float)damage);
            currentHealth = Mathf.Max(0, Mathf.RoundToInt(bossController.CurrentHealth));
            isDead = bossController.IsDead;

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
