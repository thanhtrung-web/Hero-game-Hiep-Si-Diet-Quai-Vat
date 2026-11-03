using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Arena Limits")]
    [SerializeField] private Transform leftLimit;
    [SerializeField] private Transform rightLimit;

    private Rigidbody2D rb;

    private bool isChasing;
    private bool isAttacking;
    private bool canAttack = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer =
            Mathf.Abs(player.position.x - transform.position.x);

        // Phát hiện Player
        isChasing = distanceToPlayer <= detectionRange;

        if (isChasing && !isAttacking)
            FacePlayer();

        // Đang Attack thì không xử lý Walk
        if (isAttacking)
        {
            animator.SetBool("IsWalking", false);
            return;
        }

        // Player trong Attack Range
        if (isChasing &&
            distanceToPlayer <= attackRange &&
            canAttack)
        {
            StartCoroutine(Attack());
            return;
        }

        // Chỉ Walk khi chưa đủ gần
        bool shouldWalk =
            isChasing && distanceToPlayer > stopDistance;

        animator.SetBool("IsWalking", shouldWalk);
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        // Không di chuyển trong lúc Attack
        if (isAttacking)
        {
            StopMoving();
            return;
        }

        float distanceToPlayer =
            Mathf.Abs(player.position.x - transform.position.x);

        if (!isChasing)
        {
            StopMoving();
            return;
        }

        // Đủ gần thì dừng
        if (distanceToPlayer <= stopDistance)
        {
            StopMoving();
            return;
        }

        MoveTowardsPlayer();
    }

    private void MoveTowardsPlayer()
    {
        float direction =
            Mathf.Sign(player.position.x - transform.position.x);

        float targetX =
            rb.position.x +
            direction * moveSpeed * Time.fixedDeltaTime;

        // Không ra khỏi Boss Arena
        if (leftLimit != null && rightLimit != null)
        {
            targetX = Mathf.Clamp(
                targetX,
                leftLimit.position.x,
                rightLimit.position.x
            );
        }

        rb.MovePosition(
            new Vector2(targetX, rb.position.y)
        );
    }

    private void StopMoving()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null)
            return;

        bool playerIsLeft =
            player.position.x < transform.position.x;

        spriteRenderer.flipX = playerIsLeft;
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        canAttack = false;

        StopMoving();

        animator.SetBool("IsWalking", false);

        // Chọn Attack1 -> Attack4
        int attackIndex = Random.Range(1, 5);

        animator.SetInteger("AttackIndex", attackIndex);
        animator.SetTrigger("Attack");

        // Chờ Animation Event báo Attack kết thúc
        yield return new WaitUntil(() => !isAttacking);

        // Cooldown trước đòn tiếp theo
        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    // Gọi bằng Animation Event ở cuối mỗi Attack
    public void EndAttack()
    {
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Detection
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        // Attack Range
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        // Arena
        if (leftLimit != null && rightLimit != null)
        {
            Gizmos.DrawLine(
                new Vector3(
                    leftLimit.position.x,
                    transform.position.y
                ),
                new Vector3(
                    rightLimit.position.x,
                    transform.position.y
                )
            );
        }
    }
}