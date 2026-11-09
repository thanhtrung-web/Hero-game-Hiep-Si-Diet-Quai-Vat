using UnityEngine;

public class PortalUnlockAfterEnemies : MonoBehaviour
{
    [SerializeField] private Collider2D portalTrigger;
    [SerializeField] private SpriteRenderer portalVisual;

    private GameObject bossObject;
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
        if (FindAnyObjectByType<QuestUIController>() != null)
        {
            enabled = false;
            return;
        }

        BossMap4Controller boss4 = FindAnyObjectByType<BossMap4Controller>();
        if (boss4 != null) bossObject = boss4.gameObject;
    }

    private void Update()
    {
        if (unlocked) return;

        if (bossObject != null)
        {
            EnemyHealth health = bossObject.GetComponent<EnemyHealth>();
            Collider2D col = bossObject.GetComponent<Collider2D>();
            bool isDead = (health != null && health.IsDead) || (col != null && !col.enabled);
            if (!isDead) return;
        }

        unlocked = true;

        if (portalVisual != null)
            portalVisual.enabled = true;

        if (portalTrigger != null)
            portalTrigger.enabled = true;
    }
}
