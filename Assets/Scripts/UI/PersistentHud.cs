using UnityEngine;

public class PersistentHud : MonoBehaviour
{
    private static PersistentHud instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureBossHealthBar();
    }

    private void EnsureBossHealthBar()
    {
        if (FindAnyObjectByType<BossHealthBarUI>() == null)
        {
            GameObject bossBarObj = new GameObject("BossHealthBarUI");
            bossBarObj.AddComponent<BossHealthBarUI>();
        }
    }
}
