using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CultistMobController : EnemyHealth
{
    public enum CultistState
    {
        Patrol,
        Chase,
        Attacking,
        Escaping,
        Spawning
    }

    [Header("1. Movement & Patrol")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float chaseSpeed = 2.6f;
    [SerializeField] private float patrolRadius = 4.0f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeCheckDistance = 0.7f;
    [SerializeField] private float groundCheckDepth = 1.0f;
    [SerializeField] private float wallCheckDistance = 0.6f;

    [Header("2. Combat & Skills")]
    [SerializeField] private int cultistHealth = 80;
    [SerializeField] private float aggroRange = 6.0f;
    [SerializeField] private float deAggroRange = 8.5f;
    [SerializeField] private float meleeRange = 1.6f;
    [SerializeField] private float spellRange = 4.5f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private int meleeDamage = 12;
    [SerializeField] private int spellDamage = 16;
    [SerializeField] private float escapeHealthPercent = 0.35f;

    [Header("3. Overhead Health Bar")]
    [SerializeField] private Vector3 barOffset = new Vector3(-0.6f, 1.8f, 0f);

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D myCollider;
    private Transform player;

    private CultistState state = CultistState.Patrol;
    private int direction = 1; // 1 = right, -1 = left
    private bool isAttacking = false;
    private bool hasEscaped = false;
    private float cooldownTimer = 0f;
    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;

    // Overhead Mini Health Bar
    private Transform barRoot;
    private Transform hpFill;

    protected override void Awake()
    {
        maxHealth = cultistHealth;
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        spawnX = transform.position.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
        CreateOverheadBar();
    }

    private void Update()
    {
        if (isDead) return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                Patrol();
                return;
            }
        }

        if (state == CultistState.Escaping || state == CultistState.Spawning)
            return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        float xDist = Mathf.Abs(transform.position.x - player.position.x);

        switch (state)
        {
            case CultistState.Patrol:
                if (distToPlayer <= aggroRange)
                {
                    state = CultistState.Chase;
                }
                else
                {
                    Patrol();
                }
                break;

            case CultistState.Chase:
                if (distToPlayer > deAggroRange)
                {
                    state = CultistState.Patrol;
                    break;
                }

                FaceTarget(player.position.x);

                // Check escape condition (low HP escape retreat)
                if (!hasEscaped && currentHealth <= maxHealth * escapeHealthPercent)
                {
                    StartCoroutine(ExecuteEscapeRoutine());
                    return;
                }

                // In attack range
                if (cooldownTimer <= 0f && !isAttacking)
                {
                    if (xDist <= meleeRange)
                    {
                        PerformAttack1_Claw();
                    }
                    else if (xDist <= spellRange)
                    {
                        PerformAttack2_BloodCloak();
                    }
                    else
                    {
                        MoveTowardPlayer();
                    }
                }
                else if (!isAttacking)
                {
                    MoveTowardPlayer();
                }
                break;

            case CultistState.Attacking:
                // Handled by animation events
                break;
        }
    }

    private void MoveTowardPlayer()
    {
        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

        if (groundHit.collider != null)
        {
            rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("IsMoving", true);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("IsMoving", false);
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

        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(direction * ledgeCheckDistance, 0.1f);
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDepth, groundLayer);

        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.5f);
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, new Vector2(direction, 0f), wallCheckDistance, groundLayer);

        if (groundHit.collider == null || wallHit.collider != null)
        {
            Flip();
        }
        else
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("IsMoving", true);
        }
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

    private void PerformAttack1_Claw()
    {
        isAttacking = true;
        state = CultistState.Attacking;
        cooldownTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetTrigger("Attack1");
        }
    }

    private void PerformAttack2_BloodCloak()
    {
        isAttacking = true;
        state = CultistState.Attacking;
        cooldownTimer = attackCooldown + 0.5f;
        rb.linearVelocity = Vector2.zero;
        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetTrigger("Attack2");
        }
    }

    // Called via Animation Event on Cultist_Attack1
    public void DealMeleeDamage()
    {
        if (isDead) return;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * 1.2f, 0.5f);
        ApplyDamageInRadius(hitCenter, 1.2f, meleeDamage);
    }

    // Called via Animation Event on Cultist_Attack2
    public void DealSpellDamage()
    {
        if (isDead) return;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(direction * 2.2f, 0.5f);
        ApplyDamageInRadius(hitCenter, 2.0f, spellDamage);
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

    // Called via Animation Event when attack concludes
    public void FinishAttack()
    {
        isAttacking = false;
        state = CultistState.Chase;
    }

    private IEnumerator ExecuteEscapeRoutine()
    {
        hasEscaped = true;
        state = CultistState.Escaping;
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetTrigger("Escape");

        // Wait for sink animation
        yield return new WaitForSeconds(0.8f);

        // Reposition further away in opposite direction of player
        float escapeDir = (player != null && player.position.x > transform.position.x) ? -1f : 1f;
        float newX = transform.position.x + escapeDir * 5.5f;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        state = CultistState.Spawning;
        if (anim != null) anim.SetTrigger("Spawn");

        yield return new WaitForSeconds(0.8f);
        state = CultistState.Chase;
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

        barRoot = new GameObject("CultistBar").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = barOffset;

        // BG (Dark crimson)
        CreateBarLayer("BG", new Vector3(-0.02f, 0f, 0f), new Vector3(1.24f, 0.16f, 1f), new Color(0.12f, 0.04f, 0.08f, 0.85f), pixelSprite, 10);
        // HP Fill (Bright crimson-gold)
        var hpObj = CreateBarLayer("HPFill", Vector3.zero, new Vector3(1.2f, 0.12f, 1f), new Color(0.95f, 0.2f, 0.25f, 1f), pixelSprite, 11);
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
