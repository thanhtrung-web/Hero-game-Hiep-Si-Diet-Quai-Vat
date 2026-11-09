using UnityEngine;

public class BossMap4Health : EnemyHealth
{
    private BossMap4Controller bossController;

    protected override void Awake()
    {
        base.Awake();
        bossController = GetComponent<BossMap4Controller>();
    }

    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        if (bossController != null)
        {
            bossController.TakeDamage((float)damage);
        }
        else
        {
            base.TakeDamage(damage);
        }
    }
}
