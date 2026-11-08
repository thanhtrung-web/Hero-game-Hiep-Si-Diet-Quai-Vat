using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SkeletonController : EnemyHealth
{
    // Các trạng thái của Quái
    public enum EnemyState
    {
        Patrol,     // Đi tuần tra bình thường trong vùng [minPatrolX, maxPatrolX]
        Chase,      // Thù địch: bỏ giới hạn tuần tra, đuổi theo Hiệp sĩ
        Returning   // Bỏ thù địch: đi bộ trở về vùng lãnh thổ ban đầu
    }

    [Header("1. Movement & Patrol (Tuần tra thông minh)")]
    [Tooltip("Tốc độ bước đi")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Tốc độ khi đuổi theo Hiệp sĩ")]
    [SerializeField] private float chaseSpeed = 3f;

    [Tooltip("Layer mặt đất (Chọn layer Ground)")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Khoảng cách phía trước để dò mép vực")]
    [SerializeField] private float ledgeCheckDistance = 0.8f;

    [Tooltip("Độ sâu dò sàn")]
    [SerializeField] private float groundCheckDepth = 1.0f;

    [Tooltip("Khoảng cách dò bức tường phía trước")]
    [SerializeField] private float wallCheckDistance = 0.6f;

    [Tooltip("Bán kính tuần tra từ điểm spawn (Ví dụ đứng ở 5, radius = 4 thì đi từ 1 -> 9)")]
    [SerializeField] private float patrolRadius = 4f;

    [Header("2. Aggro & Combat (Hệ thống thù địch & chiến đấu)")]
    [Tooltip("Khoảng cách phát hiện để BẮT ĐẦU THÙ ĐỊCH và đuổi theo Hiệp sĩ")]
    [SerializeField] private float aggroRange = 4.5f;

    [Tooltip("Khoảng cách Hiệp sĩ chạy xa khiến quái BỎ THÙ ĐỊCH và đi bộ về lại nhà")]
    [SerializeField] private float deAggroRange = 5.5f;

    [Tooltip("Tầm kiếm để dừng lại vung combo chém")]
    [SerializeField] private float attackRange = 1.8f;

    [Tooltip("Thời gian nghỉ giữa các lần tung combo (giây)")]
    [SerializeField] private float attackCooldown = 2.0f;

    [Tooltip("Sát thương mỗi nhát chém")]
    [SerializeField] private int attackDamage = 10;

    [Header("3. Hurt & Knockback (Bị thương)")]
    [Tooltip("Lực giật lùi khi bị Hiệp sĩ chém trúng")]
    [SerializeField] private float knockbackForce = 3f;

    [Tooltip("Thời gian bị khựng khi trúng đòn (giây)")]
    [SerializeField] private float hurtDuration = 0.25f;

    // Component nội bộ
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D myCollider;
    private Transform player;

    // Trạng thái quái
    private EnemyState currentState = EnemyState.Patrol;
    private int direction = 1; // 1 = phải, -1 = trái
    private bool isAttacking = false;
    private bool isHurt = false;
    private float cooldownTimer = 0f;
    private float hurtTimer = 0f;
    private float attackTimeoutTimer = 0f;

    // Giới hạn tuần tra quanh điểm xuất phát
    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;

    public EnemyState CurrentState => currentState;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // Ghi nhận điểm đứng ban đầu làm tâm tuần tra
        spawnX = transform.position.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
    }

    private void Update()
    {
        if (isDead) return;

        // Giảm thời gian hồi chiêu
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Xử lý trạng thái bị thương giật lùi (Stun)
        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f)
                isHurt = false;
            return;
        }

        // Tự động tìm lại Player nếu bị mất dấu
        if (player == null)
        {
            FindPlayer();
        }

        // Xử lý an toàn: nếu đang chém quá 1.6s thì tự mở khóa tránh bị đứng im
        if (isAttacking)
        {
            attackTimeoutTimer -= Time.deltaTime;
            if (attackTimeoutTimer <= 0f)
                isAttacking = false;
            return;
        }

        float playerFeetY = player != null ? player.position.y : 0f;
        if (player != null)
        {
            var pCol = player.GetComponent<Collider2D>();
            if (pCol != null) playerFeetY = pCol.bounds.min.y;
        }

        float monsterFeetY = myCollider != null ? myCollider.bounds.min.y : transform.position.y;

        // Khoảng cách theo phương ngang và phương đứng (so sánh chân chạm đất, không bị lệch bởi scale hay tâm pivot)
        float horizontalDist = player != null ? Mathf.Abs(transform.position.x - player.position.x) : float.MaxValue;
        float verticalDist = player != null ? Mathf.Abs(playerFeetY - monsterFeetY) : float.MaxValue;

        // Cùng tầng đất nếu chênh lệch mặt chân < 2.0m (chấp nhận bậc thang nhỏ)
        bool isSameHeight = player != null && verticalDist < 2.0f;

        // ==========================================
        // MÁY TRẠNG THÁI (STATE MACHINE) CỦA QUÁI
        // ==========================================
        switch (currentState)
        {
            // --- TRẠNG THÁI 1: TUẦN TRA BÌNH THƯỜNG [1 -> 9] ---
            case EnemyState.Patrol:
                // Nếu Hiệp sĩ bước vào phạm vi thù địch (<= aggroRange) -> KÍCH HOẠT ĐUỔI THEO!
                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = EnemyState.Chase;
                }
                else
                {
                    Patrol();
                }
                break;

            // --- TRẠNG THÁI 2: THÙ ĐỊCH & TRUY ĐUỔI (BỎ GIỚI HẠN TUẦN TRA) ---
            case EnemyState.Chase:
                // Nếu Hiệp sĩ chạy quá xa (> deAggroRange) hoặc khác tầng đất -> BỎ THÙ ĐỊCH, QUAY VỀ LÃNH THỔ!
                if (horizontalDist > deAggroRange || !isSameHeight)
                {
                    currentState = EnemyState.Returning;
                    break;
                }

                // Nếu Hiệp sĩ đã nằm trong tầm chém -> Dừng lại tung combo!
                if (horizontalDist <= attackRange)
                {
                    FaceTarget(player.position.x);
                    StopMoving();

                    if (cooldownTimer <= 0f)
                    {
                        StartComboAttack();
                    }
                }
                // Nếu chưa tới tầm chém -> Lao tới áp sát Hiệp sĩ
                else
                {
                    FaceTarget(player.position.x);

                    // An toàn: Kiểm tra trước chân, không đuổi lao đầu xuống vực
                    Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
                    RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

                    if (groundHit.collider)
                    {
                        MoveForward(chaseSpeed);
                    }
                    else
                    {
                        StopMoving(); // Dừng ngay mép vực, không nhảy lầu
                    }
                }
                break;

            // --- TRẠNG THÁI 3: BỎ THÙ ĐỊCH & QUAY VỀ LÃNH THỔ [1 -> 9] ---
            case EnemyState.Returning:
                // Nếu trên đường về mà Hiệp sĩ lại mon men tới gần -> Tiếp tục đuổi đánh!
                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = EnemyState.Chase;
                    break;
                }

                // Đi bộ về phía điểm xuất phát ban đầu (spawnX)
                float distToSpawn = Mathf.Abs(transform.position.x - spawnX);
                if (distToSpawn > 0.5f)
                {
                    FaceTarget(spawnX);
                    MoveForward(moveSpeed);
                }
                else
                {
                    // Đã về đến nhà -> Quay lại tuần tra bình thường!
                    currentState = EnemyState.Patrol;
                }
                break;
        }
    }

    private void Patrol()
    {
        // 0. Giới hạn phạm vi tuần tra quanh vị trí ban đầu (ví dụ: 1 -> 5 -> 9)
        if (transform.position.x >= maxPatrolX && direction > 0)
        {
            Flip();
        }
        else if (transform.position.x <= minPatrolX && direction < 0)
        {
            Flip();
        }

        // 1. Raycast dò mép vực: từ vị trí phía trước chân bắn 1 tia xuống
        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

        // 2. Raycast dò tường: từ ngực quái bắn 1 tia ngang về phía trước
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.5f);
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, new Vector2(direction, 0f), wallCheckDistance, groundLayer);

        // Nếu phía trước hụt chân (vực) hoặc đụng tường -> Tự động quay đầu!
        if (!groundHit.collider || wallHit.collider)
        {
            Flip();
        }

        MoveForward(moveSpeed);
    }

    private void MoveForward(float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (anim != null) anim.SetBool("IsMoving", true);
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (anim != null) anim.SetBool("IsMoving", false);
    }

    private void Flip()
    {
        direction = -direction;
        UpdateFacing();
    }

    private void FaceTarget(float targetX)
    {
        float diff = targetX - transform.position.x;
        // Khoảng chết 0.15m để tránh quái đứng sát người Hiệp sĩ bị xoay qua xoay lại liên tục
        if (Mathf.Abs(diff) > 0.15f)
        {
            if (diff > 0 && direction < 0)
            {
                direction = 1;
                UpdateFacing();
            }
            else if (diff < 0 && direction > 0)
            {
                direction = -1;
                UpdateFacing();
            }
        }
    }

    private void UpdateFacing()
    {
        // Sprite gốc nhìn phải (flipX = false). Đi trái thì flipX = true.
        if (sr != null)
            sr.flipX = direction < 0;
    }

    private void StartComboAttack()
    {
        isAttacking = true;
        cooldownTimer = attackCooldown;
        attackTimeoutTimer = 1.6f; // Thời gian chờ trọn vẹn 2 đòn chém
        StopMoving();

        // Kích hoạt Trigger: Animator sẽ tự chém Attack 1 rồi nối tiếp sang Attack 2
        if (anim != null)
            anim.SetTrigger("Attack");
    }

    // Gây sát thương lên Hiệp sĩ (gọi qua Animation Event ở Attack1 và Attack2)
    public void DealAttackDamage()
    {
        if (isDead) return;

        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * 1.2f, 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, 1.0f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.GetComponentInParent<KnightController>() != null || hit.GetComponentInParent<PlayerHealth>() != null)
            {
                // Sát thương cho Knight
                var knight = hit.GetComponentInParent<KnightController>();
                if (knight != null) knight.TakeDamage(attackDamage);

                // Sát thương cho Mage
                var mage = hit.GetComponentInParent<PlayerHealth>();
                if (mage != null) mage.TakeDamage(attackDamage);
            }
        }
    }

    // Kết thúc đòn đánh combo (gọi ở frame cuối của Attack 2)
    public void FinishAttack()
    {
        isAttacking = false;
    }

    // Khi Hiệp sĩ chém trúng quái
    public override void TakeDamage(int damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);

        // Bị đánh lén từ sau lưng -> Lập tức quay mặt và chuyển sang thù địch đuổi đánh!
        if (player != null)
        {
            currentState = EnemyState.Chase;
            FaceTarget(player.position.x);
        }

        if (!isDead)
        {
            isHurt = true;
            hurtTimer = hurtDuration;
            isAttacking = false;

            // Giật lùi nhẹ (Knockback) ngược hướng nhìn
            rb.linearVelocity = new Vector2(-direction * knockbackForce, 2f);
        }
        else
        {
            // Tắt va chạm để Hiệp sĩ bước qua xác, đứng yên ngã gục
            if (myCollider != null) myCollider.enabled = false;

            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            Destroy(gameObject, 1.5f);
        }
    }

    private void FindPlayer()
    {
        // 1. Ưu tiên tìm Hiệp sĩ qua KnightController
        var knight = FindAnyObjectByType<KnightController>();
        if (knight != null)
        {
            player = knight.transform;
            return;
        }

        // 2. Tìm Pháp sư qua PlayerHealth
        var mage = FindAnyObjectByType<PlayerHealth>();
        if (mage != null)
        {
            player = mage.transform;
            return;
        }

        // 3. Dự phòng tìm qua Tag Player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform.root;
        }
    }

    // Vẽ vùng cảm biến trực quan trong Scene
    private void OnDrawGizmosSelected()
    {
        // 1. Vẽ phạm vi tuần tra ban đầu (Màu xanh lá cây)
        Gizmos.color = Color.green;
        float currentSpawnX = Application.isPlaying ? spawnX : transform.position.x;
        Vector3 leftBound = new Vector3(currentSpawnX - patrolRadius, transform.position.y, 0f);
        Vector3 rightBound = new Vector3(currentSpawnX + patrolRadius, transform.position.y, 0f);
        Gizmos.DrawLine(leftBound, rightBound);
        Gizmos.DrawWireCube(leftBound, new Vector3(0.2f, 1f, 0f));
        Gizmos.DrawWireCube(rightBound, new Vector3(0.2f, 1f, 0f));

        // 2. Tầm thù địch Aggro Range (Vòng tròn vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        // 3. Tầm bỏ thù địch De-Aggro Range (Vòng tròn cam)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, deAggroRange);

        // 4. Tầm chém kiếm (Vòng tròn đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 5. Tia dò mép sàn (Màu xanh lơ)
        Gizmos.color = Color.cyan;
        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
        Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector2.down * groundCheckDepth);

        // 6. Tia dò bức tường (Màu xanh dương)
        Gizmos.color = Color.blue;
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.5f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector2(direction * wallCheckDistance, 0f));
    }
}
