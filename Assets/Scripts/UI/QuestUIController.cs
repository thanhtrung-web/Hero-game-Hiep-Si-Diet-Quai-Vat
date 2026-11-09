using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUIController : MonoBehaviour
{
    [Header("1. Kéo Text hiển thị vào đây (Lưu sẵn trong Prefab)")]
    [SerializeField] private TMP_Text questTextTMP;
    [SerializeField] private Text questTextLegacy;

    [Header("2. Mục tiêu Boss & Cổng qua màn (Kéo Cha hoặc Con đều được)")]
    [SerializeField] private GameObject bossTarget;
    [SerializeField] private GameObject portalObject;
    [SerializeField] private Collider2D portalTrigger;
    [SerializeField] private SpriteRenderer portalVisual;

    [Header("3. Nội dung & Màu sắc")]
    [SerializeField] private string progressFormat = "Cần tiêu diệt {0}/{1} Boss để vượt ải";
    [SerializeField] private string completedFormat = "Đã tiêu diệt {0}/{1} Boss — Mở cổng qua màn!";
    [SerializeField] private Color progressColor = Color.white;
    [SerializeField] private Color completedColor = new Color(0.4f, 1f, 0.4f, 1f);

    private readonly List<GameObject> trackedBosses = new List<GameObject>();
    private int totalBosses;
    private int lastKilledCount = -1;
    private bool unlocked;

    private void Awake()
    {
        // Tự động đổi chữ "quái" cũ trong Inspector thành "Boss"
        if (!string.IsNullOrEmpty(progressFormat) && progressFormat.Contains("quái"))
        {
            progressFormat = progressFormat.Replace("quái", "Boss");
        }
        if (!string.IsNullOrEmpty(completedFormat) && completedFormat.Contains("quái"))
        {
            completedFormat = completedFormat.Replace("quái", "Boss");
        }

        // Tự động tìm Text con nếu quên kéo trong Prefab
        if (questTextTMP == null)
            questTextTMP = GetComponentInChildren<TMP_Text>();

        if (questTextLegacy == null)
            questTextLegacy = GetComponentInChildren<Text>();

        AutoAlignTextIntoPanel();
        FindAndLockPortal();
    }

    private void AutoAlignTextIntoPanel()
    {
        // Tìm khung Panel nền để tự động đưa chữ vào chính giữa khung và làm mờ nền đen
        Image panelImage = GetComponentInChildren<Image>();
        if (panelImage != null)
        {
            if (panelImage.color.a > 0.85f && panelImage.color.r < 0.1f && panelImage.color.g < 0.1f && panelImage.color.b < 0.1f)
            {
                panelImage.color = new Color(0f, 0f, 0f, 0.55f);
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
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                questTextTMP.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private void Start()
    {
        RegisterBosses();
        RefreshProgress(forceUpdate: true);
    }

    private void Update()
    {
        RefreshProgress(forceUpdate: false);
    }

    private void FindAndLockPortal()
    {
        // Tự động tìm cổng trong Scene nếu chưa kéo tay
        if (portalObject == null)
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
                else
                {
                    GameWinPortal winPortal = FindAnyObjectByType<GameWinPortal>();
                    if (winPortal != null)
                        portalObject = winPortal.gameObject;
                }
            }
        }

        if (portalObject != null)
        {
            if (portalTrigger == null)
                portalTrigger = portalObject.GetComponentInChildren<Collider2D>(true);

            if (portalVisual == null)
                portalVisual = portalObject.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (portalTrigger != null)
            portalTrigger.enabled = false;

        if (portalVisual != null)
            portalVisual.enabled = false;
    }

    private void RegisterBosses()
    {
        trackedBosses.Clear();

        // 1. Nếu có kéo Boss (hoặc Object cha "Boss") vào Inspector:
        // Tự động lấy con Boss thực sự bên trong (ví dụ Boss_Frost nằm trong Boss)
        if (bossTarget != null)
        {
            EnemyHealth childBossHealth = bossTarget.GetComponentInChildren<EnemyHealth>(true);
            if (childBossHealth != null)
            {
                trackedBosses.Add(childBossHealth.gameObject);
            }
            else
            {
                Collider2D childCol = bossTarget.GetComponentInChildren<Collider2D>(true);
                if (childCol != null)
                    trackedBosses.Add(childCol.gameObject);
                else
                    trackedBosses.Add(bossTarget);
            }

            totalBosses = trackedBosses.Count;
            return;
        }

        // 2. Tự động tìm Boss theo các script Boss của từng Map
        BossMap4Controller bossMap4 = FindAnyObjectByType<BossMap4Controller>();
        if (bossMap4 != null && !trackedBosses.Contains(bossMap4.gameObject))
            trackedBosses.Add(bossMap4.gameObject);

        BossController bossMap1 = FindAnyObjectByType<BossController>();
        if (bossMap1 != null && !trackedBosses.Contains(bossMap1.gameObject))
            trackedBosses.Add(bossMap1.gameObject);

        SkeletonBossController bossMap5 = FindAnyObjectByType<SkeletonBossController>();
        if (bossMap5 != null && !trackedBosses.Contains(bossMap5.gameObject))
            trackedBosses.Add(bossMap5.gameObject);

        // 3. Tìm thêm bất kỳ đối tượng Enemy nào có chữ "Boss" trong tên (hoặc tên Object cha)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];
            if (enemy != null && IsBossObject(enemy) && !trackedBosses.Contains(enemy))
            {
                trackedBosses.Add(enemy);
            }
        }

        totalBosses = trackedBosses.Count;
    }

    private bool IsBossObject(GameObject obj)
    {
        if (obj == null) return false;

        if (obj.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (obj.transform.parent != null &&
            obj.transform.parent.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return obj.GetComponent<BossMap4Controller>() != null
            || obj.GetComponent<BossController>() != null
            || obj.GetComponent<SkeletonBossController>() != null;
    }

    private void RefreshProgress(bool forceUpdate)
    {
        int killedCount = 0;
        for (int i = 0; i < trackedBosses.Count; i++)
        {
            if (IsBossDefeated(trackedBosses[i]))
            {
                killedCount++;
            }
        }

        if (forceUpdate || killedCount != lastKilledCount)
        {
            lastKilledCount = killedCount;
            bool allDefeated = (totalBosses > 0) && (killedCount >= totalBosses);
            UpdateUI(killedCount, totalBosses, allDefeated);

            if (allDefeated && !unlocked)
            {
                UnlockPortal();
            }
        }
    }

    private bool IsBossDefeated(GameObject boss)
    {
        if (boss == null || !boss.activeInHierarchy)
            return true;

        EnemyHealth health = boss.GetComponentInChildren<EnemyHealth>(true);
        if (health != null && health.IsDead)
            return true;

        Collider2D col = boss.GetComponentInChildren<Collider2D>(true);
        if (col != null && !col.enabled)
            return true;

        // Nếu boss là một Object rỗng chứa con bên trong và con đã bị Destroy hết
        if (health == null && col == null && boss.transform.childCount == 0)
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

        Debug.Log("Đã tiêu diệt Boss — Cổng qua màn đã mở!");
    }

    private void UpdateUI(int killed, int total, bool completed)
    {
        string content = completed
            ? string.Format(completedFormat, killed, total)
            : string.Format(progressFormat, killed, total);

        Color color = completed ? completedColor : progressColor;

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
