using UnityEngine;

public class BossMap4Controller : EnemyHealth
{
    public override bool IsBoss => true;
    public override string BossTitle => "Băng Quái Tuyết Sơn - Frost Behemoth";

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 3.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 2.2f;
    public float hurtCooldown = 2.0f;
    private float lastAttackTime = -999f;
    private float lastHurtTime = -999f;

    [Header("References")]
    public Transform player;
    private Rigidbody2D rb;
    private CapsuleCollider2D bossCollider;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip takeDamageSound;
    public AudioClip dieSound;
    public AudioClip idleSound;

    private bool hasPlayedIdleSound = false;

    protected override void Awake()
    {
        if (maxHealth <= 3)
        {
            maxHealth = 100;
        }

        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<CapsuleCollider2D>();

        if (rb != null)
        {
            rb.simulated = true;
        }

        if (bossCollider != null && bossCollider.size.x > 3.0f)
        {
            bossCollider.size = new Vector2(2.5f, bossCollider.size.y);
        }

        if (attackRange < 3.2f)
        {
            attackRange = 3.5f;
        }

        if (attackCooldown < 2.1f)
        {
            attackCooldown = 2.2f;
        }

        if (hurtCooldown <= 0.1f)
        {
            hurtCooldown = 2.0f;
        }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (Time.time < lastAttackTime + attackCooldown)
        {
            StopMoving();
            return;
        }

        if (IsPlayerInAttackRange(attackRange))
        {
            FacePlayer();
            Attack();
        }
        else if (distanceToPlayer <= detectionRange)
        {
            FacePlayer();
            PlayIdleSoundOnce();
            MoveTowardsPlayer();
        }
        else
        {
            SetIdle();
        }
    }

    private bool IsPlayerInAttackRange(float range)
    {
        if (player == null) return false;

        float dist2D = Vector2.Distance(transform.position, player.position);
        float distX = Mathf.Abs(transform.position.x - player.position.x);
        float distY = Mathf.Abs(transform.position.y - player.position.y);

        if (bossCollider != null)
        {
            float edgeDist = Mathf.Sqrt(bossCollider.bounds.SqrDistance(player.position));
            if (edgeDist <= 1.2f) return true;
        }

        return dist2D <= range || (distX <= range && distY <= 3.5f);
    }

    void FacePlayer()
    {
        if (player == null) return;
        float directionX = player.position.x - transform.position.x;

        if (directionX > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        animator.SetBool("IsWalking", true);
    }

    void Attack()
    {
        StopMoving();

        lastAttackTime = Time.time;

        animator.ResetTrigger("IsTakeDmg");
        animator.SetTrigger("IsATK");

        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    void StopMoving()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null)
            animator.SetBool("IsWalking", false);
    }

    void PlayIdleSoundOnce()
    {
        if (audioSource == null || idleSound == null || player == null) return;

        if (!hasPlayedIdleSound && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            audioSource.PlayOneShot(idleSound);
            hasPlayedIdleSound = true;
        }
    }

    public void DealDamageToPlayer()
    {
        if (isDead) return;

        if (player != null)
        {
            if (IsPlayerInAttackRange(attackRange + 0.8f))
            {
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void SetIdle()
    {
        StopMoving();
    }

    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        NotifyDamageTaken(damage);
        NotifyHealthChanged(currentHealth, maxHealth);
        SpawnDamagePopup(damage);

        if (currentHealth <= 0)
        {
            NotifyDeath();
            Die();
            return;
        }

        bool isAttackingNow = Time.time < lastAttackTime + 1.1f;
        if (!isAttackingNow && Time.time >= lastHurtTime + hurtCooldown)
        {
            lastHurtTime = Time.time;
            if (animator != null)
                animator.SetTrigger("IsTakeDmg");
            StopMoving();

            if (takeDamageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(takeDamageSound);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(Mathf.RoundToInt(damage));
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        NotifyDeath();
        StopMoving();

        if (animator != null)
        {
            animator.ResetTrigger("IsATK");
            animator.ResetTrigger("IsTakeDmg");
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("IsDie");
        }

        if (dieSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dieSound);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, 3f);
    }
}
