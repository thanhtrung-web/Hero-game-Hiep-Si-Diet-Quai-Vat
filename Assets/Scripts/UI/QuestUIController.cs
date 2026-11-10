using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị thanh tiến độ Nhiệm vụ (Quest Banner UI).
/// Hỗ trợ linh hoạt 3 chế độ:
/// 1. BossBarrier: Hiển thị đếm số quái vật thường cần tiêu diệt để mở phong ấn phòng Boss (Map 5 Phase 2).
///    Khi mở phong ấn xong, chuyển sang thông báo tiến vào hạ gục Đại Trùm Lich.
/// 2. Boss: Hiển thị đếm số Boss cần tiêu diệt để mở cổng qua màn (Map 4, Map 5 Phase 1, Map 1).
/// 3. MonsterHunt: Hiển thị đếm số lượng quái vật thường trong các ải dọn lính qua màn.
/// </summary>
public class QuestUIController : MonoBehaviour
{
    public enum QuestTrackingMode
    {
        Auto,           // Tự động nhận diện: BossBarrier -> Boss -> Quái thường
        BossBarrier,    // Theo dõi tiến độ tiêu diệt quái vật mở phong ấn phòng Boss
        Boss,           // Tiêu diệt Boss
        MonsterHunt     // Tiêu diệt toàn bộ quái thường
    }

    [Header("Quest Mode")]
    [Tooltip("Chế độ theo dõi nhiệm vụ (Mặc định Auto sẽ tự nhận diện theo các đối tượng trong Scene)")]
    [SerializeField] private QuestTrackingMode trackingMode = QuestTrackingMode.Auto;

    [Header("Text References")]
    [SerializeField] private TMP_Text questTextTMP;
    [SerializeField] private Text questTextLegacy;

    [Header("Target References")]
    [Tooltip("Phong ấn phòng Boss (nếu có, sẽ tự động liên kết để hiển thị tiến độ diệt quái)")]
    [SerializeField] private BossBarrier bossBarrier;

    [Tooltip("Mục tiêu Boss chỉ định thủ công")]
    [SerializeField] private GameObject bossTarget;

    [Tooltip("Cổng dịch chuyển qua màn")]
    [SerializeField] private GameObject portalObject;
    [SerializeField] private Collider2D portalTrigger;
    [SerializeField] private SpriteRenderer portalVisual;

    [Header("Display Formats - Boss Barrier (Map 5 Phase 2)")]
    [SerializeField] private string barrierProgressFormat = "Cần tiêu diệt {0}/{1} quái để mở phòng Boss";
    [SerializeField] private string barrierUnlockedFormat = "✨ Phong ấn phòng Boss đã mở! Hãy tiêu diệt {0}!";
    [SerializeField] private string bossDefeatedFormat = "🏆 {0} đã bị tiêu diệt! Vượt ải thành công!";

    [Header("Display Formats - Standard Boss / Fallback")]
    [SerializeField] private string progressFormat = "Cần tiêu diệt {0}/{1} quái để vượt ải";
    [SerializeField] private string completedFormat = "Đã tiêu diệt {0}/{1} quái — Mở cổng qua màn!";

    [Header("Colors")]
    [SerializeField] private Color progressColor = Color.white;
    [SerializeField] private Color unlockedColor = new Color(1f, 0.85f, 0.25f, 1f); // Vàng kim rực rỡ
    [SerializeField] private Color completedColor = new Color(0.4f, 1f, 0.4f, 1f);   // Xanh lá hoàn thành

    private readonly List<GameObject> trackedBosses = new List<GameObject>();
    private readonly List<EnemyHealth> trackedRegularEnemies = new List<EnemyHealth>();
    private QuestTrackingMode resolvedMode = QuestTrackingMode.Auto;
    private int lastKilledCount = -999;
    private string lastDisplayedText = "";
    private bool unlocked = false;
    private string bossDisplayName = "Đại Trùm";

    private void Awake()
    {
        if (questTextTMP == null)
            questTextTMP = GetComponentInChildren<TMP_Text>();

        if (questTextLegacy == null)
            questTextLegacy = GetComponentInChildren<Text>();

        AutoAlignTextIntoPanel();
        FindAndLockPortal();
    }

    private void Start()
    {
        ResolveTrackingModeAndTargets();
        RefreshProgress(forceUpdate: true);
    }

    private void Update()
    {
        RefreshProgress(forceUpdate: false);
    }

    /// <summary>
    /// Tự căn chỉnh chữ TextMeshProUGUI vào chính giữa khung nền chữ nhật đen Quest_Panel.
    /// Kích hoạt Auto Sizing để nội dung không bao giờ bị tràn lề hay xuống dòng cụt lủn.
    /// </summary>
    private void AutoAlignTextIntoPanel()
    {
        Image panelImage = GetComponentInChildren<Image>();
        if (panelImage != null)
        {
            // Tối ưu độ mờ khung nền
            if (panelImage.color.a > 0.85f && panelImage.color.r < 0.1f && panelImage.color.g < 0.1f && panelImage.color.b < 0.1f)
            {
                panelImage.color = new Color(0.04f, 0.04f, 0.06f, 0.75f);
            }

            if (questTextTMP != null)
            {
                if (questTextTMP.transform.parent != panelImage.transform)
                {
                    questTextTMP.transform.SetParent(panelImage.transform, false);
                }

                RectTransform rt = questTextTMP.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(15f, 5f);
                rt.offsetMax = new Vector2(-15f, -5f);
                rt.anchoredPosition = Vector2.zero;

                questTextTMP.alignment = TextAlignmentOptions.Center;
                questTextTMP.enableAutoSizing = true;
                questTextTMP.fontSizeMin = 18f;
                questTextTMP.fontSizeMax = 28f;
            }
        }
    }

    /// <summary>
    /// Chỉ khóa portal nếu portalObject được gán tường minh hoặc nếu trong Scene có PortalUnlockAfterEnemies.
    /// Tránh tự ý tắt va chạm của cổng ở các Scene cổng được kích hoạt bằng BossController / SkeletonBossController.
    /// </summary>
    private void FindAndLockPortal()
    {
        bool shouldManagePortal = (portalObject != null);

        if (portalObject == null)
        {
            GameWinPortal winPortal = FindAnyObjectByType<GameWinPortal>();
            if (winPortal != null)
            {
                portalObject = unlockScript.gameObject;
                shouldManagePortal = true;
                portalObject = winPortal.gameObject;
            }
            else
            {
                PortalUnlockAfterEnemies unlockScript = FindAnyObjectByType<PortalUnlockAfterEnemies>();
                if (unlockScript != null)
                {
                    portalObject = unlockScript.gameObject;
                }
                else
                {
                    ScenePortal scenePortal = FindAnyObjectByType<ScenePortal>();
                    if (scenePortal != null)
                    {
                        portalObject = scenePortal.gameObject;
                    }
                }
            }
        }

        if (shouldManagePortal && portalObject != null)
        {
            if (portalTrigger == null)
                portalTrigger = portalObject.GetComponentInChildren<Collider2D>(true);

            if (portalVisual == null)
                portalVisual = portalObject.GetComponentInChildren<SpriteRenderer>(true);

            if (portalTrigger != null)
                portalTrigger.enabled = false;

            if (portalVisual != null)
                portalVisual.enabled = false;
            // Tắt hiệu ứng cổng xoay lơ lửng nếu có khi cổng đang bị khóa
            PortalVisualEffect pve = portalObject.GetComponentInChildren<PortalVisualEffect>(true);
            if (pve != null)
                pve.enabled = false;
        }
    }

    /// <summary>
    /// Tự động xác định chế độ theo dõi mục tiêu phù hợp nhất cho Scene hiện tại.
    /// </summary>
    private void ResolveTrackingModeAndTargets()
    {
        // 1. Tìm BossBarrier nếu chưa được gán
        if (bossBarrier == null)
        {
            bossBarrier = FindAnyObjectByType<BossBarrier>();
        }

        // 2. Thu thập danh sách Boss trong Scene
        RegisterBosses();

        // 3. Phân định chế độ thực thi
        if (trackingMode != QuestTrackingMode.Auto)
        {
            resolvedMode = trackingMode;
        }
        else
        {
            if (bossBarrier != null)
            {
                resolvedMode = QuestTrackingMode.BossBarrier;
            }
            else if (trackedBosses.Count > 0)
            {
                resolvedMode = QuestTrackingMode.Boss;
            }
            else
            {
                resolvedMode = QuestTrackingMode.MonsterHunt;
                RegisterRegularEnemies();
            }
        }

        // Xác định tên gọi của Boss chính trong Scene nếu có
        bossDisplayName = DetectBossName();

        Debug.Log($"📜 [QuestUIController] Chế độ Quest đã kích hoạt: {resolvedMode} (Boss: '{bossDisplayName}')");
    }

    private string DetectBossName()
    {
        if (FindAnyObjectByType<LichBossController>() != null) return "Đại Trùm Lich";
        if (FindAnyObjectByType<SkeletonBossController>() != null) return "Boss Khung Xương";
        if (FindAnyObjectByType<BossMap4Controller>() != null) return "Đại Trùm";
        if (FindAnyObjectByType<BossController>() != null) return "Đại Trùm";

        if (trackedBosses.Count > 0 && trackedBosses[0] != null)
        {
            string name = trackedBosses[0].name;
            if (name.Contains("Lich")) return "Đại Trùm Lich";
            if (name.Contains("Skeleton")) return "Boss Khung Xương";
            return "Đại Trùm";
        }

        return "Đại Trùm";
    }

    private void RegisterBosses()
    {
        trackedBosses.Clear();

        if (bossTarget != null)
        {
            EnemyHealth childBossHealth = bossTarget.GetComponentInChildren<EnemyHealth>(true);
            if (childBossHealth != null)
                trackedBosses.Add(childBossHealth.gameObject);
            else
                trackedBosses.Add(bossTarget);
            return;
        }

        LichBossController lichBoss = FindAnyObjectByType<LichBossController>();
        if (lichBoss != null && !trackedBosses.Contains(lichBoss.gameObject))
            trackedBosses.Add(lichBoss.gameObject);

        BossMap4Controller bossMap4 = FindAnyObjectByType<BossMap4Controller>();
        if (bossMap4 != null && !trackedBosses.Contains(bossMap4.gameObject))
            trackedBosses.Add(bossMap4.gameObject);

        BossController bossMap1 = FindAnyObjectByType<BossController>();
        if (bossMap1 != null && !trackedBosses.Contains(bossMap1.gameObject))
            trackedBosses.Add(bossMap1.gameObject);

        SkeletonBossController bossMap5 = FindAnyObjectByType<SkeletonBossController>();
        if (bossMap5 != null && !trackedBosses.Contains(bossMap5.gameObject))
            trackedBosses.Add(bossMap5.gameObject);

        // Quét các đối tượng có tag "Enemy" nhưng là Boss
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];
            if (enemy != null && IsBossObject(enemy) && !trackedBosses.Contains(enemy))
            {
                trackedBosses.Add(enemy);
            }
        }
    }

    private void RegisterRegularEnemies()
    {
        trackedRegularEnemies.Clear();
        var allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        foreach (var e in allEnemies)
        {
            if (e != null && !e.IsBoss && !trackedRegularEnemies.Contains(e))
            {
                trackedRegularEnemies.Add(e);
            }
        }
    }

    private bool IsBossObject(GameObject obj)
    {
        if (obj == null) return false;

        if (obj.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (obj.transform.parent != null &&
            obj.transform.parent.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var health = obj.GetComponent<EnemyHealth>();
        if (health != null && health.IsBoss) return true;

        return obj.GetComponent<BossMap4Controller>() != null
            || obj.GetComponent<BossController>() != null
            || obj.GetComponent<SkeletonBossController>() != null
            || obj.GetComponent<LichBossController>() != null;
    }

    private void RefreshProgress(bool forceUpdate)
    {
        switch (resolvedMode)
        {
            case QuestTrackingMode.BossBarrier:
                RefreshBossBarrierProgress(forceUpdate);
                break;

            case QuestTrackingMode.Boss:
                RefreshBossProgress(forceUpdate);
                break;

            case QuestTrackingMode.MonsterHunt:
                RefreshMonsterHuntProgress(forceUpdate);
                break;
        }
    }

    /// <summary>
    /// Xử lý tiến độ theo dõi rào chắn phòng Boss (Map 5 Phase 2).
    /// </summary>
    private void RefreshBossBarrierProgress(bool forceUpdate)
    {
        if (bossBarrier == null)
        {
            bossBarrier = FindAnyObjectByType<BossBarrier>();
            if (bossBarrier == null) return;
        }

        int total = bossBarrier.TotalMonsterCount;
        int remaining = bossBarrier.RemainingMonsterCount;
        int killed = Mathf.Max(0, total - remaining);

        bool barrierOpened = bossBarrier.IsOpened || (total > 0 && remaining == 0);

        if (!barrierOpened)
        {
            // Đang trong giai đoạn tiêu diệt quái vật thường để mở rào chắn
            if (forceUpdate || killed != lastKilledCount)
            {
                lastKilledCount = killed;
                string text = string.Format(barrierProgressFormat, killed, total);
                SetDisplayText(text, progressColor);
            }
        }
        else
        {
            // Phong ấn đã mở! Kiểm tra Boss
            bool hasBoss = trackedBosses.Count > 0;
            bool bossDead = true;

            for (int i = 0; i < trackedBosses.Count; i++)
            {
                if (!IsTargetDefeated(trackedBosses[i]))
                {
                    bossDead = false;
                    break;
                }
            }

            if (hasBoss && !bossDead)
            {
                // Boss còn sống: Hiển thị nhiệm vụ hạ gục Boss
                string template = string.IsNullOrEmpty(barrierUnlockedFormat)
                    ? "✨ Phong ấn phòng Boss đã mở! Hãy tiêu diệt {0}!"
                    : barrierUnlockedFormat;
                string text = template.Contains("{0}") ? string.Format(template, bossDisplayName) : template;
                SetDisplayText(text, unlockedColor);
            }
            else
            {
                // Boss đã chết (hoặc không có Boss): Hoàn thành ải!
                string template = string.IsNullOrEmpty(bossDefeatedFormat)
                    ? "🏆 {0} đã bị tiêu diệt! Vượt ải thành công!"
                    : bossDefeatedFormat;
                string text = template.Contains("{0}") ? string.Format(template, bossDisplayName) : template;
                SetDisplayText(text, completedColor);

                if (!unlocked)
                {
                    UnlockPortal();
                }
            }
        }
    }

    /// <summary>
    /// Xử lý tiến độ theo dõi Boss tiêu chuẩn (Map 4, Map 5 Phase 1, Map 1).
    /// </summary>
    private void RefreshBossProgress(bool forceUpdate)
    {
        int total = trackedBosses.Count;
        int killed = 0;
        for (int i = 0; i < trackedBosses.Count; i++)
        {
            if (IsTargetDefeated(trackedBosses[i]))
            {
                killed++;
            }
        }

        if (forceUpdate || killed != lastKilledCount)
        {
            lastKilledCount = killed;
            bool allDefeated = (total > 0) && (killed >= total);

            string format = allDefeated ? completedFormat : progressFormat;

            // Nếu format chứa "quái", tự động đổi thành "Boss" khi ở chế độ săn Boss cho trực quan
            if (!string.IsNullOrEmpty(format) && format.Contains("quái"))
            {
                format = format.Replace("quái", "Boss");
            }

            string text = string.Format(format, killed, total);
            Color color = allDefeated ? completedColor : progressColor;
            SetDisplayText(text, color);

            if (allDefeated && !unlocked)
            {
                UnlockPortal();
            }
        }
    }

    /// <summary>
    /// Xử lý tiến độ theo dõi quái vật thường khi Scene không có Boss.
    /// </summary>
    private void RefreshMonsterHuntProgress(bool forceUpdate)
    {
        int total = trackedRegularEnemies.Count;
        int remaining = 0;

        for (int i = 0; i < trackedRegularEnemies.Count; i++)
        {
            var e = trackedRegularEnemies[i];
            if (e != null && !e.IsDead && e.gameObject.activeInHierarchy)
            {
                remaining++;
            }
        }

        int killed = Mathf.Max(0, total - remaining);

        if (forceUpdate || killed != lastKilledCount)
        {
            lastKilledCount = killed;
            bool allDefeated = (total > 0) && (remaining == 0);

            string format = allDefeated ? completedFormat : progressFormat;
            string text = string.Format(format, killed, total);
            Color color = allDefeated ? completedColor : progressColor;
            SetDisplayText(text, color);

            if (allDefeated && !unlocked)
            {
                UnlockPortal();
            }
        }
    }

    private bool IsTargetDefeated(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
            return true;

        EnemyHealth health = target.GetComponentInChildren<EnemyHealth>(true);
        if (health != null && health.IsDead)
            return true;

        Collider2D col = target.GetComponentInChildren<Collider2D>(true);
        if (col != null && !col.enabled)
            return true;

        if (health == null && col == null && target.transform.childCount == 0)
            return true;

        return false;
    }

    private void UnlockPortal()
    {
        unlocked = true;

        if (portalVisual != null)
            portalVisual.enabled = true;

        if (portalTrigger != null)
            portalTrigger.enabled = true;

        if (portalObject != null)
        {
            PortalVisualEffect pve = portalObject.GetComponentInChildren<PortalVisualEffect>(true);
            if (pve != null)
                pve.enabled = true;
        }
    }

    private void SetDisplayText(string content, Color color)
    {
        if (lastDisplayedText == content && questTextTMP != null && questTextTMP.color == color)
            return;

        lastDisplayedText = content;

        if (questTextTMP != null)
        {
            questTextTMP.text = content;
            questTextTMP.color = color;
        }

        if (questTextLegacy != null)
        {
            questTextLegacy.text = content;
            questTextLegacy.color = color;
        }
    }
}
