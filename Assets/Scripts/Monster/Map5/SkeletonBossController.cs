using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SkeletonBossController : EnemyHealth
{
    public enum BossState
    {
        Patrol,
        Chase,
        Attacking,
        Summoning,
        Returning
    }

    [Header("1. Movement & Patrol")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float chaseSpeed = 3.6f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeCheckDistance = 1.0f;
    [SerializeField] private float groundCheckDepth = 1.2f;
    [SerializeField] private float wallCheckDistance = 0.8f;
    [SerializeField] private float patrolRadius = 6f;

    [Header("2. Aggro & Attack")]
    [SerializeField] private float aggroRange = 8.5f;
    [SerializeField] private float deAggroRange = 12f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 2.2f;
    [SerializeField] private int normalAttackDamage = 25;
    [SerializeField] private int heavyAttackDamage = 40;

    [Header("3. Summon Skill")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private float summonCooldown = 16f;
    [SerializeField] private int maxMinions = 2;
    [SerializeField] private float summonHpThreshold = 0.75f; // Bắt đầu triệu hồi khi máu dưới 75%

    [Header("4. Hurt Resistance (Kháng Choáng)")]
    [SerializeField] private float hurtCooldown = 1.5f; // Không bị stun-lock liên tục

    [Header("5. Victory & Exit Portal")]
    [Tooltip("Cổng dịch chuyển qua Map 5 Phase 2. Nếu để trống, boss sẽ tự tìm ScenePortal trong scene.")]
    [SerializeField] private GameObject exitPortal;
    [SerializeField] private float portalAppearDelay = 1.5f;
    [Tooltip("Vị trí lệch của cổng so với Boss (mặc định xuất hiện ngay kế cạnh Boss)")]
    [SerializeField] private Vector3 portalOffsetFromBoss = new Vector3(1.0f, 0.2f, 0f);

    // Components nội bộ
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim => animator;
    private Collider2D myCollider;
    private Transform player;

    // Trạng thái Boss
    private BossState currentState = BossState.Patrol;
    private int direction = -1; // Mặc định quay mặt sang trái (hướng về phía người chơi)
    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;

    private float nextAttackTime = 0f;
    private float nextSummonTime = 0f;
    private float lastHurtTime = -10f;

    private readonly List<GameObject> activeMinions = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (groundLayer.value == 0)
        {
            int mask = LayerMask.GetMask("Ground");
            groundLayer = (mask != 0) ? mask : (1 << 3);
        }
    }

    private void Reset()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (groundLayer.value == 0)
        {
            int mask = LayerMask.GetMask("Ground");
            groundLayer = (mask != 0) ? mask : (1 << 3);
        }
    }
#endif

    private void Start()
    {
        spawnX = transform.position.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
        UpdateFacing();

        // Mặc định Layer ground nếu chưa gán
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
            if (groundLayer == 0) groundLayer = 1 << 3;
        }

        nextSummonTime = Time.time + 6f; // Chờ 6s sau khi bắt đầu trận mới summon đợt 1
    }

    private void Update()
    {
        if (isDead) return;

        // Dọn dẹp danh sách đệ tử đã chết
        activeMinions.RemoveAll(m => m == null);

        if (currentState == BossState.Attacking || currentState == BossState.Summoning)
            return;

        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                Patrol();
                return;
            }
        }

        // Tính khoảng cách
        float horizontalDist = Mathf.Abs(transform.position.x - player.position.x);
        Collider2D playerCol = player.GetComponent<Collider2D>();
        bool isSameHeight = true;
        if (playerCol != null && myCollider != null)
        {
            float playerFeetY = playerCol.bounds.min.y;
            float bossFeetY = myCollider.bounds.min.y;
            isSameHeight = Mathf.Abs(playerFeetY - bossFeetY) < 3.0f;
        }

        switch (currentState)
        {
            case BossState.Patrol:
                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = BossState.Chase;
                    break;
                }
                Patrol();
                break;

            case BossState.Chase:
                if (horizontalDist > deAggroRange || !isSameHeight)
                {
                    currentState = BossState.Returning;
                    break;
                }

                FaceTarget(player.position.x);

                // Ưu tiên 1: Kỹ năng Summon nếu đủ điều kiện
                float hpPercent = (float)currentHealth / maxHealth;
                if (hpPercent <= summonHpThreshold && Time.time >= nextSummonTime && activeMinions.Count < maxMinions && minionPrefab != null)
                {
                    StartCoroutine(SummonRoutine());
                    return;
                }

                // Ưu tiên 2: Tấn công nếu trong tầm đánh
                if (horizontalDist <= attackRange && Time.time >= nextAttackTime)
                {
                    StartCoroutine(AttackRoutine());
                    return;
                }

                // Di chuyển áp sát người chơi
                Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.2f);
                RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

                if (groundHit.collider)
                {
                    MoveForward(chaseSpeed);
                }
                else
                {
                    StopMoving();
                }
                break;

            case BossState.Returning:
                if (horizontalDist <= aggroRange && isSameHeight)
                {
                    currentState = BossState.Chase;
                    break;
                }

                float distToSpawn = Mathf.Abs(transform.position.x - spawnX);
                if (distToSpawn > 0.6f)
                {
                    FaceTarget(spawnX);
                    MoveForward(moveSpeed);
                }
                else
                {
                    currentState = BossState.Patrol;
                }
                break;
        }
    }

    private void Patrol()
    {
        if (transform.position.x >= maxPatrolX && direction > 0)
        {
            Flip();
        }
        else if (transform.position.x <= minPatrolX && direction < 0)
        {
            Flip();
        }

        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.2f);
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.8f);
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

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
        if (Mathf.Abs(diff) > 0.2f)
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
        // Sprite gốc nhìn sang phải. Đi trái -> flipX = true.
        if (sr != null)
            sr.flipX = direction < 0;
    }

    private IEnumerator AttackRoutine()
    {
        currentState = BossState.Attacking;
        StopMoving();

        // 35% tỉ lệ tung chiêu Heavy Attack (Attack 3), 65% tung combo Attack 1 + 2
        bool isHeavy = Random.value < 0.35f;

        if (isHeavy)
        {
            if (anim != null) anim.SetTrigger("Attack3");
            yield return new WaitForSeconds(0.45f); // Chờ vung búa xuống

            CheckAndDamagePlayer(heavyAttackDamage, attackRange + 1.2f);
            yield return new WaitForSeconds(0.65f); // Hoàn tất hoạt ảnh
            nextAttackTime = Time.time + attackCooldown + 0.5f;
        }
        else
        {
            if (anim != null) anim.SetTrigger("Attack");
            yield return new WaitForSeconds(0.35f); // Nhát chém 1
            CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);

            yield return new WaitForSeconds(0.35f); // Nhát chém 2
            CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);

            yield return new WaitForSeconds(0.4f);
            nextAttackTime = Time.time + attackCooldown;
        }

        currentState = BossState.Chase;
    }

    private IEnumerator SummonRoutine()
    {
        currentState = BossState.Summoning;
        StopMoving();

        if (anim != null) anim.SetTrigger("Summon");
        yield return new WaitForSeconds(0.5f); // Đoạn giơ kiếm gọi đệ

        // Triệu hồi 1 hoặc 2 Skeleton_White
        int spawnCount = Mathf.Min(2, maxMinions - activeMinions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            float offsetX = (i == 0) ? -2.0f * direction : 2.0f * direction;
            Vector3 spawnPos = transform.position + new Vector3(offsetX, 0f, 0f);

            if (minionPrefab != null)
            {
                GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
                activeMinions.Add(minion);
            }
        }

        yield return new WaitForSeconds(0.6f);
        nextSummonTime = Time.time + summonCooldown;
        currentState = BossState.Chase;
    }

    // Hỗ trợ gọi sát thương qua Animation Event nếu gắn vào Clip
    public void DealNormalDamage()
    {
        CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);
    }

    public void DealHeavyDamage()
    {
        CheckAndDamagePlayer(heavyAttackDamage, attackRange + 1.2f);
    }

    private void CheckAndDamagePlayer(int dmg, float range)
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        float diffX = (player.position.x - transform.position.x) * direction;

        // Vùng quét chém hình tròn phía trước mặt Boss
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * (range * 0.5f), 0.7f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, range * 0.75f);

        bool dealt = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.CompareTag("Player") || hit.GetComponentInParent<KnightController>() != null || hit.GetComponentInParent<PlayerHealth>() != null)
            {
                dealt = ApplyDamageToTarget(hit.gameObject, dmg);
                if (dealt) break;
            }
        }

        // Fallback kiểm tra khoảng cách nếu hai bên đứng sát hoặc va chạm lướt qua nhau
        if (!dealt && dist <= range && diffX >= -0.5f)
        {
            ApplyDamageToTarget(player.gameObject, dmg);
        }
    }

    private bool ApplyDamageToTarget(GameObject target, int dmg)
    {
        // 1. Gây sát thương lên Hiệp Sĩ (Knight)
        var knight = target.GetComponentInParent<KnightController>();
        if (knight != null)
        {
            knight.TakeDamage(dmg);
            Debug.Log($"⚔️ [Skeleton_Boss] Đánh trúng Hiệp Sĩ gây {dmg} sát thương!");
            return true;
        }

        // 2. Gây sát thương lên Pháp Sư (Mage)
        var mage = target.GetComponentInParent<PlayerHealth>();
        if (mage != null)
        {
            mage.TakeDamage(dmg);
            Debug.Log($"⚔️ [Skeleton_Boss] Đánh trúng Pháp Sư gây {dmg} sát thương!");
            return true;
        }

        return false;
    }

    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        // Kháng choáng: Chỉ giật Hurt nếu qua thời gian hồi để tránh bị stun-lock
        if (Time.time >= lastHurtTime + hurtCooldown)
        {
            lastHurtTime = Time.time;
            if (anim != null) anim.SetTrigger("Hurt");
            StartCoroutine(FlashRedEffect());
        }

        // Chuyển sang rượt đuổi ngay nếu bị đánh từ xa
        if (currentState == BossState.Patrol || currentState == BossState.Returning)
        {
            currentState = BossState.Chase;
        }
    }

    private IEnumerator FlashRedEffect()
    {
        if (sr != null)
        {
            Color orig = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            sr.color = orig;
        }
    }

    protected override void Die()
    {
        base.Die();

        StopMoving();

        // Vô hiệu hóa va chạm và trọng lực để nằm chết đẹp mắt
        if (myCollider != null) myCollider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Debug.Log("👑💀 [Skeleton_Boss] ĐÃ BỊ TIÊU DIỆT! CỔNG DỊCH CHUYỂN ĐANG MỞ...");
        StartCoroutine(OpenExitPortalRoutine());
        Destroy(gameObject, 3.5f);
    }

    private IEnumerator OpenExitPortalRoutine()
    {
        yield return new WaitForSeconds(portalAppearDelay);

        GameObject portalObj = exitPortal;

        // Nếu chưa gán trong Inspector, tự tìm kiếm trong Scene (kể cả object đang bị ẩn / inactive)
        if (portalObj == null)
        {
            var portals = FindObjectsByType<ScenePortal>(FindObjectsInactive.Include);
            if (portals != null && portals.Length > 0)
            {
                portalObj = portals[0].gameObject;
            }
            else
            {
                // Fallback theo tên
                portalObj = GameObject.Find("Map5_ExitPortal") ?? GameObject.Find("ExitPortal") ?? GameObject.Find("Portal");
            }
        }

        if (portalObj != null)
        {
            // Luôn đặt cổng xuất hiện ngay cạnh Boss (nơi Boss vừa bị hạ gục)
            portalObj.transform.position = transform.position + portalOffsetFromBoss;

            portalObj.SetActive(true);

            var srPortal = portalObj.GetComponent<SpriteRenderer>();
            if (srPortal != null) srPortal.enabled = true;

            var colPortal = portalObj.GetComponent<Collider2D>();
            if (colPortal != null) colPortal.enabled = true;

            Debug.Log($"🌀 [Skeleton_Boss] CỔNG QUA MAP 5 PHẦN 2 ĐÃ XUẤT HIỆN TẠI VỊ TRÍ KẾ BÊN BOSS: {portalObj.transform.position}!");
        }
        else
        {
            Debug.LogWarning("⚠️ [Skeleton_Boss] Không tìm thấy ExitPortal hoặc ScenePortal nào trong Scene để kích hoạt!");
        }
    }

    private void FindPlayer()
    {
        // 1. Tìm Hiệp Sĩ qua KnightController
        var kc = FindAnyObjectByType<KnightController>();
        if (kc != null)
        {
            player = kc.transform;
            return;
        }

        // 2. Tìm Pháp Sư qua PlayerHealth
        var ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null)
        {
            player = ph.transform;
            return;
        }

        // 3. Fallback tìm theo Tag "Player"
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            var knight = p.GetComponentInParent<KnightController>();
            player = (knight != null) ? knight.transform : p.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * (attackRange * 0.5f), 0.7f);
        Gizmos.DrawWireSphere(hitCenter, attackRange * 0.75f);
    }
}
