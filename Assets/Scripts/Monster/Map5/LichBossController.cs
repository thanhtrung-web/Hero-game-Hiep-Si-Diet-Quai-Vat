using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class LichBossController : EnemyHealth
{
    public enum BossPhase
    {
        Phase1_LichKnight,
        Transitioning,
        Phase2_DeathLord
    }

    [Header("1. Boss Phase & Core Stats")]
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1_LichKnight;
    [SerializeField] private int maxBossHealth = 600;
    [SerializeField] private float moveSpeedP1 = 2.4f;
    [SerializeField] private float flySpeedP2 = 3.6f;
    [SerializeField] private LayerMask groundLayer;

    [Header("2. Combat Ranges & General Cooldowns")]
    [SerializeField] private float aggroRange = 9f;
    [SerializeField] private float meleeRange = 2.8f;
    [SerializeField] private float skillCooldown = 2.5f;

    [Header("3. Phase 1 Skills (Lich Knight)")]
    [SerializeField] private int soulSlashDamage = 20;
    [SerializeField] private int shadowDashDamage = 25;
    [SerializeField] private int darkWaveDamage = 30;
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.35f;

    [Header("4. Phase 2 Skills (Death Lord)")]
    [SerializeField] private int soulSpikeDamage = 35;
    [SerializeField] private int deathNovaDamage = 45;
    [SerializeField] private int teleportSlashDamage = 30;
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int maxActiveMinions = 4;

    [Header("5. Overhead UI")]
    [SerializeField] private Vector3 barOffset = new Vector3(-1.1f, 3.5f, 0f);

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Transform player;

    private int direction = 1;
    private bool isActing = false;
    private float nextSkillTimer = 0f;
    private int currentHP;
    private int phase1ThresholdHP;
    private readonly List<GameObject> activeMinions = new List<GameObject>();

    // Overhead Boss Health Bar
    private Transform barRoot;
    private Transform hpFill;
    private SpriteRenderer hpFillSr;

    public BossPhase CurrentPhase => currentPhase;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        currentHP = maxBossHealth;
        phase1ThresholdHP = maxBossHealth / 2;
    }

    private void Start()
    {
        CreateOverheadBar();
        FindPlayer();
    }

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

        if (isActing || currentPhase == BossPhase.Transitioning)
            return;

        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
        if (distToPlayer > aggroRange)
        {
            StopMoving();
            return;
        }

        FaceTarget(player.position.x);

        // Logic AI theo Phase
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
    private void HandlePhase1AI(float dist)
    {
        if (nextSkillTimer <= 0f)
        {
            // Chọn skill phù hợp theo khoảng cách
            if (dist <= meleeRange)
            {
                // Gần: 60% Soul Slash, 40% Dark Wave
                if (Random.value < 0.6f)
                    StartCoroutine(ExecuteSoulSlash());
                else
                    StartCoroutine(ExecuteDarkWave());
            }
            else
            {
                // Xa: 50% Shadow Dash áp sát, 50% Dark Wave
                if (Random.value < 0.5f)
                    StartCoroutine(ExecuteShadowDash());
                else
                    StartCoroutine(ExecuteDarkWave());
            }
            return;
        }

        // Trong thời gian hồi skill: đi bộ áp sát
        if (dist > meleeRange * 0.8f)
        {
            MoveForward(moveSpeedP1);
        }
        else
        {
            StopMoving();
        }
    }

    private IEnumerator ExecuteSoulSlash()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("SoulSlash");

        yield return new WaitForSeconds(0.4f);
        ApplyAoEDamage(transform.position + new Vector3(direction * 1.8f, 0.8f, 0f), 1.8f, soulSlashDamage);

        yield return new WaitForSeconds(0.4f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    private IEnumerator ExecuteShadowDash()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("ShadowDash");

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
        yield return new WaitForSeconds(0.25f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    private IEnumerator ExecuteDarkWave()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("DarkWave");

        yield return new WaitForSeconds(0.45f);
        // Sóng hắc ám lan ra 3 vị trí phía trước
        for (int i = 1; i <= 3; i++)
        {
            Vector3 wavePos = transform.position + new Vector3(direction * (i * 1.6f), 0.5f, 0f);
            ApplyAoEDamage(wavePos, 1.3f, darkWaveDamage);
            yield return new WaitForSeconds(0.12f);
        }

        yield return new WaitForSeconds(0.35f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    // ==========================================
    // PHASE TRANSITION (50% HP BURST)
    // ==========================================
    private IEnumerator TriggerPhaseTransition()
    {
        currentPhase = BossPhase.Transitioning;
        isActing = true;
        StopMoving();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (anim != null)
        {
            anim.SetTrigger("Transform");
            anim.SetBool("IsPhase2", true);
        }

        // Quỳ tụ năng lượng -> Bùng nổ đẩy lùi
        yield return new WaitForSeconds(0.6f);
        KnockbackNearbyPlayers(4f, 12f);

        yield return new WaitForSeconds(0.6f);
        currentPhase = BossPhase.Phase2_DeathLord;
        if (hpFillSr != null)
            hpFillSr.color = new Color(0.85f, 0.35f, 1f, 1f); // Đổi thanh HP sang màu tím neon

        isActing = false;
        nextSkillTimer = 1.0f;
    }

    // ==========================================
    // PHASE 2 AI: DEATH LORD (50% -> 0% HP)
    // ==========================================
    private void HandlePhase2AI(float dist)
    {
        CleanupDeadMinions();

        if (nextSkillTimer <= 0f)
        {
            // 1. Nếu ít đệ -> 35% xác suất gọi thêm xương
            if (activeMinions.Count < maxActiveMinions && Random.value < 0.35f)
            {
                StartCoroutine(ExecuteSummon());
                return;
            }

            // 2. Chọn ngẫu nhiên giữa 3 skill lớn
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

    private IEnumerator ExecuteSoulSpike()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("SoulSpike");

        // Cột linh hồn trồi lên từ vị trí người chơi và xung quanh
        Vector3 playerPos = player != null ? player.position : transform.position + new Vector3(direction * 3f, 0f, 0f);
        yield return new WaitForSeconds(0.45f);

        for (int i = -1; i <= 1; i++)
        {
            Vector3 spikePos = playerPos + new Vector3(i * 1.8f, 0.5f, 0f);
            ApplyAoEDamage(spikePos, 1.2f, soulSpikeDamage);
        }

        yield return new WaitForSeconds(0.4f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    private IEnumerator ExecutePhantomTeleport()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("PhantomTeleport");

        // Để lại bóng ma tại chỗ cũ
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

        yield return new WaitForSeconds(0.35f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    private IEnumerator ExecuteDeathNova()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("DeathNova");

        // Gồng tụ năng lượng 0.5s rồi phát nổ 360 độ
        yield return new WaitForSeconds(0.5f);
        ApplyAoEDamage(transform.position + new Vector3(0f, 0.8f, 0f), 3.4f, deathNovaDamage);
        KnockbackNearbyPlayers(3.5f, 14f);

        yield return new WaitForSeconds(0.4f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    private IEnumerator ExecuteSummon()
    {
        isActing = true;
        StopMoving();
        if (anim != null) anim.SetTrigger("Summon");

        yield return new WaitForSeconds(0.5f);
        if (minionPrefab != null)
        {
            int spawnNum = Mathf.Min(2, maxActiveMinions - activeMinions.Count);
            for (int i = 0; i < spawnNum; i++)
            {
                float side = (i == 0) ? 2.0f : -2.0f;
                Vector3 spawnPos = transform.position + new Vector3(side, 0f, 0f);
                GameObject m = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
                activeMinions.Add(m);
            }
        }

        yield return new WaitForSeconds(0.35f);
        isActing = false;
        nextSkillTimer = skillCooldown;
    }

    // ==========================================
    // TIỆN ÍCH HITBOX & DAMAGE
    // ==========================================
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
            Destroy(gameObject, 2.5f);
        }
    }

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

    private void MoveForward(float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (anim != null) anim.SetBool("IsMoving", true);
    }

    private void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("IsMoving", false);
    }

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

    private void CleanupDeadMinions()
    {
        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            if (activeMinions[i] == null)
                activeMinions.RemoveAt(i);
        }
    }

    private void CreateOverheadBar()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        barRoot = new GameObject("LichBossBar").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = barOffset;

        // BG
        CreateBarLayer("BG", new Vector3(-0.05f, 0f, 0f), new Vector3(2.3f, 0.28f, 1f), new Color(0.06f, 0.05f, 0.1f, 0.9f), pixelSprite, 10);
        // HP Fill (Phase 1: Cyan-Blue, Phase 2: Neon-Violet)
        var hpObj = CreateBarLayer("HPFill", Vector3.zero, new Vector3(2.2f, 0.18f, 1f), new Color(0.2f, 0.85f, 1f, 1f), pixelSprite, 11);
        hpFill = hpObj.transform;
        hpFillSr = hpObj.GetComponent<SpriteRenderer>();
    }

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

    private void UpdateOverheadBar()
    {
        if (hpFill != null && maxBossHealth > 0)
        {
            float ratio = Mathf.Clamp01((float)currentHP / maxBossHealth);
            hpFill.localScale = new Vector3(2.2f * ratio, 0.18f, 1f);
        }
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }
}
