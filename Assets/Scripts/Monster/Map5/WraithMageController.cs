using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class WraithMageController : EnemyHealth
{
    public enum MageState
    {
        IdleFly,    // Bay lơ lửng tại chỗ
        Patrol,     // Bay tuần tra nhẹ nhàng giữa các mốc
        Chase,      // Duy trì khoảng cách lý tưởng (Kite) với Hiệp sĩ
        Attack1,    // Bắn đạn ma thuật tử linh (Soul Missile)
        Attack2,    // Triệu hồi cột lửa tử linh (Twin Soul Pillars)
        Escape,     // Dịch chuyển/cuộn áo biến lùi khi Hiệp sĩ áp sát quá gần
        Hurt,
        Die
    }

    [Header("1. Flight & Movement")]
    [SerializeField] private float flySpeed = 2.2f;
    [SerializeField] private float hoverHeight = 1.2f;
    [SerializeField] private float patrolRadius = 5.0f;
    [SerializeField] private LayerMask groundLayer;

    [Header("2. Combat & Ranged Spells")]
    [SerializeField] private float aggroRange = 7.5f;
    [SerializeField] private float deAggroRange = 9.5f;
    [SerializeField] private float idealDistance = 4.5f;    // Khoảng cách giữ an toàn
    [SerializeField] private float escapeTriggerDist = 2.0f; // Bị áp sát dưới khoảng cách này sẽ Escape
    [SerializeField] private float spellCooldown = 2.4f;
    [SerializeField] private int missileDamage = 16;
    [SerializeField] private int pillarDamage = 26;
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private GameObject pillarHazardPrefab;

    [Header("3. Audio (SFX)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip castSound;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Transform player;

    private MageState currentState = MageState.Patrol;
    private int direction = 1;
    private bool isCasting = false;
    private bool isHurt = false;
    private float cooldownTimer = 0f;
    private float hurtTimer = 0f;
    private float escapeCooldownTimer = 0f;

    private float spawnX;
    private float minPatrolX;
    private float maxPatrolX;
    private float hoverOffset = 0f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Bay lơ lửng, không bị trọng lực kéo
        myCollider = GetComponent<Collider2D>();

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

    private void Start()
    {
        spawnX = transform.position.x;
        minPatrolX = spawnX - patrolRadius;
        maxPatrolX = spawnX + patrolRadius;

        FindPlayer();
    }

    private void Update()
    {
        if (isDead) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (escapeCooldownTimer > 0f) escapeCooldownTimer -= Time.deltaTime;

        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f) isHurt = false;
            return;
        }

        if (player == null) FindPlayer();
        if (isCasting) return;

        // Hiệu ứng nhấp nhô lơ lửng (Floating hover)
        hoverOffset = Mathf.Sin(Time.time * 2.5f) * 0.15f;

        float horizontalDist = player != null ? Mathf.Abs(transform.position.x - player.position.x) : float.MaxValue;

        switch (currentState)
        {
            case MageState.Patrol:
                if (horizontalDist <= aggroRange)
                {
                    currentState = MageState.Chase;
                }
                else
                {
                    Patrol();
                }
                break;

            case MageState.Chase:
                if (horizontalDist > deAggroRange)
                {
                    currentState = MageState.Patrol;
                    break;
                }

                FaceTarget(player.position.x);

                // Nếu Hiệp sĩ áp sát quá nguy hiểm -> kích hoạt Escape (dịch chuyển lùi)
                if (horizontalDist < escapeTriggerDist && escapeCooldownTimer <= 0f)
                {
                    TriggerEscape();
                    break;
                }

                // Nếu đang ở cự ly lý tưởng -> Dừng lại và thi triển phép thuật
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
                    // Quá xa -> Bay tới gần hơn
                    FlyTowards(player.position.x, flySpeed);
                }
                else
                {
                    // Quá gần -> Lùi nhẹ ra sau
                    FlyTowards(transform.position.x - direction * 2f, flySpeed * 0.8f);
                }
                break;
        }
    }

    private void Patrol()
    {
        if (transform.position.x <= minPatrolX && direction == -1) direction = 1;
        else if (transform.position.x >= maxPatrolX && direction == 1) direction = -1;

        UpdateFacing();
        rb.linearVelocity = new Vector2(direction * flySpeed * 0.6f, hoverOffset);
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    private void FlyTowards(float targetX, float speed)
    {
        int moveDir = (targetX > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(moveDir * speed, hoverOffset);
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    private void StopFlight()
    {
        rb.linearVelocity = new Vector2(0f, hoverOffset);
        if (animator != null) animator.SetBool("IsMoving", false);
    }

    private void FaceTarget(float targetX)
    {
        int newDir = (targetX > transform.position.x) ? 1 : -1;
        if (newDir != direction)
        {
            direction = newDir;
            UpdateFacing();
        }
    }

    private void UpdateFacing()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }

    private void StartCastSpell()
    {
        isCasting = true;
        PlayCastSound();
        StopFlight();
        cooldownTimer = spellCooldown;

        // Chọn giữa Attack 1 (Bắn đạn Soul Missile) hoặc Attack 2 (Cột lửa ma pháp)
        if (Random.value < 0.65f)
        {
            if (animator != null) animator.SetTrigger("Attack1");
        }
        else
        {
            if (animator != null) animator.SetTrigger("Attack2");
        }
    }

    // Animation Event: Bắn quả cầu ma thuật bay thẳng ngang
    public void PlayCastSound()
    {
        if (castSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(castSound);
        }
    }

    public void LaunchSoulMissile()
    {
        if (missilePrefab != null && player != null)
        {
            float shootDir = (player.position.x >= transform.position.x) ? 1.0f : -1.0f;
            direction = (int)shootDir;
            UpdateFacing();

            // Spawn tại vị trí ngang tầm trượng và bắn thẳng tắp theo trục ngang
            Vector3 spawnPos = transform.position + new Vector3(shootDir * 0.9f, 0.5f, 0);
            GameObject missile = Instantiate(missilePrefab, spawnPos, Quaternion.identity);

            var proj = missile.GetComponent<SoulMissileProjectile>();
            if (proj != null)
            {
                proj.Launch(new Vector2(shootDir, 0f), missileDamage);
            }
        }
    }

    // Animation Event: Triệu hồi 2 cột gai tử linh tại mặt sàn ngay dưới chân Hero
    public void CastTwinSoulPillars()
    {
        if (player != null)
        {
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
                // Fallback nếu chưa gắn prefab
                StartCoroutine(DamagePillarDelayed(targetGroundPos.x));
            }
        }
    }

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

    public void FinishSpell()
    {
        isCasting = false;
    }

    private void TriggerEscape()
    {
        escapeCooldownTimer = 4.0f;
        if (animator != null) animator.SetTrigger("Escape");
        rb.linearVelocity = new Vector2(-direction * 5.5f, 0.5f);
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;
        base.TakeDamage(damage);

        isHurt = true;
        hurtTimer = 0.25f;
        isCasting = false;

        if (player != null)
        {
            currentState = MageState.Chase;
            int hitFrom = (player.position.x > transform.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(-hitFrom * 2.0f, 1.0f);
        }
    }

    protected override void Die()
    {
        base.Die();
        StopFlight();
        rb.simulated = false;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.5f);
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }
}
