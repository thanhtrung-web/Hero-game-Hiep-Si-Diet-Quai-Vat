using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý cạm bẫy Cột Linh Hồn Địa Ngục (Soul Pillar Hazard) trong Map 5.
/// Được triệu hồi bởi Pháp Sư Vong Hồn (Wraith Mage) hoặc Boss Lich dưới chân người chơi.
/// Sau khoảng trễ cảnh báo (eruptionDelay), cột năng lượng bùng nổ gây sát thương diện rộng và tự biến mất.
/// </summary>
public class SoulPillarHazard : MonoBehaviour
{
    [Header("Cấu hình Cột Bẫy (Hazard Settings)")]
    [Tooltip("Lượng sát thương gây ra khi cột ma pháp bùng nổ")]
    [SerializeField] private int damage = 26;

    [Tooltip("Bán kính vùng gây sát thương xung quanh tâm cột (mét)")]
    [SerializeField] private float hitRadius = 1.0f;

    [Tooltip("Thời gian trễ cảnh báo trước khi cột bùng nổ sát thương (giây)")]
    [SerializeField] private float eruptionDelay = 0.35f;

    [Tooltip("Thời gian tồn tại tối đa của đối tượng trước khi tự hủy hoàn toàn (giây)")]
    [SerializeField] private float lifeTime = 1.1f;

    // Cờ khóa tránh gây sát thương lặp lại nhiều lần
    private bool hasDealtDamage = false;

    /// <summary>
    /// Khởi động chuỗi bùng nổ khi bẫy xuất hiện trong Scene.
    /// </summary>
    private void Start()
    {
        StartCoroutine(EruptRoutine());
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// Coroutine chờ thời gian trễ cảnh báo rồi kích hoạt gây sát thương.
    /// </summary>
    private IEnumerator EruptRoutine()
    {
        yield return new WaitForSeconds(eruptionDelay);
        if (!hasDealtDamage)
        {
            hasDealtDamage = true;
            CheckAndDealDamage();
        }
    }

    /// <summary>
    /// Kích hoạt sát thương tức thời từ bên ngoài (hoặc qua Animation Event).
    /// </summary>
    public void TriggerDamage()
    {
        if (!hasDealtDamage)
        {
            hasDealtDamage = true;
            CheckAndDealDamage();
        }
    }

    /// <summary>
    /// Thiết lập lượng sát thương gây ra từ nguồn triệu hồi (ví dụ: Boss Lich/Wraith Mage).
    /// </summary>
    /// <param name="dmg">Giá trị sát thương mới.</param>
    public void SetDamage(int dmg)
    {
        damage = dmg;
    }

    /// <summary>
    /// Quét vùng hình tròn và áp dụng sát thương lên Người chơi nếu đứng trong phạm vi nổ.
    /// </summary>
    private void CheckAndDealDamage()
    {
        // Quét các Collider nằm trong bán kính vùng nổ
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 0.5f, hitRadius);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // Gây sát thương lên Hiệp sĩ (Knight)
                var knight = col.GetComponentInParent<KnightController>();
                if (knight != null) knight.TakeDamage(damage);

                // Gây sát thương lên Pháp sư (PlayerHealth)
                var mage = col.GetComponentInParent<PlayerHealth>();
                if (mage != null) mage.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// Vẽ vùng sát thương trực quan trong cửa sổ Scene của Unity Editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, hitRadius);
    }
}
