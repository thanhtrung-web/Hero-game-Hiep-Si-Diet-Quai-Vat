using UnityEngine;

public class BossMap4Health : EnemyHealth
{
    private BossMap4Controller bossController;

    protected override void Awake()
    {
        base.Awake(); // Chạy phần khởi tạo máu và animator của lớp cha (EnemyHealth)

        // Tự động lấy script điều khiển Boss trên cùng đối tượng
        bossController = GetComponent<BossMap4Controller>();
    }

    // Ghi đè (override) hàm TakeDamage để chuyển hướng sát thương thẳng vào BossMap4Controller
    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        // Gọi trực tiếp hàm TakeDamage của BossMap4Controller (hỗ trợ ép kiểu float)
        if (bossController != null)
        {
            bossController.TakeDamage((float)damage);
        }
        else
        {
            // Dự phòng nếu không tìm thấy Boss Controller thì chạy logic trừ máu gốc
            base.TakeDamage(damage);
        }
    }
}