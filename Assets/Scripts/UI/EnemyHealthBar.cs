using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    private EnemyHealth target;
    private float heightOffset = 1.2f;

    private SpriteRenderer borderSr;
    private SpriteRenderer bgSr;
    private SpriteRenderer lagSr;
    private SpriteRenderer fillSr;

    private Transform lagTransform;
    private Transform fillTransform;

    private float maxHp = 1f;
    private float currentHp = 1f;
    private float currentRatio = 1f;
    private float lagRatio = 1f;

    private float lagPauseTimer = 0f;
    private const float LAG_PAUSE_DURATION = 0.35f;
    private const float LAG_DRAIN_SPEED = 1.6f;

    private float combatTimer = 0f;
    private const float COMBAT_DURATION = 4.0f;

    private float currentAlpha = 0f;
    private float targetAlpha = 0f;
    private bool isDead = false;

    private float barWidth = 0.88f;
    private float barHeight = 0.08f;

    public void Initialize(EnemyHealth enemyHealth, float offset)
    {
        target = enemyHealth;
        heightOffset = offset;

        if (target != null)
        {
            if (target.IsBoss)
            {
                barWidth = 1.6f;
                barHeight = 0.14f;
            }
            else
            {
                barWidth = 0.88f;
                barHeight = 0.08f;
            }

            maxHp = Mathf.Max(1, target.MaxHealth);
            currentHp = target.CurrentHealth;
            currentRatio = Mathf.Clamp01(currentHp / maxHp);
            lagRatio = currentRatio;

            target.OnHealthChanged += HandleHealthChanged;
            target.OnDamageTaken += HandleDamageTaken;
            target.OnDeath += HandleDeath;
        }

        CreateVisuals();

        if (target != null && target.IsBoss)
        {
            currentAlpha = 1f;
            targetAlpha = 1f;
            ApplyAlpha(1f);
        }
        else
        {
            ApplyAlpha(0f);
        }
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            target.OnHealthChanged -= HandleHealthChanged;
            target.OnDamageTaken -= HandleDamageTaken;
            target.OnDeath -= HandleDeath;
        }
    }

    private void CreateVisuals()
    {
        // 1. Tạo texture pixel 1x1 làm nguồn sprite
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        // Sprite căn giữa cho Khung & Nền
        Sprite centerSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        // Sprite có Pivot bên trái cho Thanh Máu Co Rút (Fill)
        Sprite leftPivotSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        // a) Viền ngoài (Border Frame)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(transform, false);
        borderObj.transform.localPosition = Vector3.zero;
        borderObj.transform.localScale = new Vector3(barWidth + 0.06f, barHeight + 0.05f, 1f);
        borderSr = borderObj.AddComponent<SpriteRenderer>();
        borderSr.sprite = centerSprite;
        borderSr.color = (target != null && target.IsBoss)
            ? new Color(0.88f, 0.72f, 0.22f, 0.95f) // Viền vàng hoàng kim cho Boss
            : new Color(0.2f, 0.22f, 0.28f, 0.95f);
        borderSr.sortingOrder = 200;

        // b) Nền đen (Background)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = centerSprite;
        bgSr.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        bgSr.sortingOrder = 201;

        // c) Vệt máu trễ (Lag Bar - Hổ phách/Vàng cam)
        GameObject lagObj = new GameObject("LagFill");
        lagObj.transform.SetParent(transform, false);
        lagObj.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        lagObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        lagSr = lagObj.AddComponent<SpriteRenderer>();
        lagSr.sprite = leftPivotSprite;
        lagSr.color = new Color(1.0f, 0.78f, 0.18f, 0.95f);
        lagSr.sortingOrder = 202;
        lagTransform = lagObj.transform;

        // d) Thanh máu chính (Main HP Fill - Đỏ thẫm rực)
        GameObject fillObj = new GameObject("MainFill");
        fillObj.transform.SetParent(transform, false);
        fillObj.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        fillSr = fillObj.AddComponent<SpriteRenderer>();
        fillSr.sprite = leftPivotSprite;
        fillSr.color = new Color(0.92f, 0.18f, 0.22f, 1f);
        fillSr.sortingOrder = 203;
        fillTransform = fillObj.transform;
    }

    private void HandleHealthChanged(int current, int max)
    {
        maxHp = Mathf.Max(1, max);
        currentHp = Mathf.Clamp(current, 0, max);
        float newRatio = Mathf.Clamp01(currentHp / maxHp);

        currentRatio = newRatio;
        lagPauseTimer = LAG_PAUSE_DURATION;

        // Kích hoạt hiển thị thanh máu khi nhận sát thương
        combatTimer = COMBAT_DURATION;
        targetAlpha = 1.0f;
    }

    private void HandleDamageTaken(int damage)
    {
        combatTimer = COMBAT_DURATION;
        targetAlpha = 1.0f;
    }

    private void HandleDeath()
    {
        isDead = true;
        targetAlpha = 0f;
        currentRatio = 0f;
        lagRatio = 0f;
        Destroy(gameObject, 0.4f);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 🔒 CHỐNG QUAY NGƯỢC (Anti-Flip): Giữ thanh máu luôn ở vị trí thế giới độc lập,
        // không bao giờ bị lật ngược khi quái vật quay đầu trái/phải!
        transform.position = target.transform.position + Vector3.up * heightOffset;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Cập nhật tỷ lệ co giãn của thanh máu chính
        if (fillTransform != null)
        {
            fillTransform.localScale = new Vector3(barWidth * currentRatio, barHeight, 1f);
        }

        // Cập nhật hiệu ứng vệt máu trễ (Catch-up Lag Fill)
        if (lagTransform != null)
        {
            if (lagPauseTimer > 0f)
            {
                lagPauseTimer -= Time.deltaTime;
            }
            else if (lagRatio > currentRatio)
            {
                lagRatio = Mathf.MoveTowards(lagRatio, currentRatio, LAG_DRAIN_SPEED * Time.deltaTime);
            }
            else
            {
                lagRatio = currentRatio;
            }

            lagTransform.localScale = new Vector3(barWidth * lagRatio, barHeight, 1f);
        }

        // Bộ đếm thời gian tự ẩn khi rời giao tranh (đối với quái thường)
        if (!isDead && target != null && !target.IsBoss)
        {
            if (combatTimer > 0f)
            {
                combatTimer -= Time.deltaTime;
                if (combatTimer <= 0f && currentRatio >= 1.0f)
                {
                    targetAlpha = 0f; // Ẩn nếu đầy máu và hết giao tranh
                }
            }
        }

        // Fade Alpha mượt mà
        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, 4.0f * Time.deltaTime);
            ApplyAlpha(currentAlpha);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        currentAlpha = alpha;
        if (borderSr != null)
        {
            Color c = borderSr.color; c.a = alpha * 0.95f; borderSr.color = c;
        }
        if (bgSr != null)
        {
            Color c = bgSr.color; c.a = alpha * 0.9f; bgSr.color = c;
        }
        if (lagSr != null)
        {
            Color c = lagSr.color; c.a = alpha * 0.95f; lagSr.color = c;
        }
        if (fillSr != null)
        {
            Color c = fillSr.color; c.a = alpha; fillSr.color = c;
        }
    }
}
