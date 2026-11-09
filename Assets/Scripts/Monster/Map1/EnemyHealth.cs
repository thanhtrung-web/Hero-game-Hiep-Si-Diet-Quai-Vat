using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] protected int maxHealth = 3;
    [SerializeField] protected Animator animator;
    [SerializeField] protected Behaviour patrolScript;
    public bool IsDead => isDead;
    

    public virtual bool IsBoss => false;
    public virtual string BossTitle => "Quái Vật";

    // Sự kiện phục vụ hệ thống UI Máu & Số sát thương
    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action<int> OnDamageTaken;        // (damage)
    public event Action OnDeath;

    protected int currentHealth;
    protected bool isDead;

    public void NotifyDamageTaken(int damage)
    {
        OnDamageTaken?.Invoke(damage);
    }

    public void NotifyHealthChanged(int cur, int max)
    {
        OnHealthChanged?.Invoke(cur, max);
    }

    public void NotifyDeath()
    {
        OnDeath?.Invoke();
    }

    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (!IsBoss)
        {
            // Quái thường: Sử dụng thanh máu nổi trên đầu
            EnsureOverheadHealthBar();
        }
        else
        {
            // Trùm (Boss): Chỉ hiển thị thanh máu Đại Trùm ở đỉnh màn hình, xóa bỏ thanh máu trên đầu
            var overheadBars = GetComponentsInChildren<EnemyHealthBar>(true);
            foreach (var b in overheadBars)
            {
                Destroy(b.gameObject);
            }

            EnsureBossHealthBar();
        }
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Kích hoạt sự kiện cho hệ thống UI Máu
        NotifyDamageTaken(damage);
        NotifyHealthChanged(currentHealth, maxHealth);

        // Nổi số sát thương bay lên trên đầu quái (được bọc an toàn tuyệt đối)
        try
        {
            SpawnDamagePopup(damage);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[EnemyHealth] Lỗi hiển thị số sát thương: " + e.Message);
        }

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    public void SpawnDamagePopup(int damage)
    {
        try
        {
            float yOffset = 1.0f;
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                yOffset = col.bounds.extents.y + 0.35f;
            }
            DamagePopupManager.ShowDamage(transform.position + Vector3.up * yOffset, damage, damage >= 20);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[EnemyHealth] SpawnDamagePopup exception: " + ex.Message);
        }
    }

    public void EnsureOverheadHealthBar()
    {
        if (IsBoss) return; // Boss không bao giờ dùng thanh máu trên đầu

        var existing = GetComponentInChildren<EnemyHealthBar>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return;
        }

        GameObject barObj = new GameObject("OverheadHealthBar");
        barObj.transform.SetParent(transform, false);

        float heightOffset = 1.0f;
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            heightOffset = col.bounds.extents.y + 0.35f;
        }
        barObj.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        var hb = barObj.AddComponent<EnemyHealthBar>();
        hb.Initialize(this, heightOffset);
    }

    public void EnsureBossHealthBar()
    {
        BossHealthBarUI bossBar = BossHealthBarUI.Instance != null
            ? BossHealthBarUI.Instance
            : FindAnyObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);

        if (bossBar == null)
        {
            GameObject bossBarObj = new GameObject("BossHealthBarUI");
            bossBar = bossBarObj.AddComponent<BossHealthBarUI>();
        }

        if (bossBar != null)
        {
            bossBar.BindBoss(this);
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        NotifyDeath();

        if (animator != null)
            animator.SetTrigger("Dead");

        if (patrolScript != null)
            patrolScript.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Destroy(gameObject, 1.5f);
    }
}
