using UnityEngine;

/// <summary>
/// Quản lý tỉ lệ rơi vật phẩm (Bình máu HealSmall / HealBig) khi quái vật bị tiêu diệt.
/// Tự động lắng nghe sự kiện OnDeath từ <see cref="EnemyHealth"/> để đảm bảo tương thích 100%
/// với tất cả quái vật (Skeleton, Pháp sư, Hiệp sĩ, Boss).
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyItemDropper : MonoBehaviour
{
    [Header("Item Drop Settings (Cấu hình rơi vật phẩm)")]
    [Tooltip("Prefab vật phẩm sẽ rơi ra (HealSmall hoặc HealBig)")]
    [SerializeField] private GameObject dropItemPrefab;

    [Tooltip("Tỉ lệ rơi vật phẩm từ 0.0 (0%) đến 1.0 (100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.2f;

    [Tooltip("Độ lệch vị trí rơi so với chân quái")]
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.4f, 0f);

    [Tooltip("Tự động gán Prefab HealSmall từ Assets nếu để trống")]
    [SerializeField] private bool autoLoadDefaultPrefab = true;

    private EnemyHealth enemyHealth;
    private bool hasDropped = false;

    public float DropChance
    {
        get => dropChance;
        set => dropChance = Mathf.Clamp01(value);
    }

    public GameObject DropItemPrefab
    {
        get => dropItemPrefab;
        set => dropItemPrefab = value;
    }

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += HandleMonsterDeath;
        }

#if UNITY_EDITOR
        if (dropItemPrefab == null && autoLoadDefaultPrefab)
        {
            dropItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/HealSmall.prefab");
        }
#endif
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= HandleMonsterDeath;
        }
    }

    /// <summary>
    /// Kích hoạt khi quái vật bắt đầu chết.
    /// </summary>
    private void HandleMonsterDeath()
    {
        if (hasDropped) return;
        hasDropped = true;

        TryDropItem();
    }

    /// <summary>
    /// Gieo xác suất và sinh vật phẩm tại vị trí chân quái.
    /// </summary>
    public void TryDropItem()
    {
        if (dropItemPrefab == null)
        {
            // Dự phòng nạp nếu chưa gán
            dropItemPrefab = Resources.Load<GameObject>("HealSmall");
            if (dropItemPrefab == null)
            {
#if UNITY_EDITOR
                dropItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/HealSmall.prefab");
#endif
            }
        }

        if (dropItemPrefab == null)
        {
            Debug.LogWarning($"[EnemyItemDropper] '{gameObject.name}' không có dropItemPrefab để rơi vật phẩm!");
            return;
        }

        float roll = Random.value;
        if (roll <= dropChance)
        {
            Vector3 spawnPos = transform.position + dropOffset;

            // Đặt vật phẩm sát sàn chân của quái
            SpriteRenderer monsterSr = GetComponentInChildren<SpriteRenderer>();
            if (monsterSr != null)
            {
                spawnPos.y = monsterSr.bounds.min.y + dropOffset.y;
            }

            GameObject dropped = Instantiate(dropItemPrefab, spawnPos, Quaternion.identity);

            // Đồng bộ Sorting Layer để vật phẩm hiển thị đè lên xác quái, không bị chìm
            SpriteRenderer itemSr = dropped.GetComponentInChildren<SpriteRenderer>();
            if (itemSr != null && monsterSr != null)
            {
                itemSr.sortingLayerID = monsterSr.sortingLayerID;
                itemSr.sortingOrder = monsterSr.sortingOrder + 2;
            }

            Debug.Log($"🎁 [{gameObject.name}] Đã rơi vật phẩm '{dropped.name}' (Tỉ lệ: {dropChance * 100f:0}%, Roll: {roll * 100f:0.0}%).");
        }
        else
        {
            Debug.Log($"💨 [{gameObject.name}] Không rơi vật phẩm (Tỉ lệ: {dropChance * 100f:0}%, Roll: {roll * 100f:0.0}%).");
        }
    }
}
