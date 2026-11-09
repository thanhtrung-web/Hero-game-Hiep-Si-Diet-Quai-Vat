using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeroGame.Map3;

public class BossHealthBarUI : MonoBehaviour
{
    private static BossHealthBarUI instance;
    public static BossHealthBarUI Instance => instance;

    [Header("UI References (Tự động khởi tạo nếu để trống)")]
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private Text bossTitleText;
    [SerializeField] private Text bossPhaseText;
    [SerializeField] private Image mainFillImage;
    [SerializeField] private Image lagFillImage;
    [SerializeField] private Text hpValueText;

    private EnemyHealth currentBoss;
    private Transform playerTransform;

    private float maxHp = 1f;
    private float currentHp = 1f;
    private float targetRatio = 1f;
    private float currentFillRatio = 0f;
    private float lagFillRatio = 0f;

    private float lagPauseTimer = 0f;
    private const float LAG_PAUSE_DURATION = 0.45f;
    private const float LAG_DRAIN_SPEED = 1.2f;

    private bool isIntroPlaying = false;
    private bool isVisible = false;
    private bool isDefeated = false;

    private Vector2 hiddenPos = new Vector2(0f, 100f);
    private Vector2 visiblePos = new Vector2(0f, -40f);

    private readonly Color normalBossColor = new Color(0.92f, 0.16f, 0.16f, 1f); // Đỏ thẫm
    private readonly Color enragedBossColor = new Color(0.95f, 0.12f, 0.48f, 1f); // Tím neon nộ / Hắc ám
    private readonly Color lagColor = new Color(1.0f, 0.72f, 0.16f, 1f);         // Vàng hổ phách

    private Sprite whiteSprite;

    private Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D whiteTex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
        }
        return whiteSprite;
    }

    private void EnsureSpritesAssigned()
    {
        Sprite s = GetWhiteSprite();

        if (mainFillImage != null && mainFillImage.sprite == null)
            mainFillImage.sprite = s;

        if (lagFillImage != null && lagFillImage.sprite == null)
            lagFillImage.sprite = s;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        EnsureUIBuilt();
        EnsureSpritesAssigned();
        HideImmediate();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSpritesAssigned();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindBoss();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnbindBoss();
        HideImmediate();
        StartCoroutine(FindBossRoutine());
    }

    private IEnumerator FindBossRoutine()
    {
        // Chờ các đối tượng trong scene thức tỉnh (Awake & Start)
        yield return new WaitForSeconds(0.4f);

        FindPlayer();
        FindAndBindBoss();
    }

    private void FindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            var pHealth = FindAnyObjectByType<PlayerHealth>();
            if (pHealth != null)
                playerTransform = pHealth.transform;
        }
    }

    public void FindAndBindBoss()
    {
        if (currentBoss != null && !currentBoss.IsDead)
            return;

        var allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.IsBoss && !enemy.IsDead)
            {
                BindBoss(enemy);
                break;
            }
        }
    }

    public void BindBoss(EnemyHealth boss)
    {
        if (boss == null || (boss == currentBoss && currentBoss != null && !currentBoss.IsDead)) return;

        UnbindBoss();

        currentBoss = boss;
        maxHp = Mathf.Max(1, boss.MaxHealth);
        currentHp = boss.CurrentHealth;
        targetRatio = Mathf.Clamp01(currentHp / maxHp);

        boss.OnHealthChanged += HandleHealthChanged;
        boss.OnDamageTaken += HandleDamageTaken;
        boss.OnDeath += HandleBossDeath;

        if (bossTitleText != null)
        {
            string title = boss.BossTitle;
            if (string.IsNullOrEmpty(title))
                title = "ĐẠI TRÙM - " + boss.gameObject.name.ToUpper();
            bossTitleText.text = "👑 " + title + " 👑";
        }

        if (bossPhaseText != null)
        {
            bossPhaseText.text = "";
        }

        isDefeated = false;
        EnsureSpritesAssigned();
    }

    private void UnbindBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= HandleHealthChanged;
            currentBoss.OnDamageTaken -= HandleDamageTaken;
            currentBoss.OnDeath -= HandleBossDeath;
            currentBoss = null;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        maxHp = Mathf.Max(1, max);
        currentHp = Mathf.Clamp(current, 0, max);
        targetRatio = Mathf.Clamp01(currentHp / maxHp);

        lagPauseTimer = LAG_PAUSE_DURATION;

        if (!isVisible && !isDefeated)
        {
            TriggerBossEntrance();
        }

        UpdateHpText();
    }

    private void HandleDamageTaken(int damage)
    {
        if (!isVisible && !isDefeated)
        {
            TriggerBossEntrance();
        }
    }

    private void HandleBossDeath()
    {
        if (isDefeated) return;
        isDefeated = true;

        StartCoroutine(BossDefeatedSequence());
    }

    private IEnumerator BossDefeatedSequence()
    {
        targetRatio = 0f;
        lagFillRatio = 0f;

        if (bossPhaseText != null)
        {
            bossPhaseText.text = "⚔️ ĐẠI TRÙM ĐÃ BỊ TIÊU DIỆT! ⚔️";
            bossPhaseText.color = new Color(1f, 0.85f, 0.2f);
        }

        // Hiệu ứng giật rung thanh máu chiến thắng
        if (bannerRoot != null)
        {
            Vector2 origPos = bannerRoot.anchoredPosition;
            float shakeTime = 0.8f;
            while (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                bannerRoot.anchoredPosition = origPos + new Vector2(Random.Range(-5f, 5f), Random.Range(-3f, 3f));
                yield return null;
            }
            bannerRoot.anchoredPosition = origPos;
        }

        yield return new WaitForSeconds(2.0f);

        // Trượt lên và ẩn
        float fadeOutTime = 0.8f;
        float elapsed = 0f;
        Vector2 startPos = bannerRoot.anchoredPosition;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutTime;
            bannerRoot.anchoredPosition = Vector2.Lerp(startPos, hiddenPos, t);
            if (bannerCanvasGroup != null)
                bannerCanvasGroup.alpha = 1f - t;
            yield return null;
        }

        HideImmediate();
        UnbindBoss();
    }

    private void TriggerBossEntrance()
    {
        if (isVisible || isDefeated) return;
        isVisible = true;
        gameObject.SetActive(true);
        StartCoroutine(EntranceAnimationRoutine());
    }

    private IEnumerator EntranceAnimationRoutine()
    {
        isIntroPlaying = true;
        currentFillRatio = 0f;
        lagFillRatio = 0f;
        UpdateBars();

        if (bannerCanvasGroup != null)
            bannerCanvasGroup.alpha = 0f;

        // Trượt từ đỉnh màn hình xuống vị trí chuẩn
        float slideDuration = 0.6f;
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            bannerRoot.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, t);
            if (bannerCanvasGroup != null)
                bannerCanvasGroup.alpha = t;
            yield return null;
        }
        bannerRoot.anchoredPosition = visiblePos;
        if (bannerCanvasGroup != null)
            bannerCanvasGroup.alpha = 1f;

        // Hiệu ứng nạp đầy thanh máu cực ngầu (Epic 0 -> 100% intro fill)
        float fillDuration = 0.85f;
        elapsed = 0f;
        float targetIntroRatio = targetRatio;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fillDuration;
            currentFillRatio = Mathf.Lerp(0f, targetIntroRatio, t);
            lagFillRatio = currentFillRatio;
            UpdateBars();
            UpdateHpText();
            yield return null;
        }

        currentFillRatio = targetRatio;
        lagFillRatio = targetRatio;
        UpdateBars();
        UpdateHpText();

        isIntroPlaying = false;
    }

    private void Update()
    {
        // 1. Kiểm tra Boss nếu chưa liên kết
        if (currentBoss == null || currentBoss.IsDead)
        {
            FindAndBindBoss();
            return;
        }

        // 2. Kiểm tra khoảng cách kích hoạt thanh máu khi lại gần Boss (< 16 mét)
        if (!isVisible && !isDefeated)
        {
            if (playerTransform == null) FindPlayer();

            if (playerTransform != null && currentBoss != null)
            {
                float dist = Vector2.Distance(playerTransform.position, currentBoss.transform.position);
                if (dist <= 16f)
                {
                    TriggerBossEntrance();
                }
            }
        }

        if (!isVisible || isIntroPlaying || isDefeated) return;

        // 3. Cập nhật tỷ lệ máu chính rút dần theo sát thương
        currentFillRatio = Mathf.MoveTowards(currentFillRatio, targetRatio, 4.0f * Time.deltaTime);

        // 4. Cập nhật vệt máu trễ (Lag Bar Catch-up)
        if (lagPauseTimer > 0f)
        {
            lagPauseTimer -= Time.deltaTime;
        }
        else if (lagFillRatio > currentFillRatio)
        {
            lagFillRatio = Mathf.MoveTowards(lagFillRatio, currentFillRatio, LAG_DRAIN_SPEED * Time.deltaTime);
        }
        else
        {
            lagFillRatio = currentFillRatio;
        }

        // 5. Kiểm tra Phase nộ / Enraged để đổi màu thanh máu
        CheckPhaseState();

        UpdateBars();
    }

    private void CheckPhaseState()
    {
        if (currentBoss == null || mainFillImage == null) return;

        bool isEnraged = false;
        string phaseName = "";

        // Kiểm tra Lich Boss Phase 2
        var lich = currentBoss as LichBossController;
        if (lich != null && lich.CurrentPhase == LichBossController.BossPhase.Phase2_DeathLord)
        {
            isEnraged = true;
            phaseName = "🔥 GIAI ĐOẠN 2: THẦN CHẾT TRỖI DẬY 🔥";
        }

        // Kiểm tra Golem Enraged
        var golem = currentBoss as GolemBoss;
        if (golem != null && currentFillRatio <= 0.5f)
        {
            isEnraged = true;
            phaseName = "⚡ CUỒNG NỘ: NĂNG LƯỢNG CỔ ĐẠI BỘC PHÁ ⚡";
        }

        mainFillImage.color = isEnraged ? enragedBossColor : normalBossColor;

        if (bossPhaseText != null)
        {
            bossPhaseText.text = phaseName;
            if (isEnraged)
            {
                bossPhaseText.color = new Color(1f, 0.35f, 0.45f);
            }
        }
    }

    private void UpdateBars()
    {
        EnsureSpritesAssigned();

        float mainRatioClamped = Mathf.Clamp01(currentFillRatio);
        float lagRatioClamped = Mathf.Clamp01(lagFillRatio);

        // 1. Cập nhật thanh máu chính (Đỏ) - cả fillAmount VÀ anchorMax để 100% co rút chuẩn xác
        if (mainFillImage != null)
        {
            mainFillImage.fillAmount = mainRatioClamped;
            RectTransform fillRt = mainFillImage.rectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(mainRatioClamped, 1f);
                fillRt.sizeDelta = Vector2.zero;
                fillRt.anchoredPosition = Vector2.zero;
            }
        }

        // 2. Cập nhật vệt máu trễ (Vàng/Cam)
        if (lagFillImage != null)
        {
            lagFillImage.fillAmount = lagRatioClamped;
            RectTransform lagRt = lagFillImage.rectTransform;
            if (lagRt != null)
            {
                lagRt.anchorMin = Vector2.zero;
                lagRt.anchorMax = new Vector2(lagRatioClamped, 1f);
                lagRt.sizeDelta = Vector2.zero;
                lagRt.anchoredPosition = Vector2.zero;
            }
        }
    }

    private void UpdateHpText()
    {
        if (hpValueText != null)
        {
            int displayHp = Mathf.RoundToInt(currentFillRatio * maxHp);
            int percent = Mathf.RoundToInt(currentFillRatio * 100f);
            hpValueText.text = $"{displayHp} / {Mathf.RoundToInt(maxHp)}  ({percent}%)";
        }
    }

    public void HideImmediate()
    {
        isVisible = false;
        isIntroPlaying = false;
        if (bannerCanvasGroup != null)
            bannerCanvasGroup.alpha = 0f;
        if (bannerRoot != null)
            bannerRoot.anchoredPosition = hiddenPos;
    }

    /// <summary>
    /// Tự động xây dựng toàn bộ hệ thống Canvas & UI Trùm chuẩn uGUI hoàn mỹ nếu chưa có
    /// </summary>
    private void EnsureUIBuilt()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95; // Vẽ đè lên trên bản đồ & dưới Pause Menu

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (bannerRoot != null) return;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Banner Root Container
        GameObject bannerObj = new GameObject("BossBannerContainer");
        bannerObj.transform.SetParent(transform, false);
        bannerRoot = bannerObj.AddComponent<RectTransform>();
        bannerRoot.anchorMin = new Vector2(0.5f, 1f);
        bannerRoot.anchorMax = new Vector2(0.5f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 1f);
        bannerRoot.sizeDelta = new Vector2(620f, 90f);
        bannerRoot.anchoredPosition = hiddenPos;

        bannerCanvasGroup = bannerObj.AddComponent<CanvasGroup>();

        // 2. Tên Boss (Boss Title Header)
        GameObject titleObj = new GameObject("BossTitleText");
        titleObj.transform.SetParent(bannerRoot, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 26f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);

        bossTitleText = titleObj.AddComponent<Text>();
        bossTitleText.font = defaultFont;
        bossTitleText.fontSize = 20;
        bossTitleText.fontStyle = FontStyle.Bold;
        bossTitleText.alignment = TextAnchor.MiddleCenter;
        bossTitleText.color = new Color(1.0f, 0.88f, 0.42f, 1f); // Vàng kim quý phái
        var titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 3. Dòng Phase / Trạng thái nộ (Subtitle)
        GameObject phaseObj = new GameObject("BossPhaseText");
        phaseObj.transform.SetParent(bannerRoot, false);
        RectTransform phaseRect = phaseObj.AddComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0f, 1f);
        phaseRect.anchorMax = new Vector2(1f, 1f);
        phaseRect.pivot = new Vector2(0.5f, 1f);
        phaseRect.sizeDelta = new Vector2(0f, 18f);
        phaseRect.anchoredPosition = new Vector2(0f, -28f);

        bossPhaseText = phaseObj.AddComponent<Text>();
        bossPhaseText.font = defaultFont;
        bossPhaseText.fontSize = 12;
        bossPhaseText.fontStyle = FontStyle.Bold;
        bossPhaseText.alignment = TextAnchor.MiddleCenter;
        bossPhaseText.color = new Color(1f, 0.4f, 0.4f, 1f);
        var phaseOutline = phaseObj.AddComponent<Outline>();
        phaseOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        phaseOutline.effectDistance = new Vector2(1f, -1f);

        // 4. Khung viền thanh máu (Bar Outer Frame)
        GameObject frameObj = new GameObject("HealthBarFrame");
        frameObj.transform.SetParent(bannerRoot, false);
        RectTransform frameRect = frameObj.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0f);
        frameRect.anchorMax = new Vector2(0.5f, 0f);
        frameRect.pivot = new Vector2(0.5f, 0f);
        frameRect.sizeDelta = new Vector2(580f, 28f);
        frameRect.anchoredPosition = new Vector2(0f, 8f);

        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = new Color(0.18f, 0.16f, 0.22f, 0.95f); // Viền obsidian ánh tím

        // 5. Nền đen sâu (Background Track)
        GameObject bgObj = new GameObject("BarBackground");
        bgObj.transform.SetParent(frameRect, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(-4f, -4f);
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.05f, 0.08f, 0.95f);

        Sprite s = GetWhiteSprite();

        // 6. Vệt máu trễ (Lag Bar - Hổ phách rực)
        GameObject lagObj = new GameObject("LagBarFill");
        lagObj.transform.SetParent(bgRect, false);
        RectTransform lagRect = lagObj.AddComponent<RectTransform>();
        lagRect.anchorMin = Vector2.zero;
        lagRect.anchorMax = Vector2.one;
        lagRect.sizeDelta = Vector2.zero;
        lagRect.anchoredPosition = Vector2.zero;

        lagFillImage = lagObj.AddComponent<Image>();
        lagFillImage.sprite = s;
        lagFillImage.type = Image.Type.Filled;
        lagFillImage.fillMethod = Image.FillMethod.Horizontal;
        lagFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        lagFillImage.color = lagColor;

        // 7. Thanh máu chính (Main Health Fill - Đỏ ruby rực rỡ)
        GameObject fillObj = new GameObject("MainBarFill");
        fillObj.transform.SetParent(bgRect, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        mainFillImage = fillObj.AddComponent<Image>();
        mainFillImage.sprite = s;
        mainFillImage.type = Image.Type.Filled;
        mainFillImage.fillMethod = Image.FillMethod.Horizontal;
        mainFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        mainFillImage.color = normalBossColor;

        // 8. Chữ số hiển thị máu (HP Text Center)
        GameObject hpTextObj = new GameObject("HpValueText");
        hpTextObj.transform.SetParent(bgRect, false);
        RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;
        hpTextRect.anchoredPosition = Vector2.zero;

        hpValueText = hpTextObj.AddComponent<Text>();
        hpValueText.font = defaultFont;
        hpValueText.fontSize = 13;
        hpValueText.fontStyle = FontStyle.Bold;
        hpValueText.alignment = TextAnchor.MiddleCenter;
        hpValueText.color = Color.white;
        var hpOutline = hpTextObj.AddComponent<Outline>();
        hpOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        hpOutline.effectDistance = new Vector2(1f, -1f);
    }
}
