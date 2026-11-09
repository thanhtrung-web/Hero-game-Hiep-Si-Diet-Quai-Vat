using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class KnightController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpForce = 7.5f;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private int maxHealth = 300;
    [SerializeField] private float maxStamina = 150f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int attack1Damage = 10;
    [SerializeField] private int attack2Damage = 15;
    [SerializeField] private int attack3Damage = 20;
    [SerializeField] private int runAttackDamage = 20;
    [SerializeField] private float attackCooldown = 0.22f;
    [SerializeField] private float staminaRegenRate = 4f;
    [SerializeField] private float sprintStaminaCost = 10f;

    [Header("Audio (SFX Chém gió & Chiến đấu)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attack1Sound;
    [SerializeField] private AudioClip attack2Sound;
    [SerializeField] private AudioClip attack3Sound;
    [SerializeField] private AudioClip runAttackSound;
    [SerializeField] private AudioClip blockSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;

    private int currentHealth;
    private float currentStamina;
    private bool isStunned;
    private float stunTimer;
    private bool isExhausted; // Khóa chạy khi cạn sạch thể lực, chống giật lùi/flicker
    private float lastAttackTime = -1f;

    // Cho phép các script khác (hoặc UI) đọc máu và thể lực
    public int CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : currentHealth;
    public int MaxHealth => playerHealth != null ? playerHealth.MaxHealth : maxHealth;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    private bool isGrounded;
    private bool isDefending;
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerHealth playerHealth;
    private bool deathHandled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();

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

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        isExhausted = false;
    }

    void Update()
    {
        // Hết máu (PlayerHealth quyết định, hoặc fallback currentHealth) -> xử lý chết 1 lần rồi dừng điều khiển
        if ((playerHealth != null && playerHealth.IsDead) || (playerHealth == null && currentHealth <= 0))
        {
            Die();
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // DEBUG: bấm H để tự trừ 20 máu (bỏ qua khiên) - chỉ có trong Editor / Development Build
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (playerHealth != null)
                playerHealth.TakeDamage(20);
            else
                TakeDamage(20);
        }
#endif

        // 1. Nếu đang bị Vỡ Khiên Choáng -> Khựng lại 1 giây không thể cử động
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
            }
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        HandleDefend();
        HandleMovement();
        HandleJump();
        HandleAttack();
        UpdateAnimation();
        HandleStaminaRegen(); // Hồi thể lực tự nhiên
    }

    private void HandleDefend()
    {
        isDefending = currentStamina > 0 && (Input.GetKey(KeyCode.K) || Input.GetMouseButton(1));
    }

    private void HandleMovement()
    {
        if (isDefending)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Bắt buộc nhả Shift ra mới xóa cờ khóa kiệt sức (muốn chạy lại phải bấm Shift một lần nữa)
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isExhausted = false;
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = moveSpeed;
        bool isMoving = Mathf.Abs(moveInput) > 0.1f;

        // Chỉ cho phép chạy nhanh khi: đang di chuyển, đè Shift, KHÔNG bị khóa kiệt sức
        if (isMoving && Input.GetKey(KeyCode.LeftShift) && !isExhausted)
        {
            if (currentStamina > 0f)
            {
                currentSpeed = sprintSpeed;
                // Trừ thể lực khi chạy nước rút
                currentStamina = Mathf.Max(0f, currentStamina - sprintStaminaCost * Time.deltaTime);

                // Vừa chạm mốc 0 thể lực -> Khóa chạy ngay lập tức, chuyển thẳng về đi bộ
                if (currentStamina <= 0f)
                {
                    isExhausted = true;
                    currentSpeed = moveSpeed;
                }
            }
            else
            {
                // Thể lực = 0 -> Khóa ngay
                isExhausted = true;
                currentSpeed = moveSpeed;
            }
        }

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

        // Tự động xoay mặt theo hướng di chuyển nhưng giữ nguyên 100% kích thước Scale đã chỉnh trong Unity
        if (moveInput > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void HandleStaminaRegen()
    {
        // Chỉ tính là đang chạy nước rút khi: không bị khóa kiệt sức, đang đè Shift và tốc độ thực tế > tốc độ đi bộ
        bool isSprinting = !isExhausted && Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(rb.linearVelocity.x) > moveSpeed + 0.1f;

        // Khi không giơ khiên và không chạy nước rút -> Hồi thể lực (4/s, tương đương 20/5s)
        if (!isDefending && !isSprinting)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (isDefending) return;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void HandleAttack()
    {
        if (isDefending) return;

        // Chỉ nhận khi vừa bấm phím (GetKeyDown / GetMouseButtonDown), tránh giữ chuột gây spam âm thanh liên tục
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;

            // Đòn chém khi đang chạy nhanh
            if (Input.GetKey(KeyCode.LeftShift) && !isExhausted && Mathf.Abs(rb.linearVelocity.x) > 4.5f && isGrounded)
            {
                anim.SetTrigger("runAttack");
                PlaySound(runAttackSound);
                StartCoroutine(DelayedDamage(0.12f, runAttackDamage));
                return;
            }

            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Knight_Attack1"))
            {
                // Combo đòn 2 (25 damage)
                anim.SetTrigger("attack2");
                PlaySound(attack2Sound);
                StartCoroutine(DelayedDamage(0.12f, attack2Damage));
            }
            else if (state.IsName("Knight_Attack2"))
            {
                // Combo đòn 3 kết liễu (35 damage)
                anim.SetTrigger("attack3");
                PlaySound(attack3Sound);
                StartCoroutine(DelayedDamage(0.15f, attack3Damage));
            }
            else
            {
                // Đòn chém đầu tiên (20 damage)
                anim.SetTrigger("attack1");
                PlaySound(attack1Sound);
                StartCoroutine(DelayedDamage(0.1f, attack1Damage));
            }
        }
    }

    // Hàm quét trúng quái và trừ máu
    public void DealDamageToEnemies(int damage)
    {
        bool hitAny = false;
        // Tọa độ mũi kiếm vung ra phía trước mặt Hiệp sĩ
        float facingDir = transform.localScale.x != 0 ? Mathf.Sign(transform.localScale.x) : 1f;
        Vector2 attackPos = (Vector2)transform.position + new Vector2(facingDir * 0.9f, 0.2f);

        // Quét tất cả vật thể nằm trong tầm kiếm chém
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, attackRange);
        foreach (Collider2D hit in hits)
        {
            // Kiểm tra nếu trúng Quái (qua Tag Enemy hoặc Layer Enemy)
            if (hit.CompareTag("Enemy") || ((1 << hit.gameObject.layer) & enemyLayer) != 0)
            {
                var enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    hitAny = true;
                    Debug.Log($"<color=yellow>TRÚNG ĐÍCH!</color> Hiệp sĩ chém trúng {hit.name}, gây {damage} sát thương!");
                }
                else
                {
                    // Hỗ trợ các quái không kế thừa EnemyHealth (Map 4 Golem, Boss)
                    var normalEnemy = hit.GetComponentInParent<GroupNormalEnemy>();
                    if (normalEnemy != null)
                    {
                        normalEnemy.TakeDamage(damage);
                        hitAny = true;
                        Debug.Log($"<color=yellow>TRÚNG ĐÍCH!</color> Hiệp sĩ chém trúng {hit.name}, gây {damage} sát thương!");
                    }
                    else
                    {
                        var boss = hit.GetComponentInParent<BossMap4Controller>();
                        if (boss != null)
                        {
                            boss.TakeDamage(damage);
                            hitAny = true;
                            Debug.Log($"<color=yellow>TRÚNG ĐÍCH!</color> Hiệp sĩ chém trúng {hit.name}, gây {damage} sát thương!");
                        }
                    }
                }
            }
        }

        if (hitAny)
            PlaySound(hitSound);
    }

    // Tự động gọi sát thương đồng bộ với vệt chém kiếm
    private System.Collections.IEnumerator DelayedDamage(float delay, int damage)
    {
        yield return new WaitForSeconds(delay);
        DealDamageToEnemies(damage);
    }

    private void UpdateAnimation()
    {
        anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isDefending", isDefending);
    }

    // Sát thương từ quái: xử lý khiên trước, phần còn lại chuyển cho PlayerHealth
    public void TakeDamage(int damage = 10)
    {
        if ((playerHealth != null && playerHealth.IsDead) || (playerHealth == null && currentHealth <= 0)) return;

        // --- TRƯỜNG HỢP 1: ĐANG GIƠ KHIÊN ĐỠ ---
        if (isDefending)
        {
            float staminaCost = damage * 1.2f;

            // KIỂM TRA VỠ KHIÊN (GUARD BREAK):
            // Nếu thể lực còn dưới 5 hoặc không đủ để gánh đòn đánh này
            if (currentStamina < 5f || currentStamina < staminaCost)
            {
                // Khiên bị đánh văng ra, choáng 1 giây!
                isDefending = false;
                isStunned = true;
                stunTimer = 1.0f;
                currentStamina = 0f;

                // Dính 50% sát thương xuyên thủng khiên
                int bleedDamage = Mathf.Max(1, damage / 2);
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(bleedDamage);
                    if (playerHealth.IsDead)
                    {
                        Die();
                        return;
                    }
                }
                else
                {
                    currentHealth = Mathf.Max(0, currentHealth - bleedDamage);
                    if (currentHealth <= 0)
                    {
                        Die();
                        return;
                    }
                }

                PlaySound(hitSound);
                anim.SetTrigger("hurt");

                Debug.Log($"<color=red>VỠ KHIÊN!</color> Hiệp sĩ bị choáng 1s và dính {bleedDamage} sát thương!");
            }
            else
            {
                // ĐỠ ĐÒN THÀNH CÔNG: trừ thể lực, KHÔNG MẤT MÁU!
                PlaySound(blockSound);
                currentStamina = Mathf.Max(0, currentStamina - staminaCost);
                Debug.Log($"Đỡ thành công! Tốn {staminaCost:F1} thể lực, còn: {currentStamina:F1}/{maxStamina}");
            }
            return;
        }

        // --- TRƯỜNG HỢP 2: KHÔNG ĐỠ KHIÊN (BỊ ĐÁNH TRỰC DIỆN) ---
        PlaySound(hitSound);

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            if (playerHealth.IsDead)
            {
                Die();
                return;
            }
        }
        else
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            Debug.Log($"Hiệp sĩ trúng đòn! Mất {damage} máu, còn: {currentHealth}/{maxHealth}");
            if (currentHealth <= 0)
            {
                Die();
                return;
            }
        }

        anim.SetTrigger("hurt");
    }

    // Được gọi khi PlayerHealth báo đã chết (xem Update hoặc TakeDamage)
    public void Die()
    {
        if (deathHandled) return;
        deathHandled = true;

        if (deathSound != null)
            PlaySound(deathSound);

        anim.SetTrigger("dead");
        rb.linearVelocity = Vector2.zero;
        isDefending = false;
        isStunned = false;
        gameObject.tag = "Untagged";
        enabled = false;
    }

    // Nhận Animation Event ở cuối clip Knight_Dead
    public void OnDeathAnimationFinished()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeathAnimationFinished();
        }
    }
}
