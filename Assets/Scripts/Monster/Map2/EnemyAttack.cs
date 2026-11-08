using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy patrol;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform rangeOrigin;
    [Tooltip("Bật nếu scale X dương nghĩa là quái đang nhìn sang phải.")]
    [SerializeField] private bool positiveScaleFacesRight = true;

    private PlayerHealth playerHealth;
    private float cooldownTimer;
    private float playerSearchTimer;
    private bool isAttacking;

    private Vector2 RangeCenter => rangeOrigin != null
        ? (Vector2)rangeOrigin.position
        : (Vector2)transform.position;

    private bool IsDead => enemyHealth != null && enemyHealth.IsDead;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (patrol == null)
            patrol = GetComponent<Enemy>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (playerTarget != null)
            playerHealth = playerTarget.GetComponentInParent<PlayerHealth>();
    }

    private void Update()
    {
        if (IsDead || animator == null)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (playerTarget == null)
        {
            playerSearchTimer -= Time.deltaTime;
            if (playerSearchTimer <= 0f)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
                if (playerHealth != null)
                    playerTarget = playerHealth.transform;
                else
                {
                    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                    if (playerObject != null)
                    {
                        playerTarget = playerObject.transform;
                        playerHealth = playerObject.GetComponentInParent<PlayerHealth>();
                    }
                }
                playerSearchTimer = 1f;
            }
            return;
        }

        if (isAttacking || cooldownTimer > 0f)
            return;

        float distance = Vector2.Distance(RangeCenter, playerTarget.position);
        if (distance <= attackRange && PlayerIsInFront())
            StartAttack();
    }

    private bool PlayerIsInFront()
    {
        if (playerTarget == null)
            return false;

        float offsetX = playerTarget.position.x - RangeCenter.x;
        bool facingRight = (transform.localScale.x >= 0f) == positiveScaleFacesRight;
        return facingRight ? offsetX > 0f : offsetX < 0f;
    }

    private void StartAttack()
    {
        isAttacking = true;
        cooldownTimer = attackCooldown;

        if (patrol != null)
            patrol.enabled = false;

        animator.SetBool("IsMoving", false);
        animator.SetTrigger("Attack");
    }

    // Add an Animation Event on the frame the attack should hit the Player.
    public void DealAttackDamage()
    {
        if (IsDead || !isAttacking || playerHealth == null || playerTarget == null)
            return;

        float distance = Vector2.Distance(RangeCenter, playerTarget.position);
        if (distance <= attackRange && PlayerIsInFront())
            playerHealth.TakeDamage(damage);
    }

    // Add an Animation Event on the last frame of the Attack clip.
    public void FinishAttack()
    {
        isAttacking = false;

        if (!IsDead && patrol != null)
        {
            patrol.enabled = true;
            animator.SetBool("IsMoving", true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = rangeOrigin != null ? rangeOrigin.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
