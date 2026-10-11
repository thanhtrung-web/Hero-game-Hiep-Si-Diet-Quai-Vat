using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Behaviour patrolScript;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform rangeOrigin;

    private PlayerHealth playerHealth;
    private float cooldownTimer;
    private float playerSearchTimer;
    private bool isAttacking;

    private bool IsDead => enemyHealth != null && enemyHealth.IsDead;

    private Vector2 RangeCenter => rangeOrigin != null
        ? (Vector2)rangeOrigin.position
        : (Vector2)transform.position;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
            playerSearchTimer -= Time.deltaTime;

            if (playerSearchTimer <= 0f)
            {
                FindPlayer();
                playerSearchTimer = 1f;
            }

            return;
        }

        if (isAttacking || cooldownTimer > 0f)
            return;

        float distance = Vector2.Distance(RangeCenter, playerHealth.transform.position);

        if (distance <= attackRange && PlayerIsInFront())
            StartAttack();
    }

    private void FindPlayer()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
            Debug.Log("Quái đã tìm thấy PlayerHealth.", this);
        else
            Debug.LogWarning("Không tìm thấy PlayerHealth đang hoạt động trong Scene.", this);
    }

    private bool PlayerIsInFront()
    {
        if (playerHealth == null || spriteRenderer == null)
            return false;

        float offsetX = playerHealth.transform.position.x - RangeCenter.x;

        // flipX = false: quái nhìn sang phải; flipX = true: quái nhìn sang trái.
        return spriteRenderer.flipX ? offsetX < 0f : offsetX > 0f;
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

        animator.SetTrigger("Attack");
    }

    // Add an Animation Event on the frame the attack should hit the Player.
    public void DealAttackDamage()
    {
        if (IsDead || !isAttacking || playerHealth == null)
            return;

        float distance = Vector2.Distance(RangeCenter, playerHealth.transform.position);

        if (distance <= attackRange && PlayerIsInFront())
            playerHealth.TakeDamage(damage);
    }

    // Add an Animation Event on the last frame of the Attack clip.
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
        Vector3 center = rangeOrigin != null
            ? rangeOrigin.position
            : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);

        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        Vector3 forward = facingLeft ? Vector3.left : Vector3.right;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(center, center + forward * attackRange);
    }
}
