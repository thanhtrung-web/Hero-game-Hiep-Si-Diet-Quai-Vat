using UnityEngine;

public class BossMap4Controller : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float detectionRange = 10f;
    public float attackDamage = 20f;

    [Header("References")]
    public Animator animator;
    public Transform player;
    private Rigidbody2D rb;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        
        if (animator == null)
            animator = GetComponent<Animator>();

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

            // LUÔN LUÔN xoay mặt về phía Player bất kể đang đứng, đi hay đánh
            FacePlayer();

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
            }
        }
    }

    // Hàm chuyên trách việc xoay mặt theo Player
    void FacePlayer()
    {
        float directionX = player.position.x - transform.position.x;

        if (directionX > 0)
        {
            // Quay mặt sang phải
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX < 0)
        {
            // Quay mặt sang trái
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        animator.SetBool("IsWalking", true);
        animator.SetBool("IsATK", false);
    }

    void Attack()
    {
        rb.linearVelocity = Vector2.zero; // Dừng di chuyển khi đánh
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsATK", true);
    }

    public void DealDamageToPlayer()
    {
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f) 
            {
                Debug.Log("Boss đã đánh trúng Player gây " + attackDamage + " sát thương!");
            }
        }
    }

    void SetIdle()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsATK", false);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        animator.SetTrigger("IsTakeDmg");
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("Die");
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 3f);
    }
}