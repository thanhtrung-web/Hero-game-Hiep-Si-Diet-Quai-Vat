using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GhostKnightMobController : EnemyHealth
{
    public enum GhostState
    {
        HoverPatrol,
        Pursue,
        Attacking,
        Dying
    }

    [Header("1. Floating & Flight")]
    [SerializeField] private float flightSpeed = 2.4f;
    [SerializeField] private float pursueSpeed = 3.4f;
    [SerializeField] private float hoverFrequency = 2.5f;
    [SerializeField] private float hoverAmplitude = 0.25f;
    [SerializeField] private float patrolDistance = 5.0f;

    [Header("2. Combat & Skills")]
    [SerializeField] private int ghostHealth = 90;
    [SerializeField] private float aggroRange = 7.0f;
    [SerializeField] private float deAggroRange = 10.0f;
    [SerializeField] private float lungeRange = 3.8f;
    [SerializeField] private float slashRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private int lungeDamage = 18;
    [SerializeField] private int slashDamage = 20;
    [SerializeField] private float lungeDashSpeed = 8.0f;

    [Header("3. Overhead Health Bar")]
    [SerializeField] private Vector3 barOffset = new Vector3(-0.6f, 1.8f, 0f);

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D myCollider;
    private Transform player;

    private GhostState state = GhostState.HoverPatrol;
    private int direction = 1;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;
    private float startX;
    private float startY;
    private float hoverTimer = 0f;

    // Overhead Mini Health Bar
    private Transform barRoot;
    private Transform hpFill;

    protected override void Awake()
    {
        maxHealth = ghostHealth;
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();

        // Ghost floats in air without gravity
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    private void Start()
    {
        startX = transform.position.x;
        startY = transform.position.y;

        FindPlayer();
        CreateOverheadBar();
    }

    private void Update()
    {
        if (isDead) return;

        hoverTimer += Time.deltaTime * hoverFrequency;
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                HoverPatrol();
                return;
            }
        }

        if (state == GhostState.Attacking || state == GhostState.Dying)
            return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        float xDist = Mathf.Abs(transform.position.x - player.position.x);

        switch (state)
        {
            case GhostState.HoverPatrol:
                if (distToPlayer <= aggroRange)
                {
                    state = GhostState.Pursue;
                }
                else
                {
                    HoverPatrol();
                }
                break;

            case GhostState.Pursue:
                if (distToPlayer > deAggroRange)
                {
                    state = GhostState.HoverPatrol;
                    break;
                }

                FaceTarget(player.position.x);

                if (cooldownTimer <= 0f && !isAttacking)
                {
                    if (xDist <= slashRange)
                    {
                        PerformAttack2_SpinSlash();
                    }
                    else if (xDist <= lungeRange)
                    {
                        StartCoroutine(PerformAttack1_LungeRoutine());
                    }
                    else
                    {
                        FlyTowardPlayer();
                    }
                }
                else if (!isAttacking)
                {
                    FlyTowardPlayer();
                }
                break;
        }
    }

    private void FlyTowardPlayer()
    {
        // Smooth 2D gliding toward player elevation with gentle hovering bob
        float targetY = player.position.y + 0.6f + Mathf.Sin(hoverTimer) * hoverAmplitude;
        Vector2 targetPos = new Vector2(player.position.x, targetY);
        Vector2 moveDir = (targetPos - (Vector2)transform.position).normalized;

        rb.linearVelocity = moveDir * pursueSpeed;
        if (anim != null) anim.SetBool("IsMoving", true);
    }

    private void HoverPatrol()
    {
        // Patrol back and forth in air
        if (transform.position.x >= startX + patrolDistance && direction > 0)
        {
            Flip();
        }
        else if (transform.position.x <= startX - patrolDistance && direction < 0)
        {
            Flip();
        }

        float targetY = startY + Mathf.Sin(hoverTimer) * hoverAmplitude;
        rb.linearVelocity = new Vector2(direction * flightSpeed, (targetY - transform.position.y) * 2f);
        if (anim != null) anim.SetBool("IsMoving", true);
    }

    private void Flip()
    {
        direction = -direction;
        if (sr != null) sr.flipX = direction < 0;
    }

    private void FaceTarget(float targetX)
    {
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) > 0.15f)
        {
            if (diff > 0 && direction < 0)
            {
                direction = 1;
                if (sr != null) sr.flipX = false;
            }
            else if (diff < 0 && direction > 0)
            {
                direction = -1;
                if (sr != null) sr.flipX = true;
            }
        }
    }

    private IEnumerator PerformAttack1_LungeRoutine()
    {
        isAttacking = true;
        state = GhostState.Attacking;
        cooldownTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetTrigger("Attack1");
        }

        // Brief windup then thrust dash
        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = new Vector2(direction * lungeDashSpeed, 0f);

        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = Vector2.zero;
    }

    private void PerformAttack2_SpinSlash()
    {
        isAttacking = true;
        state = GhostState.Attacking;
        cooldownTimer = attackCooldown + 0.4f;
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetTrigger("Attack2");
        }
    }

    // Called via Animation Event on GhostKnight_Attack1
    public void DealLungeDamage()
    {
        if (isDead) return;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * 1.8f, 0.4f);
        ApplyDamageInRadius(hitCenter, 1.4f, lungeDamage);
    }

    // Called via Animation Event on GhostKnight_Attack2
    public void DealSlashDamage()
    {
        if (isDead) return;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * 1.4f, 0.4f);
        ApplyDamageInRadius(hitCenter, 1.6f, slashDamage);
    }

    private void ApplyDamageInRadius(Vector2 center, float radius, int damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.GetComponentInParent<KnightController>() != null || hit.GetComponentInParent<PlayerHealth>() != null)
            {
                var knight = hit.GetComponentInParent<KnightController>();
                if (knight != null) knight.TakeDamage(damage);

                var mage = hit.GetComponentInParent<PlayerHealth>();
                if (mage != null) mage.TakeDamage(damage);
            }
        }
    }

    // Called via Animation Event when attack finishes
    public void FinishAttack()
    {
        isAttacking = false;
        state = GhostState.Pursue;
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;
        base.TakeDamage(damage);
        UpdateOverheadBar();
    }

    protected override void Die()
    {
        isDead = true;
        state = GhostState.Dying;
        rb.linearVelocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;
        if (barRoot != null) Destroy(barRoot.gameObject);
        if (anim != null) anim.SetTrigger("Dead");
    }

    private void CreateOverheadBar()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        barRoot = new GameObject("GhostBar").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = barOffset;

        // BG (Dark navy)
        CreateBarLayer("BG", new Vector3(-0.02f, 0f, 0f), new Vector3(1.24f, 0.16f, 1f), new Color(0.04f, 0.08f, 0.16f, 0.85f), pixelSprite, 10);
        // HP Fill (Luminous Cyan)
        var hpObj = CreateBarLayer("HPFill", Vector3.zero, new Vector3(1.2f, 0.12f, 1f), new Color(0.15f, 0.85f, 1.0f, 1f), pixelSprite, 11);
        hpFill = hpObj.transform;
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
        if (hpFill != null && maxHealth > 0)
        {
            float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
            hpFill.localScale = new Vector3(1.2f * ratio, 0.12f, 1f);
        }
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }
}
