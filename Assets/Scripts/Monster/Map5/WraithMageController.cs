using System.Collections;
using UnityEngine;

/// <summary>
/// Điều khiển quái vật Pháp Sư Vong Hồn (Wraith Mage) trong Map 5 Phase 2.
/// Kế thừa từ <see cref="EnemyHealth"/> để đồng bộ hệ thống máu, hiển thị số sát thương và sự kiện chiến đấu.
/// Sở hữu trí tuệ nhân tạo đánh xa cơ động (Kiting AI): Bay lơ lửng, duy trì cự ly lý tưởng với Hiệp sĩ,
/// Bắn đạn ma thuật tử linh (Soul Missile), Triệu hồi cột gai tử linh dưới chân người chơi (Twin Soul Pillars),
/// và Dịch chuyển lùi gấp khi bị áp sát quá gần (Escape).
/// Tích hợp cơ chế kiểm soát tầm nhìn theo cao độ (trục Y), tầm nhìn thực (Line of Sight) và phạm vi lãnh thổ (Leash)
/// để triệt để chống hiện tượng đánh xuyên tầng, bắn xuyên sàn gạch hoặc hội đồng người chơi từ tầng trên/dưới.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class WraithMageController : EnemyHealth
{
    /// <summary>
    /// Các trạng thái trong máy trạng thái (FSM) của Pháp Sư Vong Hồn.
    /// </summary>
    public enum MageState
    {
        IdleFly,    // Bay lơ lửng tại chỗ
        Patrol,     // Bay tuần tra nhẹ nhàng giữa các mốc trong lãnh thổ
        Chase,      // Duy trì khoảng cách an toàn lý tưởng (Kiting) với Hiệp sĩ
        Attack1,    // Bắn đạn ma thuật tử linh bay ngang (Soul Missile)
        Attack2,    // Triệu hồi cột gai địa ngục dưới chân mục tiêu (Soul Pillars)
        Escape,     // Cuộn áo choàng biến lùi khi Hiệp sĩ áp sát quá nguy hiểm
        Hurt,       // Khựng nhận sát thương
        Die         // Tan biến khi bị tiêu diệt
    }

    [Header("1. Flight & Movement (Bay lơ lửng & Tuần tra)")]
    [Tooltip("Tốc độ bay tuần tra và di chuyển")]
    [SerializeField] private float flySpeed = 2.2f;

    [Tooltip("Biên độ nhấp nhô lơ lửng trên không")]
    [SerializeField] private float hoverHeight = 0.15f;

    [Tooltip("Bán kính tuần tra quanh điểm xuất phát ban đầu")]
    [SerializeField] private float patrolRadius = 4.5f;

    [Tooltip("Layer mặt đất dùng để dò vị trí chân người chơi và kiểm tra chắn tầm nhìn (Line of Sight)")]
    [SerializeField] private LayerMask groundLayer;

    [Header("2. Combat & Ranged Spells (Phép thuật & Tầm bắn xa)")]
    [Tooltip("Tầm phát hiện người chơi theo phương ngang để chuyển sang trạng thái chiến đấu")]
    [SerializeField] private float aggroRange = 6.5f;

    [Tooltip("Khoảng cách người chơi chạy thoát khiến pháp sư hủy thù địch")]
    [SerializeField] private float deAggroRange = 8.5f;

    [Tooltip("Giới hạn chênh lệch độ cao (trục Y) tối đa để phát hiện người chơi (chống đánh xuyên tầng trên/dưới)")]
    [SerializeField] private float maxVerticalRange = 2.5f;

    [Tooltip("Giới hạn khoảng cách tối đa có thể truy đuổi tính từ điểm xuất phát ban đầu (Leash Distance)")]
    [SerializeField] private float maxChaseDistanceFromSpawn = 7.5f;

    [Tooltip("Kiểm tra tầm nhìn không bị cản bởi tường hoặc trần/sàn gạch (Line of Sight)")]
    [SerializeField] private bool requireLineOfSight = true;

    [Tooltip("Khoảng cách giữ cự ly an toàn lý tưởng (Kiting Distance)")]
    [SerializeField] private float idealDistance = 4.5f;

    [Tooltip("Cự ly báo động: Nếu người chơi áp sát nhỏ hơn mốc này sẽ kích hoạt Escape")]
    [SerializeField] private float escapeTriggerDist = 2.0f;

    [Tooltip("Thời gian hồi giữa các lần thi triển phép thuật (giây)")]
    [SerializeField] private float spellCooldown = 2.4f;

    [Tooltip("Sát thương quả cầu đạn ma thuật tử linh (Soul Missile)")]
    [SerializeField] private int missileDamage = 16;

    [Tooltip("Sát thương cột gai địa ngục (Soul Pillar Hazard)")]
    [SerializeField] private int pillarDamage = 26;

    [Tooltip("Prefab đạn ma thuật SoulMissile bay ngang")]
    [SerializeField] private GameObject missilePrefab;

    [Tooltip("Prefab cạm bẫy cột gai SoulPillar cắm dưới chân")]
    [SerializeField] private GameObject pillarHazardPrefab;

    [Header("3. Audio (Âm thanh niệm phép)")]
    [Tooltip("AudioSource phát âm thanh")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Clip âm thanh khi bắt đầu niệm phép thuật")]
    [SerializeField] private AudioClip castSound;

    [Tooltip("Mixer Group dành cho hiệu ứng âm thanh SFX")]
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;

    // Các thành phần nội bộ (Internal Components)
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Transform player;

    // Biến trạng thái máy FSM
    private MageState currentState = MageState.Patrol;
    private int direction = 1; // 1 = nhìn phải, -1 = nhìn trái
    private bool isCasting = false;
    private bool isHurt = false;
    private float cooldownTimer = 0f;
    private float hurtTimer = 0f;
    private float escapeCooldownTimer = 0f;

    // Giới hạn tuần tra, mốc xuất phát gốc và hiệu ứng nhấp nhô
    private Vector2 spawnPosition;
    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;
    private float hoverOffset = 0f;

    /// <summary>
    /// Khởi tạo các thành phần vật lý: triệt tiêu trọng lực để bay lơ lửng tự do.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Bay lơ lửng trên không, không bị trọng lực kéo xuống sàn
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

    /// <summary>
    /// Ghi nhận tọa độ xuất phát làm mốc tuần tra và dò tìm Người chơi trong Scene.
    /// </summary>
    private void Start()
    {
        spawnPosition = transform.position;
        spawnX = spawnPosition.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
    }

    /// <summary>
    /// Vòng lặp cập nhật AI chính: tính toán nhấp nhô lơ lửng, kiểm tra cao độ, cản tầm nhìn, giữ cự ly Kiting và điều phối phép thuật.
    /// </summary>
    private void Update()
    {
        if (isDead) return;

        // Giảm trừ thời gian hồi chiêu
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (escapeCooldownTimer > 0f) escapeCooldownTimer -= Time.deltaTime;

        // Trạng thái bị thương khựng lại
        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f) isHurt = false;
            return;
        }

        // Tự động tìm lại người chơi nếu mất dấu
        if (player == null) FindPlayer();

        // Đang tập trung niệm phép thì đứng yên trên không
        if (isCasting) return;

        // Hiệu ứng nhấp nhô lơ lửng hình sin (Floating hover wave)
        hoverOffset = Mathf.Sin(Time.time * 2.5f) * hoverHeight;

        float horizontalDist = player != null ? Mathf.Abs(transform.position.x - player.position.x) : float.MaxValue;
        float verticalDist = player != null ? Mathf.Abs(transform.position.y - player.position.y) : float.MaxValue;
        float distFromSpawn = Mathf.Abs(transform.position.x - spawnPosition.x);

        // Điều phối máy trạng thái FSM
        switch (currentState)
        {
            case MageState.Patrol:
                // Chỉ kích hoạt thù địch nếu người chơi nằm cùng tầng sàn và không bị tường/sàn gạch che khuất
                if (CanDetectPlayer(out _, out _))
                {
                    currentState = MageState.Chase;
                }
                else
                {
                    Patrol();
                }
                break;

            case MageState.Chase:
                // Kiểm tra điều kiện bỏ thù địch (De-Aggro):
                // 1. Người chơi đã chết
                // 2. Người chơi chạy quá xa theo chiều ngang (> deAggroRange)
                // 3. Người chơi nhảy/chạy sang tầng khác (verticalDist > maxVerticalRange + 1.2m)
                // 4. Pháp sư bay quá xa khỏi lãnh thổ xuất phát ban đầu (> maxChaseDistanceFromSpawn)
                // 5. Tầm nhìn bị che khuất bởi tường hoặc sàn/trần đất gạch
                bool playerDead = false;
                if (player != null)
                {
                    var ph = player.GetComponent<PlayerHealth>();
                    if (ph != null && ph.IsDead) playerDead = true;
                }

                bool shouldDeAggro = player == null ||
                                     playerDead ||
                                     horizontalDist > deAggroRange ||
                                     verticalDist > maxVerticalRange + 1.2f ||
                                     distFromSpawn > maxChaseDistanceFromSpawn;

                if (!shouldDeAggro && requireLineOfSight && groundLayer.value != 0 && player != null)
                {
                    Vector2 startPos = (Vector2)transform.position + Vector2.up * 0.3f;
                    Vector2 endPos = (Vector2)player.position + Vector2.up * 0.3f;
                    RaycastHit2D hit = Physics2D.Linecast(startPos, endPos, groundLayer);
                    if (hit.collider != null)
                    {
                        shouldDeAggro = true;
                    }
                }

                if (shouldDeAggro)
                {
                    currentState = MageState.Patrol;
                    break;
                }

                FaceTarget(player.position.x);

                // Nếu Hiệp sĩ áp sát quá nguy hiểm (< 2.0m cùng tầng) -> Kích hoạt Escape (cuộn áo biến lùi)
                if (horizontalDist < escapeTriggerDist && verticalDist < 1.8f && escapeCooldownTimer <= 0f)
                {
                    TriggerEscape();
                    break;
                }

                // AI Kiting: Nếu đang ở cự ly lý tưởng -> Dừng lại và thi triển phép thuật
                if (horizontalDist <= idealDistance + 1.0f && horizontalDist >= idealDistance - 1.0f)
                {
                    StopFlight();
                    if (cooldownTimer <= 0f)
                    {
                        StartCastSpell();
                    }
                }
                else if (horizontalDist > idealDistance + 1.0f)
                {
                    // Quá xa -> Bay tới gần hơn để vào tầm ngắm (vẫn giữ trong giới hạn leash)
                    if (distFromSpawn < maxChaseDistanceFromSpawn)
                    {
                        FlyTowards(player.position.x, flySpeed);
                    }
                    else
                    {
                        StopFlight();
                    }
                }
                else
                {
                    // Quá gần -> Lùi nhẹ ra sau để giữ khoảng cách an toàn
                    FlyTowards(transform.position.x - direction * 2f, flySpeed * 0.8f);
                }
                break;
        }
    }

    /// <summary>
    /// Kiểm tra người chơi có nằm trong tầm mắt, cùng tầng sàn và không bị tường/sàn cản trở hay không.
    /// </summary>
    private bool CanDetectPlayer(out float hDist, out float vDist)
    {
        hDist = float.MaxValue;
        vDist = float.MaxValue;

        if (player == null) return false;

        // 1. Kiểm tra người chơi đã chết chưa
        var pHealth = player.GetComponent<PlayerHealth>();
        if (pHealth != null && pHealth.IsDead) return false;

        hDist = Mathf.Abs(transform.position.x - player.position.x);
        vDist = Mathf.Abs(transform.position.y - player.position.y);

        // 2. Chống đánh xuyên tầng: chênh lệch độ cao Y không được vượt quá maxVerticalRange
        if (vDist > maxVerticalRange)
            return false;

        // 3. Nằm trong cự ly tầm nhìn ngang
        if (hDist > aggroRange)
            return false;

        // 4. Không đi quá xa khỏi lãnh thổ điểm xuất phát ban đầu
        if (Mathf.Abs(transform.position.x - spawnPosition.x) > maxChaseDistanceFromSpawn)
            return false;

        // 5. Kiểm tra tầm nhìn có bị sàn/tường gạch che khuất không (Line of sight)
        if (requireLineOfSight && groundLayer.value != 0)
        {
            Vector2 start = (Vector2)transform.position + Vector2.up * 0.3f;
            Vector2 end = (Vector2)player.position + Vector2.up * 0.3f;
            RaycastHit2D hit = Physics2D.Linecast(start, end, groundLayer);
            if (hit.collider != null)
            {
                return false; // Bị sàn hoặc tường gạch ngăn cản tầm nhìn
            }
        }

        return true;
    }

    /// <summary>
    /// Xử lý logic bay tuần tra qua lại trong phạm vi bán kính cho phép và tự kéo về độ cao gốc.
    /// </summary>
    private void Patrol()
    {
        if (transform.position.x <= minPatrolX && direction == -1) direction = 1;
        else if (transform.position.x >= maxPatrolX && direction == 1) direction = -1;

        UpdateFacing();

        // Bay mượt mà trở lại độ cao gốc nếu bị lệch trục Y do truy đuổi hoặc né đòn trước đó
        float targetY = spawnPosition.y + hoverOffset;
        float currentY = transform.position.y;
        float moveY = Mathf.Clamp((targetY - currentY) * 2.5f, -flySpeed * 0.5f, flySpeed * 0.5f);

        rb.linearVelocity = new Vector2(direction * flySpeed * 0.6f, moveY);
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    /// <summary>
    /// Bay về phía tọa độ X chỉ định với tốc độ xác định kết hợp hiệu ứng nhấp nhô.
    /// </summary>
    /// <param name="targetX">Tọa độ đích đến.</param>
    /// <param name="speed">Vận tốc bay.</param>
    private void FlyTowards(float targetX, float speed)
    {
        int moveDir = (targetX > transform.position.x) ? 1 : -1;

        // Nhẹ nhàng điều chỉnh độ cao theo người chơi nhưng không vượt quá chênh lệch an toàn
        float targetY = (player != null && Mathf.Abs(player.position.y - spawnPosition.y) <= maxVerticalRange)
            ? player.position.y + 0.8f + hoverOffset
            : spawnPosition.y + hoverOffset;

        float moveY = Mathf.Clamp((targetY - transform.position.y) * 2.0f, -speed * 0.6f, speed * 0.6f);

        rb.linearVelocity = new Vector2(moveDir * speed, moveY);
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    /// <summary>
    /// Dừng bay ngang, chỉ duy trì nhịp lơ lửng tại chỗ.
    /// </summary>
    private void StopFlight()
    {
        rb.linearVelocity = new Vector2(0f, hoverOffset);
        if (animator != null) animator.SetBool("IsMoving", false);
    }

    /// <summary>
    /// Xoay mặt về phía người chơi.
    /// </summary>
    /// <param name="targetX">Tọa độ X của mục tiêu.</param>
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
    /// Lật sprite qua localScale tương ứng với hướng di chuyển.
    /// </summary>
    private void UpdateFacing()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }

    /// <summary>
    /// Khởi động chuỗi niệm phép: lựa chọn ngẫu nhiên giữa bắn đạn (Attack 1) hoặc triệu hồi cột gai (Attack 2).
    /// </summary>
    private void StartCastSpell()
    {
        // Re-check để không niệm phép vu vơ nếu người chơi đã sang tầng khác
        if (!CanDetectPlayer(out _, out _))
        {
            currentState = MageState.Patrol;
            return;
        }

        isCasting = true;
        PlayCastSound();
        StopFlight();
        cooldownTimer = spellCooldown;

        // Tỷ lệ: 65% bắn đạn Soul Missile, 35% triệu hồi cột gai Soul Pillars
        if (Random.value < 0.65f)
        {
            if (animator != null) animator.SetTrigger("Attack1");
        }
        else
        {
            if (animator != null) animator.SetTrigger("Attack2");
        }
    }

    /// <summary>
    /// Phát âm thanh ma thuật khi bắt đầu thi triển phép.
    /// </summary>
    public void PlayCastSound()
    {
        if (castSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(castSound);
        }
    }

    /// <summary>
    /// Animation Event: Bắn quả cầu ma thuật tử linh bay thẳng ngang theo tầm mắt.
    /// </summary>
    public void LaunchSoulMissile()
    {
        if (missilePrefab != null && player != null)
        {
            // Kiểm tra an toàn: Không bắn nếu người chơi đã chạy sang tầng khác hoặc ra quá xa
            float verticalDist = Mathf.Abs(transform.position.y - player.position.y);
            float horizontalDist = Mathf.Abs(transform.position.x - player.position.x);
            if (verticalDist > maxVerticalRange + 1.2f || horizontalDist > deAggroRange)
            {
                return;
            }

            float shootDir = (player.position.x >= transform.position.x) ? 1.0f : -1.0f;
            direction = (int)shootDir;
            UpdateFacing();

            // Tạo đạn tại vị trí ngang tầm trượng và phóng thẳng tắp
            Vector3 spawnPos = transform.position + new Vector3(shootDir * 0.9f, 0.5f, 0);
            GameObject missile = Instantiate(missilePrefab, spawnPos, Quaternion.identity);

            var proj = missile.GetComponent<SoulMissileProjectile>();
            if (proj != null)
            {
                proj.Launch(new Vector2(shootDir, 0f), missileDamage);
            }
        }
    }

    /// <summary>
    /// Animation Event: Dò tìm mặt sàn và triệu hồi cột gai tử linh dưới chân người chơi.
    /// Chỉ cho phép triệu hồi nếu người chơi ở cùng tầng sàn, không triệu hồi xuyên tầng trần/sàn.
    /// </summary>
    public void CastTwinSoulPillars()
    {
        if (player != null)
        {
            // Kiểm tra an toàn: Không triệu hồi nếu người chơi ở tầng khác hoặc ra quá xa
            float verticalDist = Mathf.Abs(transform.position.y - player.position.y);
            float horizontalDist = Mathf.Abs(transform.position.x - player.position.x);
            if (verticalDist > maxVerticalRange + 1.2f || horizontalDist > deAggroRange)
            {
                return;
            }

            FaceTarget(player.position.x);

            Vector3 targetGroundPos = player.position;

            // Bắn raycast dò mặt sàn đất chính xác dưới chân Hero
            RaycastHit2D hit = Physics2D.Raycast(player.position + Vector3.up * 0.5f, Vector2.down, 5f, groundLayer);
            if (hit.collider != null)
            {
                targetGroundPos = new Vector3(player.position.x, hit.point.y, 0f);
            }
            else
            {
                targetGroundPos = new Vector3(player.position.x, player.position.y, 0f);
            }

            if (pillarHazardPrefab != null)
            {
                Instantiate(pillarHazardPrefab, targetGroundPos, Quaternion.identity);
            }
            else
            {
                // Dự phòng gây sát thương có độ trễ nếu chưa gắn prefab
                StartCoroutine(DamagePillarDelayed(targetGroundPos.x));
            }
        }
    }

    /// <summary>
    /// Coroutine dự phòng gây sát thương cột lửa sau khoảng trễ cảnh báo.
    /// </summary>
    /// <param name="targetX">Tọa độ X của cột bẫy.</param>
    private IEnumerator DamagePillarDelayed(float targetX)
    {
        yield return new WaitForSeconds(0.4f);
        if (player != null && Mathf.Abs(player.position.x - targetX) < 1.6f)
        {
            var knight = player.GetComponentInParent<KnightController>();
            if (knight != null) knight.TakeDamage(pillarDamage);

            var mage = player.GetComponentInParent<PlayerHealth>();
            if (mage != null) mage.TakeDamage(pillarDamage);
        }
    }

    /// <summary>
    /// Animation Event: Gọi ở frame cuối của đòn phép để mở khóa trạng thái niệm chú.
    /// </summary>
    public void FinishSpell()
    {
        isCasting = false;
    }

    /// <summary>
    /// Kích hoạt kỹ năng biến bóng lùi (Escape) khi đối thủ áp sát quá gần.
    /// </summary>
    private void TriggerEscape()
    {
        escapeCooldownTimer = 4.0f;
        if (animator != null) animator.SetTrigger("Escape");
        rb.linearVelocity = new Vector2(-direction * 5.5f, 0.5f);
    }

    /// <summary>
    /// Xử lý nhận sát thương từ người chơi: giảm máu, ngắt niệm chú và lùi nhẹ.
    /// </summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
    public override void TakeDamage(int damage)
    {
        if (isDead) return;
        base.TakeDamage(damage);

        isHurt = true;
        hurtTimer = 0.25f;
        isCasting = false;

        if (player != null)
        {
            float verticalDist = Mathf.Abs(transform.position.y - player.position.y);
            // Chỉ thù địch đuổi theo nếu cùng tầng đất
            if (verticalDist <= maxVerticalRange + 1.2f)
            {
                currentState = MageState.Chase;
            }

            int hitFrom = (player.position.x > transform.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(-hitFrom * 2.0f, 1.0f);
        }
    }

    /// <summary>
    /// Xử lý khi Pháp Sư Vong Hồn bị tiêu diệt: tắt va chạm và giải phóng bộ nhớ.
    /// </summary>
    protected override void Die()
    {
        base.Die();
        StopFlight();
        rb.simulated = false;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.5f);
    }

    /// <summary>
    /// Dò tìm đối tượng Người chơi trong Scene theo Tag "Player".
    /// </summary>
    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    /// <summary>
    /// Hiển thị phạm vi tuần tra, tầm nhìn theo độ cao và giới hạn truy đuổi trong Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)spawnPosition : transform.position;

        // 1. Tầm phát hiện theo chiều ngang và chiều cao (Khung vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(aggroRange * 2f, maxVerticalRange * 2f, 0f));

        // 2. Phạm vi tuần tra (Đoạn thẳng xanh dương)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(center.x - patrolRadius, center.y, 0f), new Vector3(center.x + patrolRadius, center.y, 0f));

        // 3. Giới hạn truy đuổi tối đa (Đoạn thẳng đỏ mận)
        Gizmos.color = new Color(1f, 0f, 0.5f, 0.5f);
        Gizmos.DrawLine(new Vector3(center.x - maxChaseDistanceFromSpawn, center.y - 0.2f, 0f), new Vector3(center.x + maxChaseDistanceFromSpawn, center.y - 0.2f, 0f));
    }
}
