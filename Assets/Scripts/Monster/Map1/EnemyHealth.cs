using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] protected int maxHealth = 3;
    [SerializeField] protected Animator animator;
    [SerializeField] protected Behaviour patrolScript;
    public bool IsDead => isDead;
    protected int currentHealth;
    protected bool isDead;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    protected virtual void Die()
    {
        isDead = true;

        if (patrolScript != null)
            patrolScript.enabled = false;

        if (animator != null)
            animator.SetTrigger("Dead");
        else
            Destroy(gameObject);
    }

    public void DestroyAfterDeath()
    {
        if (isDead)
            Destroy(gameObject);
    }
}
