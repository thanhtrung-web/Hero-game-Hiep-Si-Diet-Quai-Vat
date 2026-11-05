using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class DeathKnightController : EnemyHealth
{
    public enum KnightState
    {
        Spawn,      // Trỗi dậy từ hư không / hố năng lượng
        Patrol,     // Tuần tra chậm rãi, đại kiếm vác trên vai
        Chase,      // Áp sát Hiệp sĩ
        Attack1,    // Chém kiếm ngang nhanh (Slash Arc)
        Attack2,    // Bổ kiếm xuống đất kích nổ gai ma pháp (Overhead Slam)
        Escape,     // Lướt bóng đêm lùi lại né phản công (Shadow Dash)
        Returning   // Quay về vị trí tuần tra ban đầu
    }

    [Header("1. Movement & Patrol")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float chaseSpeed = 3.0f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeCheckDistance = 0.8f;
    [SerializeField] private float groundCheckDepth = 1.0f;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float patrolRadius = 4f;

    [Header("2. Combat Settings")]
    [SerializeField] private float aggroRange = 5.0f;
    [SerializeField] private float deAggroRange = 7.0f;
    [SerializeField] private float attack1Range = 2.0f;
    [SerializeField] private float attack2Range = 2.4f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private int attack1Damage = 18;
    [SerializeField] private int attack2Damage = 28;

    [Header("3. Hurt & Defense")]
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private float hurtDuration = 0.25f;
    [SerializeField] private float escapeCooldown = 5.0f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D myCollider;
    private Transform player;

    private KnightState currentState = KnightState.Patrol;
    private int direction = 1; // 1 = phải, -1 = trái
    private bool isAttacking = false;
    private bool isHurt = false;
    private float cooldownTimer = 0f;
    private float hurtTimer = 0f;
    private float escapeCooldownTimer = 0f;
    private float attackTimeoutTimer = 0f;
    private int comboCounter = 0;

    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;

    public KnightState CurrentState => currentState;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        spawnX = transform.position.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
    }

    private void Update()
    {
        if (isDead) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (escapeCooldownTimer > 0f) escapeCooldownTimer -= Time.deltaTime;

        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f) isHurt = false;
            return;
        }

        if (player == null) FindPlayer();

        if (isAttacking)
        {
            attackTimeoutTimer -= Time.deltaTime;
            if (attackTimeoutTimer <= 0f) isAttacking = false;
            return;
        }

        float playerFeetY = player != null ? player.position.y : 0f;
        if (player != null)
        {
            var pCol = player.GetComponent<Collider2D>();
            if (pCol != null) playerFeetY = pCol.bounds.min.y;
        }

        float monsterFeetY = myCollider != null ? myCollider.bounds.min.y : transform.position.y;
        float horizontalDist = player != null ? Mathf.Abs(transform.position.x - player.position.x) : float.MaxValue;
        float verticalDist = player != null ? Mathf.Abs(playerFeetY - monsterFeetY) : float.MaxValue;
        bool isSameHeight = player != null && verticalDist < 2.2f;

        switch (currentState)
        {
            case KnightState.Spawn:
                // Handled on trigger/event, returns to Patrol
                break;

            case KnightState.Patrol:
                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = KnightState.Chase;
                }
                else
                {
                    Patrol();
                }
                break;

            case KnightState.Chase:
                if (horizontalDist > deAggroRange || !isSameHeight)
                {
                    currentState = KnightState.Returning;
                    break;
                }

                if (horizontalDist <= attack1Range)
                {
                    FaceTarget(player.position.x);
                    StopMoving();

                    if (cooldownTimer <= 0f)
                    {
                        StartAttack();
                    }
                }
                else
                {
                    FaceTarget(player.position.x);
                    Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
                    RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

                    if (groundHit.collider)
                        MoveForward(chaseSpeed);
                    else
                        StopMoving();
                }
                break;

            case KnightState.Returning:
                float distToSpawn = Mathf.Abs(transform.position.x - spawnX);
                if (distToSpawn <= 0.3f)
                {
                    currentState = KnightState.Patrol;
                    StopMoving();
                    break;
                }

                FaceTarget(spawnX);
                MoveForward(moveSpeed);

                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = KnightState.Chase;
                }
                break;
        }
    }

    private void Patrol()
    {
        if (transform.position.x <= minPatrolX && direction == -1)
            direction = 1;
        else if (transform.position.x >= maxPatrolX && direction == 1)
            direction = -1;

        Vector2 wallOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

        if (wallHit.collider || !groundHit.collider)
        {
            direction = -direction;
        }

        UpdateFacing();
        MoveForward(moveSpeed);
    }

    private void MoveForward(float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null) animator.SetBool("IsMoving", false);
    }

    private void FaceTarget(float targetX)
    {
        int newDir = (targetX > transform.position.x) ? 1 : -1;
        if (newDir != direction)
        {
            direction = newDir;
            UpdateFacing();
        }
    }

    private void UpdateFacing()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }

    private void StartAttack()
    {
        isAttacking = true;
        StopMoving();
        cooldownTimer = attackCooldown;
        attackTimeoutTimer = 1.4f;

        comboCounter++;
        if (comboCounter % 3 == 0)
        {
            // Attack 2: Overhead ground slam
            if (animator != null) animator.SetTrigger("Attack2");
        }
        else
        {
            // Attack 1: Slash Arc
            if (animator != null) animator.SetTrigger("Attack1");
        }
    }

    // Animation Event: gọi ở thời điểm vung kiếm Attack 1
    public void DealSlashDamage()
    {
        DealDamageInArea(attack1Range, attack1Damage);
    }

    // Animation Event: gọi ở thời điểm bổ kiếm xuống đất Attack 2
    public void DealSlamDamage()
    {
        DealDamageInArea(attack2Range, attack2Damage);
    }

    private void DealDamageInArea(float range, int damage)
    {
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * (range * 0.5f), 0.7f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, range * 0.6f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var knight = hit.GetComponentInParent<KnightController>();
                if (knight != null) knight.TakeDamage(damage);

                var mage = hit.GetComponentInParent<PlayerHealth>();
                if (mage != null) mage.TakeDamage(damage);
            }
        }
    }

    // Animation Event: kết thúc đòn đánh
    public void FinishAttack()
    {
        isAttacking = false;

        // Cơ hội lướt bóng đêm né tránh (Escape) nếu người chơi vẫn ở rất gần
        if (player != null && escapeCooldownTimer <= 0f && Random.value < 0.4f)
        {
            float dist = Mathf.Abs(transform.position.x - player.position.x);
            if (dist < 1.5f)
            {
                TriggerEscapeDash();
            }
        }
    }

    private void TriggerEscapeDash()
    {
        escapeCooldownTimer = escapeCooldown;
        if (animator != null) animator.SetTrigger("Escape");
        rb.linearVelocity = new Vector2(-direction * 5f, rb.linearVelocity.y);
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;
        base.TakeDamage(damage);

        if (player != null)
        {
            currentState = KnightState.Chase;
            int hitFrom = (player.position.x > transform.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(-hitFrom * knockbackForce, 2.5f);
        }

        isHurt = true;
        hurtTimer = hurtDuration;
        isAttacking = false;
    }

    protected override void Die()
    {
        base.Die();
        StopMoving();
        rb.simulated = false;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.5f);
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attack1Range);
    }
}
