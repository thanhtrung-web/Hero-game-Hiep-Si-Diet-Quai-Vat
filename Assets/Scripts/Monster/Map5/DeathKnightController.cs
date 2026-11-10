using UnityEngine;

/// <summary>
/// Điều khiển quái vật Kỵ Sĩ Hắc Ám (Death Knight) trong Map 5 Phase 2.
/// Kế thừa từ <see cref="EnemyHealth"/> để đồng bộ hệ thống máu, số sát thương và sự kiện chiến đấu.
/// Sở hữu trí tuệ nhân tạo (FSM) gồm: Tuần tra thông minh (chống rơi vực), Thù địch truy đuổi,
/// Combo đòn chém kép (Slash Arc / Ground Slam), và Lướt bóng đêm né phản công (Shadow Dash Escape).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class DeathKnightController : EnemyHealth
{
    /// <summary>
    /// Các trạng thái trong máy trạng thái (FSM) của Kỵ Sĩ Hắc Ám.
    /// </summary>
    public enum KnightState
    {
        Spawn,      // Trỗi dậy từ hư không / hố năng lượng
        Patrol,     // Tuần tra chậm rãi, đại kiếm vác trên vai trong phạm vi cho phép
        Chase,      // Bỏ giới hạn tuần tra, tăng tốc áp sát Hiệp sĩ
        Attack1,    // Chém kiếm ngang nhanh (Slash Arc)
        Attack2,    // Bổ kiếm xuống đất kích nổ gai ma pháp (Overhead Slam)
        Escape,     // Lướt bóng đêm lùi lại né phản công khi đối thủ ở quá gần (Shadow Dash)
        Returning   // Mất dấu hoặc người chơi chạy xa, quay về vị trí xuất phát
    }

    [Header("1. Movement & Patrol (Di chuyển & Tuần tra)")]
    [Tooltip("Tốc độ di chuyển khi tuần tra")]
    [SerializeField] private float moveSpeed = 1.8f;

    [Tooltip("Tốc độ lao tới khi truy đuổi người chơi")]
    [SerializeField] private float chaseSpeed = 3.0f;

    [Tooltip("Layer mặt đất dùng để dò sàn và tường (Ground Layer)")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Khoảng cách phía trước để bắn tia dò mép sàn/vực")]
    [SerializeField] private float ledgeCheckDistance = 0.8f;

    [Tooltip("Độ sâu của tia dò mặt sàn đất")]
    [SerializeField] private float groundCheckDepth = 1.0f;

    [Tooltip("Khoảng cách tia dò bức tường phía trước")]
    [SerializeField] private float wallCheckDistance = 0.6f;

    [Tooltip("Bán kính tuần tra tối đa tính từ điểm xuất phát")]
    [SerializeField] private float patrolRadius = 4f;

    [Header("2. Combat Settings (Chiến đấu & Sát thương)")]
    [Tooltip("Tầm phát hiện để kích hoạt thù địch đuổi theo người chơi")]
    [SerializeField] private float aggroRange = 5.0f;

    [Tooltip("Khoảng cách người chơi chạy thoát khiến quái bỏ thù địch quay về")]
    [SerializeField] private float deAggroRange = 7.0f;

    [Tooltip("Tầm đánh của đòn chém kiếm ngang Attack 1")]
    [SerializeField] private float attack1Range = 2.0f;

    [Tooltip("Tầm đánh của đòn bổ kiếm xuống đất Attack 2")]
    [SerializeField] private float attack2Range = 2.4f;

    [Tooltip("Thời gian hồi giữa các lần ra đòn (giây)")]
    [SerializeField] private float attackCooldown = 1.8f;

    [Tooltip("Sát thương đòn chém kiếm ngang Attack 1")]
    [SerializeField] private int attack1Damage = 18;

    [Tooltip("Sát thương đòn bổ kiếm giáng đất Attack 2")]
    [SerializeField] private int attack2Damage = 28;

    [Header("3. Hurt & Defense (Phản hồi chấn thương & Né đòn)")]
    [Tooltip("Lực giật lùi khi bị người chơi chém trúng")]
    [SerializeField] private float knockbackForce = 2f;

    [Tooltip("Thời gian khựng choáng khi nhận sát thương")]
    [SerializeField] private float hurtDuration = 0.25f;

    [Tooltip("Thời gian hồi chiêu của kỹ năng lướt bóng đêm Escape (giây)")]
    [SerializeField] private float escapeCooldown = 5.0f;

    [Header("4. Audio (Âm thanh chiến đấu)")]
    [Tooltip("AudioSource phát âm thanh")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Clip âm thanh khi vung kiếm tấn công")]
    [SerializeField] private AudioClip attackSound;

    [Tooltip("Mixer Group dành cho hiệu ứng âm thanh SFX")]
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;

    // Các thành phần nội bộ (Internal Components)
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D myCollider;
    private Transform player;

    // Biến trạng thái máy FSM
    private KnightState currentState = KnightState.Patrol;
    private int direction = 1; // 1 = nhìn phải, -1 = nhìn trái
    private bool isAttacking = false;
    private bool isHurt = false;
    private float cooldownTimer = 0f;
    private float hurtTimer = 0f;
    private float escapeCooldownTimer = 0f;
    private float attackTimeoutTimer = 0f;
    private int comboCounter = 0;

    // Giới hạn vùng tuần tra
    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;

    /// <summary>
    /// Trạng thái hiện tại của Kỵ Sĩ Hắc Ám.
    /// </summary>
    public KnightState CurrentState => currentState;

    /// <summary>
    /// Khởi tạo các thành phần vật lý, hình ảnh và hệ thống âm thanh.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();

        // Tự động tìm hoặc bổ sung AudioSource nếu chưa có
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        if (sfxMixerGroup != null)
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    /// <summary>
    /// Thiết lập vùng tuần tra quanh điểm xuất phát và dò tìm Người chơi trong Scene.
    /// </summary>
    private void Start()
    {
        spawnX = transform.position.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
    }

    /// <summary>
    /// Vòng lặp cập nhật AI chính: đếm thời gian hồi chiêu, kiểm tra cao độ sàn và điều phối FSM.
    /// </summary>
    private void Update()
    {
        if (isDead) return;

        // Giảm trừ các bộ đếm hồi chiêu
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (escapeCooldownTimer > 0f) escapeCooldownTimer -= Time.deltaTime;

        // Nếu đang bị khựng choáng do nhận đòn đánh
        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f) isHurt = false;
            return;
        }

        // Tự động tìm lại Người chơi nếu mất tham chiếu
        if (player == null) FindPlayer();

        // Kiểm tra an toàn: tránh tình trạng kẹt trạng thái vung kiếm quá lâu
        if (isAttacking)
        {
            attackTimeoutTimer -= Time.deltaTime;
            if (attackTimeoutTimer <= 0f) isAttacking = false;
            return;
        }

        // Tính toán khoảng cách theo phương ngang và độ cao mặt sàn chân đứng
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

        // Điều phối hành vi theo máy trạng thái (FSM)
        switch (currentState)
        {
            case KnightState.Spawn:
                // Xử lý hoạt ảnh trỗi dậy, sau đó tự chuyển về Patrol
                break;

            case KnightState.Patrol:
                // Người chơi tiến vào phạm vi thù địch cùng tầng đất -> Chuyển sang Truy đuổi
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
                // Người chơi chạy quá xa hoặc khác tầng đất -> Hủy thù địch, quay về điểm xuất phát
                if (horizontalDist > deAggroRange || !isSameHeight)
                {
                    currentState = KnightState.Returning;
                    break;
                }

                // Nếu đã vào tầm đánh -> Dừng chân và tung combo
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
                    // Di chuyển áp sát người chơi, dò mép sàn chống rơi vực
                    FaceTarget(player.position.x);
                    Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
                    RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

                    if (groundHit.collider)
                        MoveForward(chaseSpeed);
                    else
                        StopMoving(); // Dừng ngay mép vực
                }
                break;

            case KnightState.Returning:
                // Di chuyển trở lại vị trí xuất phát ban đầu
                float distToSpawn = Mathf.Abs(transform.position.x - spawnX);
                if (distToSpawn <= 0.3f)
                {
                    currentState = KnightState.Patrol;
                    StopMoving();
                    break;
                }

                FaceTarget(spawnX);
                MoveForward(moveSpeed);

                // Nếu người chơi lại tiếp cận trong lúc đang đi về -> Tiếp tục truy đuổi
                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = KnightState.Chase;
                }
                break;
        }
    }

    /// <summary>
    /// Xử lý logic tuần tra: đổi hướng khi chạm biên [minPatrolX, maxPatrolX], mép vực hoặc tường.
    /// </summary>
    private void Patrol()
    {
        if (transform.position.x <= minPatrolX && direction == -1)
            direction = 1;
        else if (transform.position.x >= maxPatrolX && direction == 1)
            direction = -1;

        // Bắn tia kiểm tra tường phía trước
        Vector2 wallOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

        // Bắn tia kiểm tra mép vực phía trước chân
        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

        if (wallHit.collider || !groundHit.collider)
        {
            direction = -direction;
        }

        UpdateFacing();
        MoveForward(moveSpeed);
    }

    /// <summary>
    /// Di chuyển về phía trước theo hướng hiện tại với tốc độ chỉ định.
    /// </summary>
    /// <param name="speed">Tốc độ di chuyển.</param>
    private void MoveForward(float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    /// <summary>
    /// Dừng hoàn toàn quán tính di chuyển ngang và cập nhật hoạt ảnh.
    /// </summary>
    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null) animator.SetBool("IsMoving", false);
    }

    /// <summary>
    /// Xoay mặt về phía tọa độ X của mục tiêu.
    /// </summary>
    /// <param name="targetX">Tọa độ X cần hướng tới.</param>
    private void FaceTarget(float targetX)
    {
        int newDir = (targetX > transform.position.x) ? 1 : -1;
        if (newDir != direction)
        {
            direction = newDir;
            UpdateFacing();
        }
    }

    /// <summary>
    /// Cập nhật hướng xoay của nhân vật thông qua localScale.
    /// </summary>
    private void UpdateFacing()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }

    /// <summary>
    /// Bắt đầu chuỗi tấn công combo: xen kẽ giữa chém ngang (Attack 1) và bổ kiếm kích nổ đất (Attack 2).
    /// </summary>
    private void StartAttack()
    {
        isAttacking = true;
        StopMoving();
        PlayAttackSound();
        cooldownTimer = attackCooldown;
        attackTimeoutTimer = 1.4f;

        comboCounter++;
        if (comboCounter % 3 == 0)
        {
            // Đòn thứ 3: Bổ kiếm xuống sàn kích nổ gai ma pháp (Overhead ground slam)
            if (animator != null) animator.SetTrigger("Attack2");
        }
        else
        {
            // Đòn thông thường: Chém kiếm ngang nhanh (Slash Arc)
            if (animator != null) animator.SetTrigger("Attack1");
        }
    }

    /// <summary>
    /// Phát âm thanh khi vung kiếm (có thể gọi trực tiếp qua Animation Event).
    /// </summary>
    public void PlayAttackSound()
    {
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    /// <summary>
    /// Animation Event: Gây sát thương của đòn chém kiếm ngang Attack 1.
    /// </summary>
    public void DealSlashDamage()
    {
        DealDamageInArea(attack1Range, attack1Damage);
    }

    /// <summary>
    /// Animation Event: Gây sát thương của đòn bổ kiếm xuống đất Attack 2.
    /// </summary>
    public void DealSlamDamage()
    {
        DealDamageInArea(attack2Range, attack2Damage);
    }

    /// <summary>
    /// Quét vùng hình tròn phía trước mặt quái và gây sát thương lên Người chơi.
    /// </summary>
    /// <param name="range">Tầm với của đòn đánh.</param>
    /// <param name="damage">Lượng sát thương gây ra.</param>
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

    /// <summary>
    /// Animation Event: Gọi ở frame cuối của đòn đánh để kết thúc trạng thái tấn công.
    /// Có tỷ lệ kích hoạt lướt bóng đêm né tránh (Escape) nếu người chơi đứng quá gần.
    /// </summary>
    public void FinishAttack()
    {
        isAttacking = false;

        // Cơ hội lướt bóng đêm né tránh (Escape) nếu người chơi vẫn ở cự ly áp sát nguy hiểm
        if (player != null && escapeCooldownTimer <= 0f && Random.value < 0.4f)
        {
            float dist = Mathf.Abs(transform.position.x - player.position.x);
            if (dist < 1.5f)
            {
                TriggerEscapeDash();
            }
        }
    }

    /// <summary>
    /// Kích hoạt kỹ năng lướt bóng đêm (Shadow Dash) lùi nhanh ra sau né phản công.
    /// </summary>
    private void TriggerEscapeDash()
    {
        escapeCooldownTimer = escapeCooldown;
        if (animator != null) animator.SetTrigger("Escape");
        rb.linearVelocity = new Vector2(-direction * 5f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Xử lý nhận sát thương từ người chơi: giảm máu, nảy số sát thương, khựng choáng và giật lùi nhẹ.
    /// </summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
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

    /// <summary>
    /// Xử lý khi Kỵ Sĩ Hắc Ám bị tiêu diệt: tắt va chạm, dừng vật lý và hủy sau thời gian ngắn.
    /// </summary>
    protected override void Die()
    {
        base.Die();
        StopMoving();
        rb.simulated = false;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.5f);
    }

    /// <summary>
    /// Tìm kiếm đối tượng Người chơi trong Scene theo Tag "Player".
    /// </summary>
    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    /// <summary>
    /// Vẽ các đường biểu diễn vùng nhận biết và tầm đánh trong Editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attack1Range);
    }
}
