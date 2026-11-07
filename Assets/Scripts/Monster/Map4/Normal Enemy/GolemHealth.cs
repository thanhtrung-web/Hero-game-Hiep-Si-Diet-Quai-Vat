using UnityEngine;

public class GolemHealth : EnemyHealth
{
    private GroupNormalEnemy controller;

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<GroupNormalEnemy>();
    }

    public override void TakeDamage(int damage)
    {
        if (controller != null)
        {
            controller.TakeDamage(damage);
        }
        else
        {
            base.TakeDamage(damage);
        }
    }
}
