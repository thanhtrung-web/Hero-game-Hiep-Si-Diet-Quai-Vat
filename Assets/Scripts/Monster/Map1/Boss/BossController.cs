using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// FSM của Boss: Idle -> Chase (Walk) -> Attack (random Attack1-4) -> Special (gọi minion) -> Hurt -> Death.
/// Điều khiển animation bằng tên state (Animator.Play) nên Animator KHÔNG cần parameter hay transition.
/// Boss lật hướng bằng localScale.x nên collider con cũng lật theo. Hướng gốc của sprite chỉnh ở Sprite Faces Right.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(BossHealth))]
public class BossController : MonoBehaviour
{
    private enum State { Idle, Chase, Attack, Special, Hurt, Dead }

    [Serializable]
    public class BossAttack
    {
        [Tooltip("Tên state trong Animator")]
        public string stateName = "Attack1";
        public int damage = 15;
        [Tooltip("Thời điểm gây sát thương trong animation (0 = đầu, 1 = cuối)")]
        [Range(0f, 1f)] public float hitAtNormalizedTime = 0.65f;
        [Tooltip("Tâm hitbox so với boss (x dương = phía trước mặt). Đơn vị cục bộ, tự nhân theo scale.")]
        public Vector2 hitboxOffset = new Vector2(0.7f, 0.2f);
        public Vector2 hitboxSize = new Vector2(0.9f, 0.5f);
        [Tooltip("Thời gian nghỉ sau đòn này trước khi ra đòn tiếp (giây)")]
        public float cooldown = 1.2f;
    }

    [Header("Animator State Names (phải trùng tên state trong Animator)")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string hurtState = "Hurt";
    [SerializeField] private string deathState = "Death";
    [SerializeField] private string specialState = "Special";

    [Header("Facing")]
    [Tooltip("Sprite gốc quay mặt sang PHẢI thì tick. Sprite của boss này quay mặt sang TRÁI nên để tắt.")]
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [Tooltip("Khoảng cách (đơn vị cục bộ) từ boss tới Player để bắt đầu ra đòn")]
    [SerializeField] private float attackRange = 1.2f;

    [Header("Attacks (random, không lặp liên tiếp)")]
    [SerializeField]
    private BossAttack[] attacks =
    {
        // Attack1, Attack2, Attack4: đòn thò đầu ra cắn (cắn ở frame 5/6, 5/6, 4/6)
        new BossAttack { stateName = "Attack1", damage = 15, hitAtNormalizedTime = 0.65f, hitboxOffset = new Vector2(0.7f, 0.2f),  hitboxSize = new Vector2(0.9f, 0.5f), cooldown = 1.2f },
        new BossAttack { stateName = "Attack2", damage = 18, hitAtNormalizedTime = 0.65f, hitboxOffset = new Vector2(0.75f, 0.2f), hitboxSize = new Vector2(1.0f, 0.5f), cooldown = 1.2f },
        // Attack3: đòn lăn/đè người, hitbox ôm quanh thân
        new BossAttack { stateName = "Attack3", damage = 20, hitAtNormalizedTime = 0.5f,  hitboxOffset = new Vector2(0.2f, 0.2f),  hitboxSize = new Vector2(1.2f, 0.6f), cooldown = 1.5f },
        new BossAttack { stateName = "Attack4", damage = 25, hitAtNormalizedTime = 0.5f,  hitboxOffset = new Vector2(0.8f, 0.2f),  hitboxSize = new Vector2(1.1f, 0.5f), cooldown = 1.6f },
    };

    [Header("Hurt (Stagger)")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 lần bị choáng. Giữa 2 lần đó boss vẫn nhận sát thương nhưng không khựng lại.")]
    [SerializeField] private float staggerCooldown = 4f;
    [Tooltip("Có bị ngắt đòn đang ra khi trúng đòn không (mặc định: boss có giáp, không bị ngắt)")]
    [SerializeField] private bool staggerWhileAttacking = false;

    [Header("Special (gọi Minion)")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private Transform[] minionSpawnPoints;
    [SerializeField] private int minionsPerCast = 2;
    [SerializeField] private int maxAliveMinions = 4;
    [SerializeField] private float specialCooldown = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float specialHitAtNormalizedTime = 0.5f;
    [SerializeField] private bool specialOnlyInPhase2 = true;

    [Header("Phase 2")]
    [SerializeField] private float phase2SpeedMultiplier = 1.4f;
    [SerializeField] private float phase2CooldownMultiplier = 0.7f;

    [Header("Arena")]
    [Tooltip("Collider2D (nên là BoxCollider2D, bật Is Trigger) phủ khu vực Boss. Boss không đi ra ngoài; Player bước vào thì trận đấu bắt đầu.")]
    [SerializeField] private Collider2D arenaBounds;
    [SerializeField] private float arenaMargin = 0.5f;
    [Tooltip("Chỉ dùng khi không gán Arena Bounds: bán kính phát hiện Player")]
    [SerializeField] private float aggroRange = 12f;

    [Header("Death")]
    [Tooltip("Hủy boss sau bao nhiêu giây khi chết. Số âm = giữ xác lại.")]
    [SerializeField] private float destroyAfterDeath = 3f;
    [SerializeField] private bool killMinionsOnDeath = true;

    [Header("Events (nối với Camera Arena, cửa, nhạc boss...)")]
    [SerializeField] private UnityEvent onFightStart;
    [SerializeField] private UnityEvent onDefeated;

    private Rigidbody2D rb;
    private Animator animator;
    private BossHealth health;

    private Transform target;
    private PlayerHealth targetHealth;
    private float nextFindTime;

    private State state = State.Idle;
    private Coroutine busyRoutine;
    private string currentAnimName;
    private bool fightStarted;
    private bool inPhase2;

    private float moveX;
    private float baseScaleX;

    private float nextActionTime;
    private float nextSpecialTime;
    private float nextStaggerTime;
    private int lastAttackIndex = -1;
    private bool warnedNoMinion;

    private readonly List<GameObject> minions = new List<GameObject>();

    // Hướng boss đang nhìn trong thế giới: +1 = phải, -1 = trái (suy ra từ scale và hướng gốc của sprite)
    private float Facing => Mathf.Sign(transform.localScale.x) * (spriteFacesRight ? 1f : -1f);

    private bool IsFree => state == State.Idle || state == State.Chase;
    private float CurrentSpeed => walkSpeed * (inPhase2 ? phase2SpeedMultiplier : 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<BossHealth>();

        baseScaleX = Mathf.Abs(transform.localScale.x);
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnPhase2 += HandlePhase2;
        health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnPhase2 -= HandlePhase2;
        health.OnDied -= HandleDied;
    }

    private void Start()
    {
        currentAnimName = idleState;
    }

    // ------------------------------------------------------------------ FSM

    private void Update()
    {
        if (state == State.Dead)
            return;

        ResolveTarget();

        // Chưa có Player (chưa spawn) hoặc Player đã chết -> đứng yên
        if (target == null || targetHealth.IsDead)
        {
            if (IsFree)
            {
                moveX = 0f;
                PlayLoop(idleState);
            }
            return;
        }

        if (!fightStarted)
        {
            if (!PlayerInArena())
            {
                moveX = 0f;
                PlayLoop(idleState);
                return;
            }
            StartFight();
        }

        if (!IsFree)
            return;

        FaceTarget();

        if (SpecialReady())
        {
            StartBusy(SpecialRoutine());
            return;
        }

        float dx = target.position.x - transform.position.x;
        float range = attackRange * Mathf.Abs(transform.lossyScale.x);

        if (Mathf.Abs(dx) > range)
        {
            state = State.Chase;
            moveX = Mathf.Sign(dx) * CurrentSpeed;
            PlayLoop(walkState);
            return;
        }

        // Trong tầm đánh: đứng yên, hết cooldown thì ra đòn
        state = State.Idle;
        moveX = 0f;
        PlayLoop(idleState);

        if (Time.time >= nextActionTime && attacks != null && attacks.Length > 0)
            StartBusy(AttackRoutine(PickAttack()));
    }

    private void FixedUpdate()
    {
        if (state == State.Dead)
            return;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
        ClampToArena();
    }

    private void StartFight()
    {
        fightStarted = true;
        nextActionTime = Time.time + 0.8f;
        nextSpecialTime = Time.time + specialCooldown * 0.5f;
        onFightStart?.Invoke();
    }

    // ------------------------------------------------------------------ Routines

    private IEnumerator AttackRoutine(BossAttack attack)
    {
        state = State.Attack;
        moveX = 0f;
        FaceTarget();

        yield return PlayOneShot(attack.stateName, attack.hitAtNormalizedTime, () => DealAttackDamage(attack));

        nextActionTime = Time.time + attack.cooldown * (inPhase2 ? phase2CooldownMultiplier : 1f);
        EndBusy();
    }

    private IEnumerator SpecialRoutine()
    {
        state = State.Special;
        moveX = 0f;
        FaceTarget();

        yield return PlayOneShot(specialState, specialHitAtNormalizedTime, SpawnMinions);

        nextSpecialTime = Time.time + specialCooldown;
        nextActionTime = Time.time + 0.6f;
        EndBusy();
    }

    private IEnumerator HurtRoutine()
    {
        state = State.Hurt;
        moveX = 0f;

        yield return PlayOneShot(hurtState);

        nextActionTime = Mathf.Max(nextActionTime, Time.time + 0.3f);
        EndBusy();
    }

    private IEnumerator DeathRoutine()
    {
        animator.Play(deathState, 0, 0f);

        if (destroyAfterDeath >= 0f)
        {
            yield return new WaitForSeconds(destroyAfterDeath);
            Destroy(gameObject);
        }
    }

    // Phát animation 1 lần; gọi onHit đúng thời điểm (normalizedTime) rồi chờ animation kết thúc.
    private IEnumerator PlayOneShot(string stateName, float hitAt = -1f, Action onHit = null)
    {
        currentAnimName = stateName;
        animator.Play(stateName, 0, 0f);
        yield return null; // chờ Animator vào state

        bool hitDone = onHit == null || hitAt < 0f;
        float safety = 6f; // chống kẹt nếu tên state sai

        while (safety > 0f)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsName(stateName))
            {
                if (!hitDone && info.normalizedTime >= hitAt)
                {
                    hitDone = true;
                    onHit();
                }

                if (info.normalizedTime >= 0.98f)
                    break;
            }

            safety -= Time.deltaTime;
            yield return null;
        }

        if (!hitDone)
            onHit();
    }

    private void StartBusy(IEnumerator routine)
    {
        if (busyRoutine != null)
            StopCoroutine(busyRoutine);

        busyRoutine = StartCoroutine(routine);
    }

    private void EndBusy()
    {
        busyRoutine = null;
        state = State.Idle;
        currentAnimName = null; // để vòng Update sau phát lại Idle/Walk
    }

    // ------------------------------------------------------------------ Health events

    private void HandleDamaged(int amount)
    {
        if (state == State.Dead || state == State.Special)
            return;

        if (Time.time < nextStaggerTime)
            return;

        if (state == State.Attack && !staggerWhileAttacking)
            return;

        nextStaggerTime = Time.time + staggerCooldown;
        StartBusy(HurtRoutine());
    }

    private void HandlePhase2()
    {
        inPhase2 = true;
        nextSpecialTime = Time.time; // vào Phase 2 là gọi minion ngay khi rảnh
    }

    private void HandleDied()
    {
        if (state == State.Dead)
            return;

        if (busyRoutine != null)
        {
            StopCoroutine(busyRoutine);
            busyRoutine = null;
        }

        state = State.Dead;
        moveX = 0f;
        rb.linearVelocity = Vector2.zero;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        rb.simulated = false;

        if (killMinionsOnDeath)
        {
            foreach (GameObject minion in minions)
            {
                if (minion != null)
                    Destroy(minion);
            }
            minions.Clear();
        }

        onDefeated?.Invoke();
        StartCoroutine(DeathRoutine());
    }

    // ------------------------------------------------------------------ Combat helpers

    private BossAttack PickAttack()
    {
        int index = UnityEngine.Random.Range(0, attacks.Length);

        // Không lặp lại đòn vừa dùng (nếu có từ 2 đòn trở lên)
        if (attacks.Length > 1 && index == lastAttackIndex)
            index = (index + 1 + UnityEngine.Random.Range(0, attacks.Length - 1)) % attacks.Length;

        lastAttackIndex = index;
        return attacks[index];
    }

    // Tâm hitbox trong thế giới: offset.x dương = phía trước mặt boss (đúng cả khi quay trái/phải)
    private Vector2 GetAttackCenter(BossAttack attack)
    {
        Vector3 scale = transform.lossyScale;
        return (Vector2)transform.position + new Vector2(
            Facing * attack.hitboxOffset.x * Mathf.Abs(scale.x),
            attack.hitboxOffset.y * Mathf.Abs(scale.y));
    }

    private void DealAttackDamage(BossAttack attack)
    {
        Vector2 center = GetAttackCenter(attack);
        Vector2 size = new Vector2(
            attack.hitboxSize.x * Mathf.Abs(transform.lossyScale.x),
            attack.hitboxSize.y * Mathf.Abs(transform.lossyScale.y));

        // Knight: khiên hoạt động. Mage: trừ máu trực tiếp.
        PlayerDamage.DamageInBox(center, size, 0f, attack.damage);
    }

    private bool SpecialReady()
    {
        if (minionPrefab == null)
            return false;

        if (specialOnlyInPhase2 && !inPhase2)
            return false;

        return Time.time >= nextSpecialTime && AliveMinions() < maxAliveMinions;
    }

    private int AliveMinions()
    {
        minions.RemoveAll(m => m == null);
        return minions.Count;
    }

    private void SpawnMinions()
    {
        if (minionPrefab == null)
        {
            if (!warnedNoMinion)
            {
                Debug.LogWarning("BossController: Chưa gán Minion Prefab nên Special không gọi được minion.", this);
                warnedNoMinion = true;
            }
            return;
        }

        int count = Mathf.Min(minionsPerCast, maxAliveMinions - AliveMinions());

        for (int i = 0; i < count; i++)
        {
            Vector3 position = (minionSpawnPoints != null && minionSpawnPoints.Length > 0)
                ? minionSpawnPoints[i % minionSpawnPoints.Length].position
                : transform.position + new Vector3(Facing * (1.5f + i), 0f, 0f);

            minions.Add(Instantiate(minionPrefab, position, Quaternion.identity));
        }
    }

    // ------------------------------------------------------------------ Misc helpers

    private void ResolveTarget()
    {
        if (target != null || Time.time < nextFindTime)
            return;

        nextFindTime = Time.time + 0.5f;

        // Mage và Knight đều có PlayerHealth nên tìm được cả hai (Player spawn lúc runtime)
        PlayerHealth found = FindFirstObjectByType<PlayerHealth>();
        if (found != null)
        {
            targetHealth = found;
            target = found.transform;
        }
    }

    private bool PlayerInArena()
    {
        if (arenaBounds != null)
            return arenaBounds.OverlapPoint(target.position);

        return Vector2.Distance(transform.position, target.position) <= aggroRange;
    }

    private void ClampToArena()
    {
        if (arenaBounds == null)
            return;

        Bounds bounds = arenaBounds.bounds;
        float minX = bounds.min.x + arenaMargin;
        float maxX = bounds.max.x - arenaMargin;
        float x = rb.position.x;

        if (x < minX || x > maxX)
        {
            rb.position = new Vector2(Mathf.Clamp(x, minX, maxX), rb.position.y);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        SetFacing(target.position.x >= transform.position.x ? 1f : -1f);
    }

    private void SetFacing(float direction)
    {
        if (Mathf.Approximately(direction, Facing))
            return;

        // direction: hướng muốn nhìn (+1 phải, -1 trái). Đổi sang dấu của scale theo hướng gốc của sprite.
        Vector3 scale = transform.localScale;
        scale.x = baseScaleX * direction * (spriteFacesRight ? 1f : -1f);
        transform.localScale = scale;
    }

    private void PlayLoop(string stateName)
    {
        if (currentAnimName == stateName)
            return;

        currentAnimName = stateName;
        animator.CrossFadeInFixedTime(stateName, 0.08f);
    }

    private void OnDrawGizmosSelected()
    {
        float sx = Mathf.Abs(transform.lossyScale.x);
        float sy = Mathf.Abs(transform.lossyScale.y);

        // Hitbox từng đòn (đỏ)
        Gizmos.color = Color.red;
        if (attacks != null)
        {
            foreach (BossAttack attack in attacks)
            {
                Vector3 center = GetAttackCenter(attack);
                Gizmos.DrawWireCube(center, new Vector3(attack.hitboxSize.x * sx, attack.hitboxSize.y * sy, 0f));
            }
        }

        // Tầm bắt đầu ra đòn (vàng)
        Gizmos.color = Color.yellow;
        Vector3 p = transform.position;
        float range = attackRange * sx;
        Gizmos.DrawLine(p + Vector3.left * range, p + Vector3.right * range);
    }
}