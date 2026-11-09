using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    private static DamagePopupManager instance;
    public static DamagePopupManager Instance
    {
        get
        {
            if (instance == null)
            {
                var existing = FindAnyObjectByType<DamagePopupManager>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("DamagePopupManager");
                    instance = go.AddComponent<DamagePopupManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Hiển thị số sát thương bay lên tại vị trí trong thế giới 2D
    /// </summary>
    public static void ShowDamage(Vector3 worldPosition, int damage, bool isCrit = false)
    {
        if (damage <= 0) return;

        // Đảm bảo Manager đã sẵn sàng
        var mgr = Instance;
        if (mgr == null) return;

        mgr.SpawnPopup(worldPosition, damage, isCrit);
    }

    private void SpawnPopup(Vector3 worldPosition, int damage, bool isCrit)
    {
        try
        {
            GameObject popupObj = new GameObject("DamagePopup_" + damage);
            popupObj.transform.position = worldPosition;

            DamagePopup popup = popupObj.AddComponent<DamagePopup>();
            popup.Setup(damage, isCrit);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DamagePopupManager] Lỗi tạo DamagePopup: " + ex.Message);
        }
    }
}
