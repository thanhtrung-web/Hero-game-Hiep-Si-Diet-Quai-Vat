using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUIController : MonoBehaviour
{
    [Header("1. Kéo Text hiển thị vào đây (Lưu sẵn trong Prefab)")]
    [SerializeField] private TMP_Text questTextTMP;
    [SerializeField] private Text questTextLegacy;

    [Header("2. Cổng qua màn (Tùy chọn: Kéo vào hoặc để trống sẽ tự tìm)")]
    [SerializeField] private GameObject portalObject;
    [SerializeField] private Collider2D portalTrigger;
    [SerializeField] private SpriteRenderer portalVisual;

    [Header("3. Nội dung & Màu sắc")]
    [SerializeField] private string progressFormat = "Cần tiêu diệt {0}/{1} quái để vượt ải";
    [SerializeField] private string completedFormat = "Cần tiêu diệt {0}/{1} quái để vượt ải (Đã mở cổng!)";
    [SerializeField] private Color progressColor = Color.white;
    [SerializeField] private Color completedColor = new Color(0.4f, 1f, 0.4f, 1f);

    private readonly List<GameObject> trackedEnemies = new List<GameObject>();
    private int totalEnemies;
    private int lastKilledCount = -1;
    private bool unlocked;

    private void Awake()
    {
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
        RegisterEnemies();
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

    private void RegisterEnemies()
    {
        trackedEnemies.Clear();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && !trackedEnemies.Contains(enemies[i]))
            {
                trackedEnemies.Add(enemies[i]);
            }
        }

        totalEnemies = trackedEnemies.Count;
    }

    private void RefreshProgress(bool forceUpdate)
    {
        // Cập nhật nếu có quái sinh thêm trong màn
        GameObject[] currentEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < currentEnemies.Length; i++)
        {
            if (currentEnemies[i] != null && !trackedEnemies.Contains(currentEnemies[i]))
            {
                trackedEnemies.Add(currentEnemies[i]);
                totalEnemies = trackedEnemies.Count;
                forceUpdate = true;
            }
        }

        int killedCount = 0;
        for (int i = 0; i < trackedEnemies.Count; i++)
        {
            if (IsEnemyDefeated(trackedEnemies[i]))
            {
                killedCount++;
            }
        }

        if (forceUpdate || killedCount != lastKilledCount)
        {
            lastKilledCount = killedCount;
            bool allDefeated = (totalEnemies == 0) || (killedCount >= totalEnemies);
            UpdateUI(killedCount, totalEnemies, allDefeated);

            if (allDefeated && !unlocked)
            {
                UnlockPortal();
            }
        }
    }

    private bool IsEnemyDefeated(GameObject enemy)
    {
        if (enemy == null || !enemy.activeInHierarchy)
            return true;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null && health.IsDead)
            return true;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col != null && !col.enabled)
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

        Debug.Log("Đã tiêu diệt đủ quái — Cổng qua màn đã mở!");
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
