using System.Collections;
using UnityEngine;

public class FallingTrap : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Lượng sát thương gây ra cho người chơi")]
    public int damage = 30;

    [Header("Fall Trigger Settings")]
    [Tooltip("Khoảng cách tối đa người chơi đứng bên dưới để kích hoạt (chiều dọc Y)")]
    public float detectionHeight = 10f;
    [Tooltip("Độ rộng phát hiện người chơi (chiều ngang X)")]
    public float detectionWidth = 2.5f;
    [Tooltip("Thời gian rung cảnh báo trước khi rơi (giây)")]
    public float shakeDuration = 0.4f;
    [Tooltip("Cường độ rung")]
    public float shakeIntensity = 0.08f;
    [Tooltip("Tốc độ rơi (độ nặng của đá)")]
    public float gravityMultiplier = 3.5f;

    [Header("Audio & Cleanup")]
    public AudioClip fallSound;
    public AudioClip hitSound;
    public float destroyDelayAfterGround = 1.2f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Transform player;
    private Collider2D trapCollider;
    private bool isTriggered = false;
    private bool hasDealtDamage = false;
    private bool hasHitGround = false;
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        trapCollider = GetComponent<Collider2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        // Ban đầu đứng yên
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = gravityMultiplier;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Đảm bảo collider là Trigger để XUYÊN QUA người và quái, không bị cản lại
        if (trapCollider != null)
        {
            trapCollider.isTrigger = true;
        }

        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
        }
        else
        {
            KnightController knight = FindFirstObjectByType<KnightController>();
            if (knight != null) player = knight.transform;
        }
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
        }

        // Khi chưa kích hoạt: Quét xem player có đi vào bên dưới bẫy không
        if (!isTriggered)
        {
            if (player != null)
            {
                float diffX = Mathf.Abs(player.position.x - transform.position.x);
                float diffY = transform.position.y - player.position.y;

                if (diffX <= (detectionWidth / 2f) && diffY > 0f && diffY <= detectionHeight)
                {
                    StartCoroutine(TriggerFallSequence());
                }
            }
        }
    }

    IEnumerator TriggerFallSequence()
    {
        isTriggered = true;

        // 1. Rung cảnh báo
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            transform.position = initialPosition + new Vector3(offsetX, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition;

        // Tách khỏi cha để rơi tự do độc lập
        transform.SetParent(null);

        // Đảm bảo là Trigger để rơi xuyên người và quái
        if (trapCollider != null)
            trapCollider.isTrigger = true;

        // 2. Âm thanh rơi
        if (fallSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fallSound);
        }

        // 3. Kích hoạt rơi tự do
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    // Nhận diện khi xuyên qua bất kỳ đối tượng nào
    void OnTriggerEnter2D(Collider2D other)
    {
        HandlePassThrough(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePassThrough(collision.gameObject);
    }

    void HandlePassThrough(GameObject obj)
    {
        if (!isTriggered) return;

        // 1. Xuyên qua quái (Enemy): Bỏ qua hoàn toàn, không cản trở
        if (obj.CompareTag("Enemy") || obj.layer == LayerMask.NameToLayer("Enemy"))
        {
            return;
        }

        // 2. Xuyên qua Player: Gây 30 sát thương (nhưng KHÔNG dừng lại, vẫn tiếp tục rơi)
        if (!hasDealtDamage)
        {
            KnightController knight = obj.GetComponentInParent<KnightController>();
            if (knight != null)
            {
                hasDealtDamage = true;
                knight.TakeDamage(damage);
                Debug.Log($"<color=red>💥 [FallingTrap] Bẫy đá xuyên qua Hiệp Sĩ! Gây {damage} sát thương!</color>");
                PlayHitSound();
            }
            else
            {
                PlayerHealth mageHealth = obj.GetComponentInParent<PlayerHealth>();
                if (mageHealth != null)
                {
                    hasDealtDamage = true;
                    mageHealth.TakeDamage(damage);
                    Debug.Log($"<color=red>💥 [FallingTrap] Bẫy đá xuyên qua Pháp Sư! Gây {damage} sát thương!</color>");
                    PlayHitSound();
                }
                else if (obj.CompareTag("Player"))
                {
                    hasDealtDamage = true;
                    obj.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                    PlayHitSound();
                }
            }
        }

        // 3. Chạm vào sàn đất (Ground): Lúc này mới dừng và vỡ
        if (!hasHitGround && (obj.layer == LayerMask.NameToLayer("Ground") || obj.CompareTag("Ground")))
        {
            hasHitGround = true;
            PlayHitSound();

            // Ngừng rơi và biến mất sau destroyDelayAfterGround
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            Destroy(gameObject, destroyDelayAfterGround);
        }
    }

    void PlayHitSound()
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Vector3 center = transform.position + Vector3.down * (detectionHeight / 2f);
        Gizmos.DrawWireCube(center, new Vector3(detectionWidth, detectionHeight, 0.1f));
    }
}
