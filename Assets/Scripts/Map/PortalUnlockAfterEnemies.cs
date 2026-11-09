using UnityEngine;

public class PortalUnlockAfterEnemies : MonoBehaviour
{
    [SerializeField] private Collider2D portalTrigger;
    [SerializeField] private SpriteRenderer portalVisual;

    private bool unlocked;

    private void Awake()
    {
        if (portalTrigger == null)
            portalTrigger = GetComponent<Collider2D>();

        if (portalVisual == null)
            portalVisual = GetComponent<SpriteRenderer>();

        if (portalTrigger != null)
            portalTrigger.enabled = false;

        if (portalVisual != null)
            portalVisual.enabled = false;
    }

    private void Start()
    {
        // Nếu trong Scene đã có QuestUIController thì nhường toàn bộ việc đếm quái + mở cổng cho QuestUIController,
        // tắt Update của script này để không bị chạy trùng lặp code.
        if (FindAnyObjectByType<QuestUIController>() != null)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (unlocked) return;

        if (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
            return;

        unlocked = true;

        if (portalVisual != null)
            portalVisual.enabled = true;

        if (portalTrigger != null)
            portalTrigger.enabled = true;

        Debug.Log("Đã hạ hết quái — cổng xuất hiện!");
    }
}
