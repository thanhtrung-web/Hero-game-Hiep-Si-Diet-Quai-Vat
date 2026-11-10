using UnityEngine;

/// <summary>
/// Điều khiển đường đạn ma thuật tử linh (Soul Missile Projectile) trong Map 5.
/// Đạn được bắn ra từ Pháp Sư Vong Hồn (Wraith Mage) hoặc Boss, bay theo đường thẳng nằm ngang song song mặt đất.
/// Tự động phát hiện va chạm với người chơi (Knight/Mage) hoặc địa hình (Ground) để phát nổ/hủy đối tượng.
/// </summary>
public class SoulMissileProjectile : MonoBehaviour
{
    [Header("Cấu hình đường đạn (Projectile Settings)")]
    [Tooltip("Tốc độ bay thẳng của viên đạn")]
    [SerializeField] private float speed = 7.0f;

    [Tooltip("Lượng sát thương gây ra khi va chạm với người chơi")]
    [SerializeField] private int damage = 16;

    [Tooltip("Thời gian tồn tại tối đa (giây) trước khi tự hủy nếu không trúng mục tiêu")]
    [SerializeField] private float lifeTime = 4.0f;

    // Hướng di chuyển chuẩn hóa theo trục ngang
    private Vector2 moveDir = Vector2.right;

    /// <summary>
    /// Kích hoạt phóng đạn theo hướng chỉ định và thiết lập lượng sát thương.
    /// </summary>
    /// <param name="direction">Vector hướng phóng đạn (chỉ lấy trục X để bay ngang thẳng tắp).</param>
    /// <param name="dmg">Lượng sát thương gây ra khi trúng đích.</param>
    public void Launch(Vector2 direction, int dmg = 16)
    {
        // Khóa hướng bay hoàn toàn theo trục ngang (Horizontal Straight Line)
        float dirX = (direction.x >= 0f) ? 1.0f : -1.0f;
        moveDir = new Vector2(dirX, 0f);
        damage = dmg;

        // Giữ thẳng góc và lật chiều hiển thị Sprite theo trục X
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(dirX, 1.0f, 1.0f);

        // Đặt hẹn giờ tự hủy sau khoảng lifeTime
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// Cập nhật vị trí viên đạn mỗi frame theo hướng và tốc độ cố định.
    /// </summary>
    private void Update()
    {
        // Bay thẳng ngang song song với mặt đất
        transform.position += (Vector3)(moveDir * speed * Time.deltaTime);
    }

    /// <summary>
    /// Xử lý va chạm 2D (Trigger) khi đạn chạm vào Người chơi hoặc Mặt đất.
    /// </summary>
    /// <param name="collision">Collider đối tượng va chạm.</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Va chạm với Người chơi (Player)
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // Gây sát thương lên Hiệp sĩ (Knight) nếu có
            var knight = collision.GetComponentInParent<KnightController>();
            if (knight != null) knight.TakeDamage(damage);

            // Gây sát thương lên Pháp sư (PlayerHealth) nếu có
            var mage = collision.GetComponentInParent<PlayerHealth>();
            if (mage != null) mage.TakeDamage(damage);

            Destroy(gameObject);
        }
        // 2. Va chạm với Địa hình/Mặt đất (Ground)
        else if (collision.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
