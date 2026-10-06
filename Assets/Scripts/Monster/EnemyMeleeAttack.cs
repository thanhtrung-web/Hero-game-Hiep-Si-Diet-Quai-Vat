using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Behaviour patrolScript;
    [SerializeField] private EnemyHealth enemyHealth;

    private PlayerHealth playerHealth;
    private float cooldownTimer;
    private float searchTimer;
    private bool isAttacking;

    private bool IsDead => enemyHealth != null && enemyHealth.IsDead;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (IsDead)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (playerHealth == null)
        {
            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                FindPlayer();
                searchTimer = 1f;
            }

            return;
        }

        if (isAttacking || cooldownTimer > 0f)
            return;

        float distance = Vector2.Distance(
            transform.position,
            playerHealth.transform.position
        );

        if (distance <= attackRange)
        {
            Debug.Log($"Quái vào tầm đánh Player. Khoảng cách: {distance:F2}", this);
            StartAttack();
        }
    }

    private void FindPlayer()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
            Debug.Log("Quái đã tìm thấy PlayerHealth.", this);
        else
            Debug.LogWarning("Không tìm thấy PlayerHealth đang hoạt động trong Scene.", this);
    }

    private void StartAttack()
    {
        isAttacking = true;
        cooldownTimer = attackCooldown;

        if (patrolScript != null)
            patrolScript.enabled = false;

        if (rb != null)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = 0f;
            rb.linearVelocity = velocity;
        }

        Debug.Log("Quái bắt đầu tấn công.", this);
        animator.SetTrigger("Attack");
    }

    // Animation Event ở frame đòn đánh chạm Player.
    public void DealAttackDamage()
    {
        if (IsDead || !isAttacking || playerHealth == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            playerHealth.transform.position
        );

        if (distance <= attackRange)
        {
            playerHealth.TakeDamage(damage);
            Debug.Log($"Quái gây {damage} sát thương cho Player.", this);
        }
        else
        {
            Debug.Log("Player đã ra khỏi tầm trước khi đòn đánh chạm.", this);
        }
    }

    // Animation Event ở frame cuối của clip Attack.
    public void FinishAttack()
    {
        if (IsDead)
            return;

        isAttacking = false;

        if (patrolScript != null)
            patrolScript.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}