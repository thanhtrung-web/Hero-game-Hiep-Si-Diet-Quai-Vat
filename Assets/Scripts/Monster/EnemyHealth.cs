using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Animator animator;
    [SerializeField] private Behaviour patrolScript;
    public bool IsDead => isDead;
    private int currentHealth;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth == 0)
        {
            isDead = true;

            if (patrolScript != null)
                patrolScript.enabled = false;

            if (animator != null)
                animator.SetTrigger("Dead");
            else
                Destroy(gameObject);

            return;
        }

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    
    public void DestroyAfterDeath()
    {
        if (isDead)
            Destroy(gameObject);
    }
}