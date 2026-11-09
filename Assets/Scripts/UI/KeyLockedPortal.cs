using UnityEngine;

public class KeyLockedPortal : MonoBehaviour
{
    [SerializeField] private ScenePortal scenePortal;
    [SerializeField] private Collider2D portalTrigger;
    [SerializeField] private int requiredKeys = 1;

    private bool unlocked = false;

    private void Awake()
    {
        if (scenePortal == null)
            scenePortal = GetComponent<ScenePortal>();

        if (portalTrigger == null)
            portalTrigger = GetComponent<Collider2D>();

        if (scenePortal != null)
            scenePortal.enabled = false;

        if (portalTrigger != null)
            portalTrigger.enabled = false;
    }

    private void Update()
    {
        if (unlocked || KeyItem.Collected < requiredKeys) return;

        unlocked = true;

        if (scenePortal != null)
            scenePortal.enabled = true;

        if (portalTrigger != null)
            portalTrigger.enabled = true;

        Debug.Log("Đã đủ key — cổng đã mở!");
    }
}