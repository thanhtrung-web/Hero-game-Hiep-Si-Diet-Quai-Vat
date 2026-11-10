using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển Trùm Đại Tướng Quân Bộ Xương (Skeleton Warlord) trong Map 5 Phase 1.
/// Kế thừa từ <see cref="EnemyHealth"/> và kích hoạt chế độ Boss (IsBoss = true).
/// Tự động liên kết với thanh máu Boss ở đỉnh màn hình (BossHealthBarUI) và hệ thống Damage Popup.
/// Tích hợp AI chiến đấu chuyên sâu: Tuần tra thông minh, Truy đuổi dũng mãnh, Combo kiếm thuật 3 giai đoạn đa dạng,
/// Triệu hồi lâu la (Skeleton_White) khi máu dưới 75%, Kháng choáng (Super-Armor) khi ra chiêu,
/// và Tự động mở Cổng dịch chuyển qua Map 5 Phase 2 khi bị tiêu diệt.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SkeletonBossController : EnemyHealth
{
    /// <summary>
    /// Đánh dấu đây là Boss để UI hiển thị thanh máu lớn trên đỉnh màn hình.
    /// </summary>
    public override bool IsBoss => true;

    /// <summary>
    /// Danh hiệu hiển thị trên thanh máu Boss.
    /// </summary>
    public override string BossTitle => "Đại Tướng Quân Bộ Xương - Skeleton Warlord";

    /// <summary>
    /// Các trạng thái trong máy trạng thái (FSM) của Boss Đại Tướng Quân.
    /// </summary>
    public enum BossState
    {
        Patrol,     // Tuần tra chậm rãi trong lãnh thổ
        Chase,      // Áp sát Hiệp sĩ
        Attacking,  // Đang thi triển chuỗi combo tấn công
        Summoning,  // Đang niệm chú triệu hồi quân tiếp viện
        Returning   // Quay về vị trí tuần tra khi người chơi chạy xa
    }

    [Header("1. Movement & Patrol (Di chuyển & Tuần tra)")]
    [Tooltip("Tốc độ bước đi khi tuần tra")]
    [SerializeField] private float moveSpeed = 2.2f;

    [Tooltip("Tốc độ khi truy đuổi người chơi")]
    [SerializeField] private float chaseSpeed = 3.6f;

    [Tooltip("Layer mặt đất dùng để dò sàn và tường (Ground Layer)")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Khoảng cách phía trước bàn chân để bắn tia dò mép sàn/vực")]
    [SerializeField] private float ledgeCheckDistance = 1.0f;

    [Tooltip("Độ sâu của tia dò mặt đất")]
    [SerializeField] private float groundCheckDepth = 1.2f;

    [Tooltip("Khoảng cách tia dò bức tường phía trước")]
    [SerializeField] private float wallCheckDistance = 0.8f;

    [Tooltip("Bán kính tuần tra tối đa tính từ điểm xuất phát")]
    [SerializeField] private float patrolRadius = 6f;

    [Header("2. Aggro & Attack (Phạm vi thù địch & Đòn đánh)")]
    [Tooltip("Khoảng cách phát hiện để chuyển sang trạng thái rượt đuổi")]
    [SerializeField] private float aggroRange = 8.5f;

    [Tooltip("Khoảng cách người chơi chạy thoát khiến Boss ngừng đuổi và quay về")]
    [SerializeField] private float deAggroRange = 12f;

    [Tooltip("Tầm kiếm để bắt đầu vung đòn")]
    [SerializeField] private float attackRange = 2.5f;

    [Tooltip("Thời gian hồi giữa các đợt combo kiếm thuật (giây)")]
    [SerializeField] private float attackCooldown = 2.2f;

    [Tooltip("Sát thương mỗi nhát chém thông thường (Attack 1 & 2)")]
    [SerializeField] private int normalAttackDamage = 25;

    [Tooltip("Sát thương đòn trọng kích giáng đất (Attack 3)")]
    [SerializeField] private int heavyAttackDamage = 40;

    [Header("3. Summon Skill (Kỹ năng triệu hồi lâu la)")]
    [Tooltip("Prefab của lính xương triệu hồi (Skeleton_White)")]
    [SerializeField] private GameObject minionPrefab;

    [Tooltip("Thời gian hồi chiêu của kỹ năng triệu hồi (giây)")]
    [SerializeField] private float summonCooldown = 16f;

    [Tooltip("Số lượng lâu la tối đa được phép tồn tại cùng lúc")]
    [SerializeField] private int maxMinions = 2;

    [Tooltip("Ngưỡng máu kích hoạt kỹ năng triệu hồi (mặc định dưới 75% HP)")]
    [SerializeField] private float summonHpThreshold = 0.75f;

    [Header("4. Hurt Resistance (Kháng Choáng - Super-Armor)")]
    [Tooltip("Thời gian giãn cách tối thiểu giữa các lần bị giật khựng Hurt (chống bị stun-lock)")]
    [SerializeField] private float hurtCooldown = 1.5f;

    [Header("5. Victory & Exit Portal (Cổng dịch chuyển qua Phase 2)")]
    [Tooltip("Cổng dịch chuyển qua Map 5 Phase 2. Nếu để trống, boss sẽ tự tìm ScenePortal trong scene.")]
    [SerializeField] private GameObject exitPortal;

    [Tooltip("Thời gian trễ (giây) sau khi Boss ngã xuống trước khi cổng kích hoạt")]
    [SerializeField] private float portalAppearDelay = 1.5f;

    [Tooltip("Vị trí lệch của cổng so với Boss (dự phòng khi cổng chưa có vị trí cố định)")]
    [SerializeField] private Vector3 portalOffsetFromBoss = new Vector3(1.0f, 0.2f, 0f);

    [Header("6. Audio (Hệ thống âm thanh SFX)")]
    [Tooltip("AudioSource phát âm thanh")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Clip âm thanh chung dự phòng")]
    [SerializeField] private AudioClip attackSound;

    [Tooltip("Clip âm thanh nhát chém ngang Attack 1")]
    [SerializeField] private AudioClip attack1Sound;

    [Tooltip("Clip âm thanh nhát chém vát lên Attack 2")]
    [SerializeField] private AudioClip attack2Sound;

    [Tooltip("Clip âm thanh đòn trọng kích giáng kiếm Attack 3")]
    [SerializeField] private AudioClip attack3Sound;

    [Tooltip("Mixer Group dành cho hiệu ứng âm thanh SFX")]
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;

    // Components nội bộ (Internal Components)
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim => animator;
    private Collider2D myCollider;
    private Transform player;

    // Biến trạng thái Boss
    private BossState currentState = BossState.Patrol;
    private int direction = -1; // Mặc định quay mặt sang trái (hướng về phía người chơi)
    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;

    private float nextAttackTime = 0f;
    private float nextSummonTime = 0f;
    private float lastHurtTime = -10f;

    // Quản lý danh sách lâu la còn sống
    private readonly List<GameObject> activeMinions = new List<GameObject>();

    /// <summary>
    /// Khởi tạo các thành phần Rigidbody, Animator, Collider và AudioSource.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();

        // Tự động kết nối hoặc thêm mới AudioSource
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

#if UNITY_EDITOR
    /// <summary>
    /// Tự động cập nhật tham chiếu trong Unity Editor khi chỉnh sửa Inspector.
    /// </summary>
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

    /// <summary>
    /// Đặt lại giá trị mặc định trong Unity Editor.
    /// </summary>
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

    /// <summary>
    /// Thiết lập vùng tuần tra, tìm Người chơi và chuẩn bị đợt triệu hồi đầu tiên.
    /// </summary>
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

        // Tự động kết nối đệ tử Skeleton_White nếu bị thiếu hoặc mất liên kết
        if (minionPrefab == null)
        {
#if UNITY_EDITOR
            minionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Map5/Skeleton_White.prefab");
#endif
        }

        nextSummonTime = Time.time + 6f; // Chờ 6s sau khi bắt đầu trận mới summon đợt 1
    }

    /// <summary>
    /// Vòng lặp cập nhật AI chính của Boss: kiểm tra cự ly, ưu tiên kỹ năng triệu hồi và combo tấn công.
    /// </summary>
    private void Update()
    {
        if (isDead) return;

        // Dọn dẹp danh sách đệ tử đã chết
        activeMinions.RemoveAll(m => m == null);

        // Đang tung chiêu tấn công hoặc niệm phép triệu hồi thì bỏ qua logic di chuyển
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

        // Tính khoảng cách ngang và kiểm tra cao độ mặt sàn
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

                // Ưu tiên 1: Kỹ năng Triệu hồi (Summon) nếu máu dưới 75%, hết hồi chiêu và chưa quá số đệ tối đa
                float hpPercent = (float)currentHealth / maxHealth;
                if (hpPercent <= summonHpThreshold && Time.time >= nextSummonTime && activeMinions.Count < maxMinions && minionPrefab != null)
                {
                    StartCoroutine(SummonRoutine());
                    return;
                }

                // Ưu tiên 2: Tấn công nếu trong tầm đánh -> Dừng chân và vung kiếm (hoặc đứng thủ chờ hồi đòn)
                if (horizontalDist <= attackRange)
                {
                    StopMoving();

                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(AttackRoutine());
                    }
                    return;
                }

                // Di chuyển áp sát người chơi nếu ngoài tầm đánh (kiểm tra mép sàn chống rớt vực)
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

    /// <summary>
    /// Xử lý logic tuần tra qua lại trong khu vực chỉ định.
    /// </summary>
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

    /// <summary>
    /// Di chuyển theo hướng hiện tại với tốc độ chỉ định.
    /// </summary>
    /// <param name="speed">Tốc độ di chuyển.</param>
    private void MoveForward(float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (anim != null) anim.SetBool("IsMoving", true);
    }

    /// <summary>
    /// Dừng quán tính di chuyển và chuyển sang trạng thái đứng yên.
    /// </summary>
    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (anim != null) anim.SetBool("IsMoving", false);
    }

    /// <summary>
    /// Đảo chiều di chuyển của Boss.
    /// </summary>
    private void Flip()
    {
        direction = -direction;
        UpdateFacing();
    }

    /// <summary>
    /// Xoay mặt về phía người chơi với khoảng đệm chống giật 0.35m.
    /// </summary>
    /// <param name="targetX">Tọa độ X cần nhìn về.</param>
    private void FaceTarget(float targetX)
    {
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) > 0.35f)
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

    /// <summary>
    /// Cập nhật hướng xoay của Sprite (Sprite gốc hướng sang phải).
    /// </summary>
    private void UpdateFacing()
    {
        if (sr != null)
            sr.flipX = direction < 0;
    }

    /// <summary>
    /// Coroutine điều phối chuỗi tấn công combo ngẫu nhiên với 3 phong cách đa dạng:
    /// 1. Trọng kích giáng kiếm đơn (Attack 3): 25%
    /// 2. Combo 2 đòn liên hoàn (Attack 1 -> Attack 2): 40%
    /// 3. Đại combo 3 đòn liên tiếp hủy diệt (Attack 1 -> 2 -> 3): 35%
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        currentState = BossState.Attacking;
        StopMoving();
        if (player != null) FaceTarget(player.position.x);

        float roll = Random.value;

        if (roll < 0.25f)
        {
            // === KIỂU 1: TRỌNG KÍCH ĐƠN (ATTACK 3) ===
            if (anim != null) anim.SetTrigger("Attack3");

            // 0s - 0.45s: Giơ kiếm tụ lực lên cao
            yield return new WaitForSeconds(0.45f);
            PlayAttack3Sound();

            // 0.45s - 0.60s: Vung kiếm giáng xuống đất
            yield return new WaitForSeconds(0.15f);
            CheckAndDamagePlayer(heavyAttackDamage, attackRange + 1.2f);

            // 0.60s - 1.10s: Thu kiếm về tư thế thủ (0.50s)
            yield return new WaitForSeconds(0.50f);

            nextAttackTime = Time.time + attackCooldown;
        }
        else if (roll < 0.65f)
        {
            // === KIỂU 2: COMBO 2 ĐÒN (ATTACK 1 -> ATTACK 2) ===
            if (anim != null) anim.SetTrigger("Attack");

            // Đòn 1: Chém ngang (vung kiếm tới tại 0.40s)
            yield return new WaitForSeconds(0.25f);
            PlayAttack1Sound();
            yield return new WaitForSeconds(0.15f);
            CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);

            // Chờ đòn 1 chuyển tiếp sang đòn 2 (tại 0.71s, còn 0.31s)
            yield return new WaitForSeconds(0.31f);

            // Đòn 2: Chém vát ngược lên (sát thương nổ tại 0.35s sau khi vào đòn 2)
            yield return new WaitForSeconds(0.20f);
            PlayAttack2Sound();
            yield return new WaitForSeconds(0.15f);
            CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);

            // Chờ thu kiếm hoàn tất đòn 2 về Idle (0.33s)
            yield return new WaitForSeconds(0.33f);

            nextAttackTime = Time.time + attackCooldown;
        }
        else
        {
            // === KIỂU 3: COMBO 3 ĐÒN LIÊN HOÀN (ATTACK 1 -> 2 -> 3) ===
            if (anim != null) anim.SetTrigger("Attack");

            // Đòn 1: Chém ngang (0.40s)
            yield return new WaitForSeconds(0.25f);
            PlayAttack1Sound();
            yield return new WaitForSeconds(0.15f);
            CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);

            // Chờ đòn 1 sang đòn 2 (0.31s)
            yield return new WaitForSeconds(0.31f);

            // Đòn 2: Chém vát lên (0.35s)
            yield return new WaitForSeconds(0.20f);
            PlayAttack2Sound();
            yield return new WaitForSeconds(0.15f);
            CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);

            // Đang vung lên cao, trigger ngay đòn 3 để tiếp nối nhịp combo mượt mà
            yield return new WaitForSeconds(0.20f);
            if (anim != null) anim.SetTrigger("Attack3");

            // Đòn 3: Giơ cao rồi giáng mạnh xuống đất (0.60s)
            yield return new WaitForSeconds(0.45f);
            PlayAttack3Sound();
            yield return new WaitForSeconds(0.15f);
            CheckAndDamagePlayer(heavyAttackDamage, attackRange + 1.2f);

            // Thu kiếm hoàn tất đòn 3 (0.50s)
            yield return new WaitForSeconds(0.50f);

            nextAttackTime = Time.time + attackCooldown + 0.4f;
        }

        currentState = BossState.Chase;
    }

    /// <summary>
    /// Coroutine thi triển kỹ năng triệu hồi lâu la Skeleton_White hai bên sườn Boss.
    /// </summary>
    private IEnumerator SummonRoutine()
    {
        currentState = BossState.Summoning;
        StopMoving();

        if (anim != null) anim.SetTrigger("Summon");

        // Đợi Boss giơ cao kiếm tụ năng lượng (thời điểm 0.8s)
        yield return new WaitForSeconds(0.8f);

        // Triệu hồi 1 hoặc 2 Skeleton_White
        int spawnCount = Mathf.Min(2, maxMinions - activeMinions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            float offsetX = (i == 0) ? -2.2f * direction : 2.2f * direction;
            Vector3 spawnPos = transform.position + new Vector3(offsetX, 0f, 0f);

            if (minionPrefab != null)
            {
                GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
                activeMinions.Add(minion);
            }
        }

        // Chờ hạ kiếm hoàn tất toàn bộ hoạt ảnh Summon dài 1.6s
        yield return new WaitForSeconds(0.8f);

        nextSummonTime = Time.time + summonCooldown;
        currentState = BossState.Chase;
    }

    /// <summary>
    /// Phát âm thanh đòn chém ngang Attack 1.
    /// </summary>
    public void PlayAttack1Sound()
    {
        AudioClip clip = (attack1Sound != null) ? attack1Sound : attackSound;
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Phát âm thanh đòn chém vát lên Attack 2.
    /// </summary>
    public void PlayAttack2Sound()
    {
        AudioClip clip = (attack2Sound != null) ? attack2Sound : attackSound;
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Phát âm thanh đòn trọng kích giáng kiếm Attack 3.
    /// </summary>
    public void PlayAttack3Sound()
    {
        AudioClip clip = (attack3Sound != null) ? attack3Sound : attackSound;
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Phát âm thanh tấn công dự phòng.
    /// </summary>
    public void PlayAttackSound()
    {
        PlayAttack1Sound();
    }

    /// <summary>
    /// Gây sát thương nhát chém thông thường (có thể gọi qua Animation Event).
    /// </summary>
    public void DealNormalDamage()
    {
        CheckAndDamagePlayer(normalAttackDamage, attackRange + 0.6f);
    }

    /// <summary>
    /// Gây sát thương nhát chém trọng kích (có thể gọi qua Animation Event).
    /// </summary>
    public void DealHeavyDamage()
    {
        CheckAndDamagePlayer(heavyAttackDamage, attackRange + 1.2f);
    }

    /// <summary>
    /// Quét vùng hình tròn phía trước mặt Boss và gây sát thương lên Người chơi nếu nằm trong phạm vi.
    /// </summary>
    /// <param name="dmg">Lượng sát thương gây ra.</param>
    /// <param name="range">Tầm quét kiếm.</param>
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

    /// <summary>
    /// Áp dụng sát thương lên đối tượng Người chơi (hỗ trợ cả Hiệp Sĩ Knight và Pháp Sư Mage).
    /// </summary>
    /// <param name="target">GameObject mục tiêu.</param>
    /// <param name="dmg">Lượng sát thương.</param>
    /// <returns>True nếu đã gây sát thương thành công.</returns>
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

    /// <summary>
    /// Xử lý nhận sát thương từ người chơi: cập nhật thanh máu Boss, nảy số sát thương,
    /// nhấp nháy đỏ và duy trì Super-Armor không bị ngắt chiêu khi đang ra đòn.
    /// </summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        NotifyDamageTaken(damage);
        NotifyHealthChanged(currentHealth, maxHealth);
        SpawnDamagePopup(damage);

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        // Kháng choáng (Super-Armor): Không bị giật Hurt ngắt đòn khi đang vung kiếm hoặc triệu hồi
        if (currentState != BossState.Attacking && currentState != BossState.Summoning)
        {
            if (Time.time >= lastHurtTime + hurtCooldown)
            {
                lastHurtTime = Time.time;
                if (anim != null) anim.SetTrigger("Hurt");
            }
        }

        // Luôn nhấp nháy đỏ báo hiệu nhận sát thương
        StartCoroutine(FlashRedEffect());

        // Chuyển sang rượt đuổi ngay nếu bị đánh từ xa
        if (currentState == BossState.Patrol || currentState == BossState.Returning)
        {
            currentState = BossState.Chase;
        }
    }

    /// <summary>
    /// Hiệu ứng nhấp nháy đỏ sprite khi Boss bị trúng đòn.
    /// </summary>
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

    /// <summary>
    /// Vị trí nơi Boss ngã xuống để cố định vị trí mở Cổng dịch chuyển.
    /// </summary>
    private Vector3 deathPosition;

    /// <summary>
    /// Xử lý khi Boss bị tiêu diệt: tắt va chạm, mở Cổng dịch chuyển qua Map 5 Phase 2 và tự hủy.
    /// Không gọi base.Die() để tránh bị Destroy(1.5s) ngắt đột ngột trước khi hoàn tất hoạt cảnh và coroutine mở cổng.
    /// </summary>
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        deathPosition = transform.position;

        NotifyDeath();
        StopMoving();

        if (anim != null) anim.SetTrigger("Dead");
        if (patrolScript != null) patrolScript.enabled = false;

        // Vô hiệu hóa va chạm và trọng lực để nằm chết đẹp mắt
        if (myCollider != null) myCollider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Debug.Log("👑💀 [Skeleton_Boss] ĐÃ BỊ TIÊU DIỆT! CỔNG DỊCH CHUYỂN ĐANG MỞ...");
        StartCoroutine(OpenExitPortalRoutine());
        Destroy(gameObject, 4.0f);
    }

    /// <summary>
    /// Cơ chế dự phòng đảm bảo Cổng dịch chuyển luôn kích hoạt kể cả khi Boss bị Destroy đột ngột.
    /// </summary>
    private void OnDestroy()
    {
        if (isDead)
        {
            ActivateExitPortalDirectly();
        }
    }

    /// <summary>
    /// Coroutine chờ thời gian trễ trước khi kích hoạt Cổng dịch chuyển.
    /// </summary>
    private IEnumerator OpenExitPortalRoutine()
    {
        yield return new WaitForSeconds(portalAppearDelay);
        ActivateExitPortalDirectly();
    }

    /// <summary>
    /// Kích hoạt trực tiếp Cổng dịch chuyển qua Map 5 Phase 2 ngay tại vị trí Boss ngã xuống.
    /// </summary>
    private void ActivateExitPortalDirectly()
    {
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

        // Failsafe tối thượng: Tự khởi tạo cổng nếu trong Scene bị xóa mất
        if (portalObj == null)
        {
            portalObj = new GameObject("Map5_ExitPortal");
            var col = portalObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.8f);
            var sp = portalObj.AddComponent<ScenePortal>();
            sp.TargetSceneName = "Map5_Phase2";
            portalObj.AddComponent<PortalVisualEffect>();
        }

        if (portalObj != null)
        {
            // Xuất hiện ngay tại nơi Boss bị tiêu diệt
            Vector3 basePos = (deathPosition != Vector3.zero) ? deathPosition : transform.position;
            Vector3 offset = (portalOffsetFromBoss != Vector3.zero) ? portalOffsetFromBoss : new Vector3(1.2f, 0.2f, 0f);
            Vector3 targetPos = basePos + offset;

            portalObj.transform.position = targetPos;

            var pve = portalObj.GetComponent<PortalVisualEffect>();
            if (pve != null)
            {
                pve.SetBasePosition(targetPos);
            }

            portalObj.SetActive(true);

            var srPortal = portalObj.GetComponent<SpriteRenderer>();
            if (srPortal != null)
            {
                srPortal.enabled = true;
                if (srPortal.sortingOrder < 5) srPortal.sortingOrder = 5;
            }

            var colPortal = portalObj.GetComponent<Collider2D>();
            if (colPortal != null) colPortal.enabled = true;

            var scenePortal = portalObj.GetComponent<ScenePortal>();
            if (scenePortal != null)
            {
                scenePortal.enabled = true;
                scenePortal.TargetSceneName = "Map5_Phase2";
            }

            Debug.Log($"🌀 [Skeleton_Boss] CỔNG QUA MAP 5 PHẦN 2 ĐÃ SẴN SÀNG TẠI VỊ TRÍ: {targetPos}!");
        }
        else
        {
            Debug.LogWarning("⚠️ [Skeleton_Boss] Không tìm thấy ExitPortal hoặc ScenePortal nào trong Scene để kích hoạt!");
        }
    }

    /// <summary>
    /// Dò tìm Người chơi trong Scene theo thứ tự: KnightController -> PlayerHealth -> Tag Player.
    /// </summary>
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

    /// <summary>
    /// Vẽ vùng cảm biến trực quan trong Scene Editor (tầm thù địch, tầm đánh, vùng chém quạt).
    /// </summary>
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
