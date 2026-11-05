using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Quái thường đi bộ: Patrol (tuần tra, quay đầu khi gặp vực/tường) -> Chase (đuổi Player)
/// -> Attack (cận chiến) -> Hurt (khựng + bị đẩy lùi) -> Death.
/// Điều khiển animation bằng tên state (Animator.Play) nên Animator KHÔNG cần parameter hay transition.
/// State nào không có trong Animator thì được bỏ qua (quái vẫn chạy bình thường, chỉ không có animation).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(EnemyHealth))]
public class WalkerEnemyController : MonoBehaviour
{
    private enum State { Patrol, Chase, Attack, Hurt, Dead }

    [Header("Facing")]
    [Tooltip("Sprite gốc quay mặt sang PHẢI thì tick. Quay mặt sang TRÁI thì bỏ tick.")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Animator State Names (state nào không có sẽ bị bỏ qua)")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string attackState = "Attack";
    [SerializeField] private string hurtState = "Hurt";
    [SerializeField] private string deathState = "Death";
    [Tooltip("Thời gian chờ thay cho animation khi Animator thiếu state tương ứng (giây)")]
    [SerializeField] private float fallbackAnimDuration = 0.4f;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [Tooltip("Đi tối đa bao xa mỗi bên so với chỗ xuất hiện (đơn vị thế giới)")]
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float patrolPause = 1f;

    [Header("Detection")]
    [SerializeField] private float detectRange = 6f;
    [Tooltip("Xa hơn mức này thì quái bỏ đuổi, quay lại tuần tra")]
    [SerializeField] private float loseRange = 9f;
    [Tooltip("Chênh lệch độ cao tối đa để quái nhìn thấy Player")]
    [SerializeField] private float verticalTolerance = 1.5f;
    [SerializeField] private float chaseSpeed = 2.5f;

    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [Tooltip("Khoảng cách (đơn vị cục bộ, tự nhân theo scale) để bắt đầu ra đòn")]
    [SerializeField] private float attackRange = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float hitAtNormalizedTime = 0.5f;
    [Tooltip("Tâm hitbox so với quái (x dương = phía trước mặt). Đơn vị cục bộ, tự nhân theo scale.")]
    [SerializeField] private Vector2 hitboxOffset = new Vector2(0.5f, 0.2f);
    [SerializeField] private Vector2 hitboxSize = new Vector2(0.8f, 0.5f);
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Hurt")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 lần khựng lại (giây)")]
    [SerializeField] private float hurtCooldown = 0.4f;
    [SerializeField] private float knockbackSpeed = 2.5f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Terrain Check (quay đầu khi gặp vực / tường)")]
    [Tooltip("Layer của mặt đất. Để Nothing thì quái không kiểm tra vực/tường (chỉ quay ở giới hạn tuần tra).")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeCheckForward = 0.5f;
    [SerializeField] private float ledgeCheckHeight = 0.3f;
    [SerializeField] private float ledgeCheckDepth = 0.8f;
    [SerializeField] private float wallCheckHeight = 0.3f;
    [SerializeField] private float wallCheckDistance = 0.5f;

    [Header("Death")]
    [Tooltip("Hủy quái sau bao nhiêu giây khi chết. Số âm = giữ xác lại.")]
    [SerializeField] private float destroyAfterDeath = 1.5f;
    [Tooltip("Vật phẩm rơi khi chết (ví dụ prefab HealthPickup)")]
    [SerializeField] private GameObject dropPrefab;
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.25f;

    private Rigidbody2D rb;
    private Animator animator;
    private EnemyHealth health;

    private Transform target;
    private PlayerHealth targetHealth;
    private float nextFindTime;

    private State state = State.Patrol;
    private Coroutine busyRoutine;
    private string currentAnimName;

    private float moveX;
    private float baseScaleX;
    private float spawnX;
    private float patrolWaitUntil;
    private float nextAttackTime;
    private float nextHurtTime;
    private float knockbackUntil;
    private float knockbackVelocity;

    // Hướng quái đang nhìn trong thế giới: +1 = phải, -1 = trái
    private float Facing => Mathf.Sign(transform.localScale.x) * (spriteFacesRight ? 1f : -1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        baseScaleX = Mathf.Abs(transform.localScale.x);
        spawnX = transform.position.x;

        if (groundLayer.value == 0)
        {
            Debug.LogWarning(
                $"{name}: Chưa chọn Ground Layer nên quái không nhận biết vực/tường.", this);
        }
    }

    private void OnEnable()
    {
        //health.OnDamaged += HandleDamaged;
        //health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        //health.OnDamaged -= HandleDamaged;
        //health.OnDied -= HandleDied;
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
        bool hasTarget = target != null && !targetHealth.IsDead;

        switch (state)
        {
            case State.Patrol: UpdatePatrol(hasTarget); break;
            case State.Chase: UpdateChase(hasTarget); break;
            // Attack và Hurt do coroutine điều khiển
        }
    }

    private void FixedUpdate()
    {
        if (state == State.Dead)
            return;

        float velocityX = Time.time < knockbackUntil ? knockbackVelocity : moveX;
        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
    }

    private void UpdatePatrol(bool hasTarget)
    {
        if (hasTarget && PlayerInDetectRange())
        {
            state = State.Chase;
            return;
        }

        // Đang đứng nghỉ ở đầu mút tuần tra
        if (Time.time < patrolWaitUntil)
        {
            moveX = 0f;
            PlayLoop(idleState);
            return;
        }

        float x = transform.position.x;
        bool atLimit = (Facing > 0f && x >= spawnX + patrolDistance) ||
                       (Facing < 0f && x <= spawnX - patrolDistance);

        if (atLimit || BlockedAhead())
        {
            SetFacing(-Facing);
            patrolWaitUntil = Time.time + patrolPause;
            moveX = 0f;
            PlayLoop(idleState);
            return;
        }

        moveX = Facing * patrolSpeed;
        PlayLoop(walkState);
    }

    private void UpdateChase(bool hasTarget)
    {
        if (!hasTarget || PlayerOutOfLoseRange())
        {
            state = State.Patrol;
            patrolWaitUntil = Time.time + 0.5f;
            moveX = 0f;
            return;
        }

        FaceTarget();

        float dx = target.position.x - transform.position.x;
        float range = attackRange * Mathf.Abs(transform.lossyScale.x);

        if (Mathf.Abs(dx) > range)
        {
            // Không đuổi tới mức rơi xuống vực hoặc đâm vào tường
            if (BlockedAhead())
            {
                moveX = 0f;
                PlayLoop(idleState);
            }
            else
            {
                moveX = Mathf.Sign(dx) * chaseSpeed;
                PlayLoop(walkState);
            }
            return;
        }

        // Trong tầm đánh
        moveX = 0f;
        PlayLoop(idleState);

        if (Time.time >= nextAttackTime && VerticalOk())
            StartBusy(AttackRoutine());
    }

    // ------------------------------------------------------------------ Routines

    private IEnumerator AttackRoutine()
    {
        state = State.Attack;
        moveX = 0f;
        FaceTarget();

        yield return PlayOneShot(attackState, hitAtNormalizedTime, DealAttackDamage);

        nextAttackTime = Time.time + attackCooldown;
        EndBusy(State.Chase);
    }

    private IEnumerator HurtRoutine()
    {
        state = State.Hurt;
        moveX = 0f;

        yield return PlayOneShot(hurtState);

        // Bị đánh từ xa cũng quay sang đuổi Player (nếu quá loseRange thì tự bỏ đuổi)
        EndBusy(target != null ? State.Chase : State.Patrol);
    }

    private IEnumerator DeathRoutine()
    {
        if (HasState(deathState))
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

        // Thiếu state trong Animator: chờ thời gian dự phòng, vẫn gọi onHit ở giữa
        if (!HasState(stateName))
        {
            if (onHit != null)
            {
                yield return new WaitForSeconds(fallbackAnimDuration * 0.5f);
                onHit();
                yield return new WaitForSeconds(fallbackAnimDuration * 0.5f);
            }
            else
            {
                yield return new WaitForSeconds(fallbackAnimDuration);
            }
            yield break;
        }

        animator.Play(stateName, 0, 0f);
        yield return null; // chờ Animator vào state

        bool hitDone = onHit == null || hitAt < 0f;
        float safety = 6f; // chống kẹt

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

    private void EndBusy(State next)
    {
        busyRoutine = null;
        state = next;
        currentAnimName = null; // để vòng Update sau phát lại Idle/Walk
    }

    // ------------------------------------------------------------------ Health events

    private void HandleDamaged(int amount)
    {
        if (state == State.Dead || Time.time < nextHurtTime)
            return;

        nextHurtTime = Time.time + hurtCooldown;

        // Bị đẩy lùi ra xa Player (không biết Player ở đâu thì lùi ngược hướng đang nhìn)
        float awayDirection = target != null
            ? (transform.position.x >= target.position.x ? 1f : -1f)
            : -Facing;
        knockbackVelocity = awayDirection * knockbackSpeed;
        knockbackUntil = Time.time + knockbackDuration;

        StartBusy(HurtRoutine());
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
        knockbackUntil = 0f;
        rb.linearVelocity = Vector2.zero;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        rb.simulated = false;

        if (dropPrefab != null && UnityEngine.Random.value <= dropChance)
            Instantiate(dropPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);

        StartCoroutine(DeathRoutine());
    }

    // ------------------------------------------------------------------ Combat

    private Vector2 GetAttackCenter()
    {
        Vector3 scale = transform.lossyScale;
        return (Vector2)transform.position + new Vector2(
            Facing * hitboxOffset.x * Mathf.Abs(scale.x),
            hitboxOffset.y * Mathf.Abs(scale.y));
    }

    private void DealAttackDamage()
    {
        Vector2 size = new Vector2(
            hitboxSize.x * Mathf.Abs(transform.lossyScale.x),
            hitboxSize.y * Mathf.Abs(transform.lossyScale.y));

        // Knight: khiên hoạt động. Mage: trừ máu trực tiếp.
        PlayerDamage.DamageInBox(GetAttackCenter(), size, 0f, damage);
    }

    // ------------------------------------------------------------------ Helpers

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

    private bool VerticalOk()
    {
        return Mathf.Abs(target.position.y - transform.position.y) <= verticalTolerance;
    }

    private bool PlayerInDetectRange()
    {
        float dx = Mathf.Abs(target.position.x - transform.position.x);
        return dx <= detectRange && VerticalOk();
    }

    private bool PlayerOutOfLoseRange()
    {
        float dx = Mathf.Abs(target.position.x - transform.position.x);
        float dy = Mathf.Abs(target.position.y - transform.position.y);
        return dx > loseRange || dy > verticalTolerance * 2f;
    }

    // Phía trước là tường hoặc là vực (không còn mặt đất)?
    private bool BlockedAhead()
    {
        if (groundLayer.value == 0)
            return false;

        Vector3 scale = transform.lossyScale;
        float sx = Mathf.Abs(scale.x);
        float sy = Mathf.Abs(scale.y);
        Vector2 position = transform.position;

        Vector2 wallOrigin = position + new Vector2(0f, wallCheckHeight * sy);
        if (Physics2D.Raycast(wallOrigin, Vector2.right * Facing, wallCheckDistance * sx, groundLayer))
            return true;

        Vector2 ledgeOrigin = position + new Vector2(Facing * ledgeCheckForward * sx, ledgeCheckHeight * sy);
        return !Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDepth * sy, groundLayer);
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

        // direction: hướng muốn nhìn (+1 phải, -1 trái). Đổi sang dấu scale theo hướng gốc của sprite.
        Vector3 scale = transform.localScale;
        scale.x = baseScaleX * direction * (spriteFacesRight ? 1f : -1f);
        transform.localScale = scale;
    }

    private bool HasState(string stateName)
    {
        return animator.runtimeAnimatorController != null &&
               animator.HasState(0, Animator.StringToHash(stateName));
    }

    private void PlayLoop(string stateName)
    {
        if (currentAnimName == stateName)
            return;

        currentAnimName = stateName;

        if (HasState(stateName))
            animator.CrossFadeInFixedTime(stateName, 0.08f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 scale = transform.lossyScale;
        float sx = Mathf.Abs(scale.x);
        float sy = Mathf.Abs(scale.y);
        Vector3 p = transform.position;

        // Hitbox đòn đánh (đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetAttackCenter(), new Vector3(hitboxSize.x * sx, hitboxSize.y * sy, 0f));

        // Tầm bắt đầu ra đòn (vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p + Vector3.left * attackRange * sx, p + Vector3.right * attackRange * sx);

        // Tầm phát hiện (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p + Vector3.left * detectRange, p + Vector3.right * detectRange);

        // Giới hạn tuần tra (xanh lá): chỗ xuất hiện khi Play, vị trí hiện tại khi đang chỉnh trong Editor
        float center = Application.isPlaying ? spawnX : p.x;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(center - patrolDistance, p.y, 0f), new Vector3(center + patrolDistance, p.y, 0f));

        // Tia kiểm tra tường và vực (xanh dương)
        Gizmos.color = Color.blue;
        float facing = Facing;
        Vector3 wallOrigin = p + new Vector3(0f, wallCheckHeight * sy, 0f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector3.right * facing * wallCheckDistance * sx);
        Vector3 ledgeOrigin = p + new Vector3(facing * ledgeCheckForward * sx, ledgeCheckHeight * sy, 0f);
        Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector3.down * ledgeCheckDepth * sy);
    }
}
