using UnityEngine;

public class BossMap4Controller : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("References")]
    public Animator animator;
    public Transform player;
    private Rigidbody2D rb;
    private bool isDead = false;

    [Header("Audio (Dùng 1 Audio Source)")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip takeDamageSound;
    public AudioClip dieSound;
    public AudioClip idleSound;

    // 🔒 CỜ KHÓA TUYỆT ĐỐI: Đảm bảo Idle Sound chỉ kêu đúng 1 LẦN DUY NHẤT
    private bool hasPlayedIdleSound = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

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

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                FacePlayer();
            }

            // 1. Đang đứng khựng chờ sau khi chém -> Idle
            if (Time.time < lastAttackTime + attackCooldown)
            {
                StopMoving();
                PlayIdleSoundOnce();
                return;
            }

            // 2. Logic di chuyển & tấn công
            if (distanceToPlayer <= attackRange)
            {
                Attack();
            }
            else if (distanceToPlayer <= detectionRange)
            {
                MoveTowardsPlayer();
            }
            else
            {
                SetIdle();
                PlayIdleSoundOnce();
            }
        }
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
        animator.SetTrigger("IsATK");
        lastAttackTime = Time.time;

        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("IsWalking", false);
    }

    // 🔊 PHÁT ĐÚNG 1 LẦN DUY NHẤT VÀ KHÓA TẮT HẲN
    void PlayIdleSoundOnce()
    {
        if (audioSource == null || idleSound == null) return;

        // Nếu CHƯA TỪNG phát lần nào -> Phát đúng 1 tiếng rồi khóa vĩnh viễn
        if (!hasPlayedIdleSound)
        {
            audioSource.PlayOneShot(idleSound);
            hasPlayedIdleSound = true; // Khóa cờ ngay lập tức, không bao giờ mở lại
        }
    }

    public void DealDamageToPlayer()
    {
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f)
            {
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void SetIdle()
    {
        StopMoving();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        animator.SetTrigger("IsTakeDmg");
        StopMoving();

        if (takeDamageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(takeDamageSound);
        }

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        StopMoving();
        animator.SetTrigger("IsDie");

        if (dieSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dieSound);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}