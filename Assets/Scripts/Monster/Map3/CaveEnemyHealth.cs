using UnityEngine;

namespace HeroGame.Map3
{
    // Map 3 adapter. KnightController looks up EnemyHealth, so inheritance
    // preserves compatibility without editing Knight or Map 1 source files.
    [DisallowMultipleComponent]
    public class CaveEnemyHealth : EnemyHealth
    {
        private CaveEnemy caveEnemy;

        protected override void Awake()
        {
            base.Awake();
            caveEnemy = GetComponent<CaveEnemy>();
        }

        public override void TakeDamage(int damage)
        {
            if (isDead || damage <= 0) return;

            base.TakeDamage(damage);

            if (!caveEnemy) caveEnemy = GetComponent<CaveEnemy>();
            if (!caveEnemy) return;

            if (isDead) caveEnemy.NotifyDeath();
            else caveEnemy.NotifyDamaged();
        }
    }
}
