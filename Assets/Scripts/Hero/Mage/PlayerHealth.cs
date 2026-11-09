using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool enableDamageTest = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private Animator animator;
    private bool hasIsDeadBool;
    private bool hasDeadTrigger;
    private bool hasHurtTriggerUpper;
    private bool hasHurtTriggerLower;
    private bool deathTriggered;
    private bool gameOverShown;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "IsDead" && param.type == AnimatorControllerParameterType.Bool)
                    hasIsDeadBool = true;
                else if (param.name == "dead" && param.type == AnimatorControllerParameterType.Trigger)
                    hasDeadTrigger = true;
                else if (param.name == "Hurt" && param.type == AnimatorControllerParameterType.Trigger)
                    hasHurtTriggerUpper = true;
                else if (param.name == "hurt" && param.type == AnimatorControllerParameterType.Trigger)
                    hasHurtTriggerLower = true;
            }

            if (hasIsDeadBool)
                animator.SetBool("IsDead", false);
        }
    }

    private void Update()
    {
        if (!enableDamageTest || IsDead)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            TakeDamage(20);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}", this);

        if (IsDead)
        {
            Die();
        }
        else
        {
            if (hasHurtTriggerUpper)
                animator.SetTrigger("Hurt");
            else if (hasHurtTriggerLower)
                animator.SetTrigger("hurt");
        }
    }

    public void Die()
    {
        if (deathTriggered) return;
        deathTriggered = true;

        currentHealth = 0;

        // Kích hoạt animation chết phù hợp với nhân vật
        if (hasIsDeadBool)
            animator.SetBool("IsDead", true);
        if (hasDeadTrigger)
            animator.SetTrigger("dead");

        // Đổi tag để quái lập tức ngừng nhắm bắn và bỏ qua xác nhân vật
        gameObject.tag = "Untagged";

        // Khóa vận tốc ngang để xác không trượt
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Tắt di chuyển/điều khiển của Knight hoặc Mage
        KnightController knight = GetComponent<KnightController>();
        if (knight != null)
        {
            knight.Die();
        }

        PlayerMovement mageMovement = GetComponent<PlayerMovement>();
        if (mageMovement != null)
        {
            mageMovement.enabled = false;
        }

        // Dự phòng: nếu Animation Event không kích hoạt sau 1.2s thì vẫn hiện Game Over UI
        StartCoroutine(DeathTimeoutFallback());
    }

    public bool Heal(int amount)
    {
        if (IsDead || amount <= 0 || currentHealth >= maxHealth)
            return false;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        return true;
    }

    private IEnumerator DeathTimeoutFallback()
    {
        yield return new WaitForSeconds(1.2f);
        if (IsDead && !gameOverShown)
        {
            TriggerGameOverUI();
        }
    }

    public void OnDeathAnimationFinished()
    {
        if (!IsDead || gameOverShown) return;
        StartCoroutine(ShowGameOverWithDelay(0.4f));
    }

    private IEnumerator ShowGameOverWithDelay(float delay)
    {
        if (gameOverShown) yield break;
        yield return new WaitForSeconds(delay);
        TriggerGameOverUI();
    }

    private void TriggerGameOverUI()
    {
        if (gameOverShown) return;
        gameOverShown = true;

        GameEndUI gameEndUI = FindAnyObjectByType<GameEndUI>(FindObjectsInactive.Include);

        if (gameEndUI != null)
        {
            gameEndUI.ShowLose();
        }
        else
        {
            Debug.LogError("[PlayerHealth] Không tìm thấy GameEndUI!");
        }
    }
}
