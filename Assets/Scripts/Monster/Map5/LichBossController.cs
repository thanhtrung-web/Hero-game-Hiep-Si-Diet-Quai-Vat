using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển Đại Trùm Cuối - Chúa Tể Vong Hồn Lich (Lich Lord) trong Map 5 Phase 2.
/// Kế thừa từ <see cref="EnemyHealth"/> và kích hoạt thuộc tính Boss (IsBoss = true).
/// Cơ chế chiến đấu 2 Giai đoạn (2-Phase Boss Fight) hoành tráng:
/// - Phase 1 (Lich Knight - 100% -> 50% HP): Sử dụng song kiếm ma thuật, chém Soul Slash, lướt bóng đêm thần tốc Shadow Dash, và phóng sóng hắc ám Dark Wave.
/// - Phase Chuyển Giai Đoạn (Transitioning - 50% HP): Tụ năng lượng hư không, phát nổ đẩy lùi diện rộng và biến thân thành Tử Thần Vong Hồn.
/// - Phase 2 (Death Lord - 50% -> 0% HP): Bay lơ lửng, triệu hồi bẫy chông linh hồn Soul Spike từ mặt đất, Dịch chuyển bóng ma ra sau lưng Phantom Teleport,
///   Bùng nổ hư không Death Nova cực đại, và Triệu hồi lâu la tiếp viện liên tục.
/// Khi bị tiêu diệt, kích hoạt bảng chiến thắng toàn diện Game Win (GameEndUI).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class LichBossController : EnemyHealth
{
    /// <summary>
    /// Đánh dấu đây là Trùm để hiển thị thanh máu Boss ở đỉnh màn hình.
    /// </summary>
    public override bool IsBoss => true;

    /// <summary>
    /// Danh hiệu hiển thị trên thanh máu Boss.
    /// </summary>
    public override string BossTitle => "Đại Phù Thủy Hắc Ám - Lich Lord";

    /// <summary>
    /// Các giai đoạn chiến đấu của Lich Boss.
    /// </summary>
    public enum BossPhase
    {
        Phase1_LichKnight, // Giai đoạn 1: Kỵ Sĩ Hắc Ám dùng kiếm cận chiến cơ động
        Transitioning,     // Giai đoạn chuyển giao: Biến thân và bùng nổ đẩy lùi
        Phase2_DeathLord   // Giai đoạn 2: Tử Thần Vong Hồn bay lơ lửng và tung đại ma pháp
    }

    [Header("1. Boss Phase & Core Stats (Giai đoạn & Chỉ số cơ bản)")]
    [Tooltip("Giai đoạn hiện tại của Boss")]
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1_LichKnight;

    [Tooltip("Lượng máu tối đa của Boss Lich")]
    [SerializeField] private int maxBossHealth = 600;

    [Tooltip("Tốc độ di chuyển trên mặt đất ở Phase 1")]
    [SerializeField] private float moveSpeedP1 = 3.6f;

    [Tooltip("Thời gian hồi giữa các nhịp chiêu thức dồn dập ở Phase 1 (giây)")]
    [SerializeField] private float phase1SkillCooldown = 1.1f;

    [Tooltip("Tốc độ bay lượn lơ lửng ở Phase 2")]
    [SerializeField] private float flySpeedP2 = 3.6f;

    [Tooltip("Layer mặt đất dùng để dò sàn cắm bẫy chông gai")]
    [SerializeField] private LayerMask groundLayer;

    [Header("2. Combat Ranges & General Cooldowns (Khoảng cách & Hồi chiêu)")]
    [Tooltip("Tầm phát hiện người chơi")]
    [SerializeField] private float aggroRange = 9f;

    [Tooltip("Tầm cận chiến để bắt đầu vung kiếm")]
    [SerializeField] private float meleeRange = 2.8f;

    [Tooltip("Thời gian hồi giữa các kỹ năng lớn ở Phase 2 (giây)")]
    [SerializeField] private float skillCooldown = 2.5f;

    [Header("3. Phase 1 Skills - Lich Knight (Bộ kỹ năng Giai đoạn 1)")]
    [Tooltip("Sát thương nhát chém linh hồn Soul Slash")]
    [SerializeField] private int soulSlashDamage = 20;

    [Tooltip("Sát thương đa đoạn mỗi nhịp lướt bóng đêm Shadow Dash")]
    [SerializeField] private int shadowDashDamage = 5;

    [Tooltip("Sát thương sóng năng lượng hắc ám Dark Wave")]
    [SerializeField] private int darkWaveDamage = 30;

    [Tooltip("Vận tốc lướt thần tốc")]
    [SerializeField] private float dashSpeed = 16f;

    [Tooltip("Thời gian duy trì cú lướt (giây)")]
    [SerializeField] private float dashDuration = 0.35f;

    [Header("4. Phase 2 Skills - Death Lord (Bộ kỹ năng Giai đoạn 2)")]
    [Tooltip("Sát thương bẫy chông gai địa ngục Soul Spike")]
    [SerializeField] private int soulSpikeDamage = 35;

    [Tooltip("Sát thương vụ nổ 360 độ cực đại Death Nova")]
    [SerializeField] private int deathNovaDamage = 45;

    [Tooltip("Bán kính vùng nổ sóng chấn động Death Nova")]
    [SerializeField] private float deathNovaRadius = 5.2f;

    [Tooltip("Sát thương nhát chém chớp nhoáng sau khi dịch chuyển Phantom Teleport")]
    [SerializeField] private int teleportSlashDamage = 30;

    [Tooltip("Prefab cạm bẫy chông gai Soul Spike")]
    [SerializeField] private GameObject soulSpikePrefab;

    [Tooltip("Prefab quái lâu la triệu hồi 1 (Skeleton)")]
    [SerializeField] private GameObject minionPrefab;

    [Tooltip("Prefab quái lâu la triệu hồi 2 (DeathKnight/WraithMage)")]
    [SerializeField] private GameObject minionPrefab2;

    [Tooltip("Số lượng đệ tử triệu hồi tối đa cùng lúc")]
    [SerializeField] private int maxActiveMinions = 4;

    [Header("5. Overhead UI (Thanh máu nổi trên đầu Boss)")]
    [Tooltip("Vị trí lệch của thanh máu so với tâm Boss")]
    [SerializeField] private Vector3 barOffset = new Vector3(-1.1f, 3.5f, 0f);

    [Header("6. Audio (Hệ thống âm thanh SFX)")]
    [Tooltip("AudioSource phát âm thanh")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Clip âm thanh chung khi phát động kỹ năng")]
    [SerializeField] private AudioClip skillSound;

    [Tooltip("Clip âm thanh lướt bóng đêm Shadow Dash")]
    [SerializeField] private AudioClip dashSound;

    [Tooltip("Clip âm thanh dịch chuyển hư không Phantom Teleport")]
    [SerializeField] private AudioClip teleportSound;

    [Tooltip("Mixer Group dành cho hiệu ứng âm thanh SFX")]
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;

    // Components nội bộ (Internal Components)
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Transform player;

    // Biến trạng thái máy FSM
    private int direction = 1;
    private bool isActing = false;
    private float nextSkillTimer = 0f;
    private int currentHP;
    private int phase1ThresholdHP;
    private readonly List<GameObject> activeMinions = new List<GameObject>();

    // Thanh máu nổi mini phía trên đầu Boss
    private Transform barRoot;
    private Transform hpFill;
    private SpriteRenderer hpFillSr;

    /// <summary>
    /// Giai đoạn chiến đấu hiện tại của Boss.
    /// </summary>
    public BossPhase CurrentPhase => currentPhase;

    /// <summary>
    /// Khởi tạo chỉ số máu tối đa, phân chia ngưỡng máu chuyển phase và cấu hình âm thanh.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        if (sfxMixerGroup != null)
            audioSource.outputAudioMixerGroup = sfxMixerGroup;

        // Thiết lập thanh máu gốc
        maxHealth = maxBossHealth;
        currentHealth = maxBossHealth;
        currentHP = maxBossHealth;
        phase1ThresholdHP = maxBossHealth / 2; // Ngưỡng 50% máu kích hoạt Phase 2
    }

    /// <summary>
    /// Tạo thanh máu mini trên đầu và dò tìm Người chơi trong Scene.
    /// </summary>
    private void Start()
    {
        CreateOverheadBar();
        FindPlayer();
    }

    /// <summary>
    /// Vòng lặp cập nhật AI chính: đếm thời gian hồi chiêu, điều phối AI theo từng Phase và cập nhật thanh máu.
    /// </summary>
    private void Update()
    {
        if (isDead)
        {
            if (barRoot != null) barRoot.gameObject.SetActive(false);
            return;
        }

        UpdateOverheadBar();

        if (nextSkillTimer > 0f)
            nextSkillTimer -= Time.deltaTime;

        if (player == null)
            FindPlayer();

        // Đang thực hiện chuỗi chiêu thức hoặc đang biến thân thì dừng di chuyển thông thường
        if (isActing || currentPhase == BossPhase.Transitioning)
            return;

        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
        if (distToPlayer > aggroRange)
        {
            StopMoving();
            return;
        }

        FaceTarget(player.position.x);

        // Phân nhánh logic AI theo từng Giai đoạn
        if (currentPhase == BossPhase.Phase1_LichKnight)
        {
            HandlePhase1AI(distToPlayer);
        }
        else if (currentPhase == BossPhase.Phase2_DeathLord)
        {
            HandlePhase2AI(distToPlayer);
        }
    }

    // ==========================================
    // PHASE 1 AI: LICH KNIGHT (100% -> 50% HP)
    // ==========================================
    /// <summary>
    /// Điều phối hành vi AI ở Giai đoạn 1: cận chiến dồn dập bằng song kiếm và lướt bóng đêm.
    /// </summary>
    /// <param name="dist">Khoảng cách tới người chơi.</param>
    private void HandlePhase1AI(float dist)
    {
        if (nextSkillTimer <= 0f)
        {
            // Chọn chiêu thức dứt khoát theo cự ly
            if (dist <= meleeRange)
            {
                // Cận chiến: 70% chém kiếm Soul Slash, 30% chưởng sóng bóng tối Dark Wave
                if (Random.value < 0.7f)
                    StartCoroutine(ExecuteSoulSlash());
                else
                    StartCoroutine(ExecuteDarkWave());
            }
            else
            {
                // Cự ly xa: 75% Lướt thần tốc Shadow Dash áp sát cực nhanh, 25% Dark Wave
                if (Random.value < 0.75f)
                    StartCoroutine(ExecuteShadowDash());
                else
                    StartCoroutine(ExecuteDarkWave());
            }
            return;
        }

        // Trong lúc hồi chiêu ngắn (1.1s): Luôn di chuyển áp sát, không bao giờ đứng đơ
        if (dist > meleeRange * 0.7f)
        {
            MoveForward(moveSpeedP1);
        }
        else
        {
            MoveForward(moveSpeedP1 * 0.5f);
        }
    }

    /// <summary>
    /// Kỹ năng Soul Slash: Vung kiếm chém quét hình vòng cung gây sát thương lớn.
    /// </summary>
    private IEnumerator ExecuteSoulSlash()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("SoulSlash");
        PlaySkillSound();

        yield return new WaitForSeconds(0.3f);
        ApplyAoEDamage(transform.position + new Vector3(direction * 1.8f, 0.8f, 0f), 1.9f, soulSlashDamage);

        yield return new WaitForSeconds(0.18f); // Hồi phục nhanh, không bị đứng đơ
        isActing = false;
        nextSkillTimer = phase1SkillCooldown;
    }

    /// <summary>
    /// Kỹ năng Shadow Dash: Lướt bóng đêm thần tốc xuyên qua người chơi, để lại tàn ảnh và gây sát thương đa đoạn.
    /// </summary>
    private IEnumerator ExecuteShadowDash()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("ShadowDash");
        if (dashSound != null && audioSource != null)
            audioSource.PlayOneShot(dashSound);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);
            SpawnAfterimage(new Color(0.6f, 0.2f, 0.9f, 0.6f));
            ApplyAoEDamage(transform.position + new Vector3(0f, 0.8f, 0f), 1.4f, shadowDashDamage);
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMoving();
        yield return new WaitForSeconds(0.12f);
        isActing = false;
        nextSkillTimer = phase1SkillCooldown;
    }

    /// <summary>
    /// Kỹ năng Dark Wave: Phóng chuỗi sóng năng lượng hắc ám liên tiếp 4 đợt về phía trước.
    /// </summary>
    private IEnumerator ExecuteDarkWave()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("DarkWave");
        PlaySkillSound();

        yield return new WaitForSeconds(0.45f);
        // Sóng hắc ám Dark Wave phóng ra 4 gai liên hoàn phía trước
        for (int i = 1; i <= 4; i++)
        {
            Vector3 wavePos = transform.position + new Vector3(direction * (i * 1.6f), 0.5f, 0f);
            ApplyAoEDamage(wavePos, 1.3f, darkWaveDamage);
            yield return new WaitForSeconds(0.12f);
        }

        yield return new WaitForSeconds(0.18f);
        isActing = false;
        nextSkillTimer = phase1SkillCooldown;
    }

    // ==========================================
    // PHASE TRANSITION (50% HP BURST)
    // ==========================================
    /// <summary>
    /// Coroutine chuyển giao Giai đoạn khi Boss tụt dưới 50% HP: quỳ tụ năng lượng,
    /// kích hoạt sóng xung kích hất văng người chơi và biến thân sang Phase 2.
    /// </summary>
    private IEnumerator TriggerPhaseTransition()
    {
        currentPhase = BossPhase.Transitioning;
        isActing = true;
        StopMoving();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (anim != null)
        {
            anim.SetTrigger("Transform");
            PlaySkillSound();
            anim.SetBool("IsPhase2", true);
        }

        // Quỳ tụ năng lượng hư không -> Bùng nổ đẩy lùi cực mạnh
        yield return new WaitForSeconds(0.6f);
        KnockbackNearbyPlayers(4f, 12f);

        yield return new WaitForSeconds(0.6f);
        currentPhase = BossPhase.Phase2_DeathLord;
        if (hpFillSr != null)
            hpFillSr.color = new Color(0.85f, 0.35f, 1f, 1f); // Đổi thanh HP mini sang màu tím neon tử thần

        isActing = false;
        nextSkillTimer = 1.0f;
    }

    // ==========================================
    // PHASE 2 AI: DEATH LORD (50% -> 0% HP)
    // ==========================================
    /// <summary>
    /// Điều phối hành vi AI ở Giai đoạn 2: bay lơ lửng, triệu hồi lâu la và thi triển 3 đại ma pháp.
    /// </summary>
    /// <param name="dist">Khoảng cách tới người chơi.</param>
    private void HandlePhase2AI(float dist)
    {
        CleanupDeadMinions();

        if (nextSkillTimer <= 0f)
        {
            // 1. Nếu số lượng đệ còn ít -> 35% xác suất gọi thêm lâu la
            if (activeMinions.Count < maxActiveMinions && Random.value < 0.35f)
            {
                StartCoroutine(ExecuteSummon());
                return;
            }

            // 2. Chọn ngẫu nhiên giữa 3 đại ma pháp uy lực
            float roll = Random.value;
            if (roll < 0.33f)
                StartCoroutine(ExecuteSoulSpike());
            else if (roll < 0.66f)
                StartCoroutine(ExecutePhantomTeleport());
            else
                StartCoroutine(ExecuteDeathNova());

            return;
        }

        // Bay lơ lửng áp sát người chơi
        if (dist > meleeRange)
        {
            Vector2 target = (Vector2)player.position + new Vector2(0f, 0.5f);
            Vector2 dirVec = (target - (Vector2)transform.position).normalized;
            rb.linearVelocity = dirVec * flySpeedP2;
            if (anim != null) anim.SetBool("IsMoving", true);
        }
        else
        {
            StopMoving();
        }
    }

    /// <summary>
    /// Kỹ năng Soul Spike: Dò tìm mặt sàn và triệu hồi 3 cọc chông linh hồn đâm vọt lên dưới chân người chơi.
    /// </summary>
    private IEnumerator ExecuteSoulSpike()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("SoulSpike");
        PlaySkillSound();

        // 1. Dò tìm chính xác mặt đất dưới chân người chơi
        Vector3 targetGroundPos = player != null ? player.position : transform.position + new Vector3(direction * 3f, 0f, 0f);
        if (player != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(player.position + Vector3.up * 0.5f, Vector2.down, 6f, groundLayer);
            if (hit.collider != null)
            {
                targetGroundPos = new Vector3(player.position.x, hit.point.y + 0.1f, 0f);
            }
        }

        // Chờ Lich gồng tay niệm chú
        yield return new WaitForSeconds(0.4f);

        // 2. Triệu hồi chông linh hồn đâm vọt lên từ mặt đất dưới chân Hiệp Sĩ (3 cột: chính giữa và 2 bên)
        if (soulSpikePrefab != null)
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector3 spikePos = targetGroundPos + new Vector3(i * 1.5f, 0f, 0f);
                GameObject spikeObj = Instantiate(soulSpikePrefab, spikePos, Quaternion.identity);
                var hazard = spikeObj.GetComponent<SoulPillarHazard>();
                if (hazard != null)
                {
                    hazard.SetDamage(soulSpikeDamage);
                }
            }
        }
        else
        {
            // Dự phòng nếu không có prefab: gây sát thương tại vị trí mặt đất dưới chân Knight
            for (int i = -1; i <= 1; i++)
            {
                Vector3 spikePos = targetGroundPos + new Vector3(i * 1.5f, 0.5f, 0f);
                ApplyAoEDamage(spikePos, 1.2f, soulSpikeDamage);
            }
        }

        yield return new WaitForSeconds(0.45f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    /// <summary>
    /// Kỹ năng Phantom Teleport: Để lại tàn ảnh, dịch chuyển tức thời ra sau lưng người chơi và chém chớp nhoáng.
    /// </summary>
    private IEnumerator ExecutePhantomTeleport()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("PhantomTeleport");
        if (teleportSound != null && audioSource != null)
            audioSource.PlayOneShot(teleportSound);

        // Để lại bóng ma hư không tại chỗ cũ
        for (int i = 0; i < 2; i++)
        {
            SpawnAfterimage(new Color(0.4f, 0.85f, 1f, 0.8f));
            yield return new WaitForSeconds(0.1f);
        }

        // Dịch chuyển ra phía sau lưng người chơi
        if (player != null)
        {
            float side = (player.localScale.x > 0) ? -1.8f : 1.8f;
            transform.position = player.position + new Vector3(side, 0.5f, 0f);
            FaceTarget(player.position.x);
        }

        yield return new WaitForSeconds(0.2f);
        // Chém kết liễu sau khi xuất hiện
        ApplyAoEDamage(transform.position + new Vector3(direction * 1.8f, 0.8f, 0f), 1.8f, teleportSlashDamage);

        yield return new WaitForSeconds(0.18f);
        isActing = false;
        nextSkillTimer = phase1SkillCooldown;
    }

    /// <summary>
    /// Kỹ năng Death Nova: Tụ năng lượng hư không rồi kích nổ 360 độ cực đại (bán kính 5.2m), hất văng mọi mục tiêu.
    /// </summary>
    private IEnumerator ExecuteDeathNova()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("DeathNova");
        PlaySkillSound();

        // Gồng tụ năng lượng 0.5s rồi phát nổ 360 độ cực đại (bán kính mở rộng lên 5.2m)
        yield return new WaitForSeconds(0.5f);
        Vector3 novaCenter = transform.position + new Vector3(0f, 0.8f, 0f);
        SpawnNovaVFX(novaCenter, deathNovaRadius);
        ApplyAoEDamage(novaCenter, deathNovaRadius, deathNovaDamage);
        KnockbackNearbyPlayers(deathNovaRadius + 0.4f, 16f);

        yield return new WaitForSeconds(0.35f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    /// <summary>
    /// Sinh hiệu ứng sóng xung kích trực quan cho vụ nổ Death Nova.
    /// </summary>
    /// <param name="center">Tâm vụ nổ.</param>
    /// <param name="maxRadius">Bán kính nổ tối đa.</param>
    private void SpawnNovaVFX(Vector3 center, float maxRadius)
    {
        GameObject fxObj = new GameObject("DeathNova_VFX");
        fxObj.transform.position = center;
        StartCoroutine(NovaShockwaveRoutine(fxObj, maxRadius));
    }

    /// <summary>
    /// Coroutine hoạt họa vòng tròn sóng xung kích phóng to và mờ dần.
    /// </summary>
    private IEnumerator NovaShockwaveRoutine(GameObject fxObj, float maxRadius)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        var sr = fxObj.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = new Color(0.6f, 0.15f, 0.95f, 0.6f);
        sr.sortingOrder = 45;

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration && fxObj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentScale = Mathf.Lerp(1.0f, maxRadius * 2.2f, t);
            fxObj.transform.localScale = new Vector3(currentScale, currentScale * 0.75f, 1f);

            Color c = sr.color;
            c.a = Mathf.Lerp(0.6f, 0f, t);
            sr.color = c;
            yield return null;
        }

        if (fxObj != null) Destroy(fxObj);
    }

    /// <summary>
    /// Kỹ năng Summon: Triệu hồi 2 lâu la trợ chiến hai bên sườn Boss.
    /// </summary>
    private IEnumerator ExecuteSummon()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("Summon");
        PlaySkillSound();

        yield return new WaitForSeconds(0.5f);
        if (minionPrefab != null || minionPrefab2 != null)
        {
            int spawnNum = Mathf.Min(2, maxActiveMinions - activeMinions.Count);
            for (int i = 0; i < spawnNum; i++)
            {
                float side = (i == 0) ? 2.5f : -2.5f;
                Vector3 spawnPos = transform.position + new Vector3(side, 0f, 0f);
                GameObject prefabToSpawn = (i == 0 || minionPrefab2 == null) ? minionPrefab : minionPrefab2;
                if (prefabToSpawn == null) prefabToSpawn = minionPrefab2 != null ? minionPrefab2 : minionPrefab;
                if (prefabToSpawn != null)
                {
                    GameObject m = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                    activeMinions.Add(m);
                }
            }
        }

        yield return new WaitForSeconds(0.18f);
        isActing = false;
        nextSkillTimer = phase1SkillCooldown;
    }

    // ==========================================
    // TIỆN ÍCH HITBOX & DAMAGE
    // ==========================================
    /// <summary>
    /// Phát âm thanh chung khi kích hoạt chiêu thức.
    /// </summary>
    public void PlaySkillSound()
    {
        if (skillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(skillSound);
        }
    }

    /// <summary>
    /// Gây sát thương diện rộng hình tròn lên Người chơi (Hiệp Sĩ và Pháp Sư).
    /// </summary>
    /// <param name="center">Tâm vùng sát thương.</param>
    /// <param name="radius">Bán kính quét.</param>
    /// <param name="dmg">Lượng sát thương gây ra.</param>
    private void ApplyAoEDamage(Vector2 center, float radius, int dmg)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var knight = hit.GetComponent<KnightController>();
                if (knight != null) knight.TakeDamage(dmg);

                var mage = hit.GetComponent<PlayerHealth>();
                if (mage != null) mage.TakeDamage(dmg);
            }
        }
    }

    /// <summary>
    /// Tạo lực hất văng người chơi ra xa khỏi tâm vụ nổ.
    /// </summary>
    /// <param name="radius">Bán kính quét mục tiêu.</param>
    /// <param name="force">Lực đẩy lùi.</param>
    private void KnockbackNearbyPlayers(float radius, float force)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var playerRb = hit.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 pushDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    playerRb.linearVelocity = pushDir * force;
                }
            }
        }
    }

    /// <summary>
    /// Xử lý nhận sát thương từ người chơi: giảm máu, kiểm tra ngưỡng chuyển Phase (50%),
    /// và kích hoạt chuỗi chiến thắng Game Win khi HP chạm 0.
    /// </summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
    public override void TakeDamage(int damage)
    {
        if (isDead || currentPhase == BossPhase.Transitioning)
            return;

        currentHP = Mathf.Max(0, currentHP - Mathf.Max(0, damage));
        base.TakeDamage(damage);

        // Kích hoạt chuyển Phase khi HP <= 50%
        if (currentPhase == BossPhase.Phase1_LichKnight && currentHP <= phase1ThresholdHP)
        {
            StartCoroutine(TriggerPhaseTransition());
            return;
        }

        if (isDead)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            StartCoroutine(VictorySequenceRoutine());
            Destroy(gameObject, 3.5f);
        }
    }

    /// <summary>
    /// Coroutine kích hoạt bảng kết thúc chiến thắng (GameEndUI) sau khi Đại trùm Lich ngã xuống.
    /// </summary>
    private System.Collections.IEnumerator VictorySequenceRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        // Kích hoạt bảng chiến thắng Game Win khi tiêu diệt Đại trùm Lich Boss
        GameEndUI gameEndUI = FindAnyObjectByType<GameEndUI>(FindObjectsInactive.Include);
        if (gameEndUI != null)
        {
            Debug.Log("🏆 [Lich_Boss] ĐẠI TRÙM ĐÃ BỊ HẠ GỤC! KÍCH HOẠT CHIẾN THẮNG!");
            gameEndUI.ShowWin();
        }
        else
        {
            // Fallback: Tìm cổng chiến thắng nếu có trong Scene
            var winPortals = FindObjectsByType<GameWinPortal>(FindObjectsInactive.Include);
            if (winPortals != null && winPortals.Length > 0)
            {
                winPortals[0].gameObject.SetActive(true);
                winPortals[0].transform.position = transform.position;
            }
        }
    }

    /// <summary>
    /// Tạo bóng ma tàn ảnh (Ghost Trail) khi Boss di chuyển tốc độ cao hoặc biến thân.
    /// </summary>
    /// <param name="color">Màu sắc của tàn ảnh.</param>
    private void SpawnAfterimage(Color color)
    {
        if (sr == null || sr.sprite == null) return;
        GameObject ghost = new GameObject("GhostTrail");
        ghost.transform.position = transform.position;
        ghost.transform.localScale = transform.localScale;
        var gSr = ghost.AddComponent<SpriteRenderer>();
        gSr.sprite = sr.sprite;
        gSr.flipX = sr.flipX;
        gSr.color = color;
        gSr.sortingOrder = sr.sortingOrder - 1;
        Destroy(ghost, 0.35f);
    }

    /// <summary>
    /// Di chuyển về phía trước theo hướng hiện tại với tốc độ xác định.
    /// </summary>
    /// <param name="speed">Tốc độ di chuyển.</param>
    private void MoveForward(float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (anim != null) anim.SetBool("IsMoving", true);
    }

    /// <summary>
    /// Dừng hoàn toàn quán tính di chuyển.
    /// </summary>
    private void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("IsMoving", false);
    }

    /// <summary>
    /// Xoay mặt về phía mục tiêu.
    /// </summary>
    /// <param name="targetX">Tọa độ X cần nhìn về.</param>
    private void FaceTarget(float targetX)
    {
        if (targetX > transform.position.x && direction < 0)
        {
            direction = 1;
            if (sr != null) sr.flipX = false;
        }
        else if (targetX < transform.position.x && direction > 0)
        {
            direction = -1;
            if (sr != null) sr.flipX = true;
        }
    }

    /// <summary>
    /// Loại bỏ các đối tượng lâu la đã bị tiêu diệt khỏi danh sách quản lý.
    /// </summary>
    private void CleanupDeadMinions()
    {
        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            if (activeMinions[i] == null)
                activeMinions.RemoveAt(i);
        }
    }

    /// <summary>
    /// Khởi tạo thanh máu nổi pixel mini trên đầu Boss.
    /// </summary>
    private void CreateOverheadBar()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        barRoot = new GameObject("LichBossBar").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = barOffset;

        // Lớp nền (BG)
        CreateBarLayer("BG", new Vector3(-0.05f, 0f, 0f), new Vector3(2.3f, 0.28f, 1f), new Color(0.06f, 0.05f, 0.1f, 0.9f), pixelSprite, 10);
        // Lớp thanh máu (HP Fill) - Phase 1: Xanh ngọc Cyan, Phase 2: Tím neon Neon-Violet
        var hpObj = CreateBarLayer("HPFill", Vector3.zero, new Vector3(2.2f, 0.18f, 1f), new Color(0.2f, 0.85f, 1f, 1f), pixelSprite, 11);
        hpFill = hpObj.transform;
        hpFillSr = hpObj.GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Tạo một thành phần lớp hiển thị của thanh máu.
    /// </summary>
    private GameObject CreateBarLayer(string name, Vector3 localPos, Vector3 scale, Color color, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(barRoot, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return go;
    }

    /// <summary>
    /// Cập nhật tỷ lệ co giãn thanh máu nổi theo lượng HP hiện tại.
    /// </summary>
    private void UpdateOverheadBar()
    {
        if (hpFill != null && maxBossHealth > 0)
        {
            float ratio = Mathf.Clamp01((float)currentHP / maxBossHealth);
            hpFill.localScale = new Vector3(2.2f * ratio, 0.18f, 1f);
        }
    }

    /// <summary>
    /// Dò tìm đối tượng Người chơi trong Scene theo Tag "Player".
    /// </summary>
    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }
}
