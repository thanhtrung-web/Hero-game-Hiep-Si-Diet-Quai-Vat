
using UnityEngine;

public class BossBarrier : MonoBehaviour
{
    [Header("Danh sach quai con (KHONG them Boss)")]
    [SerializeField] private EnemyHealth[] monsters;

    private bool barrierOpened = false;

    private void Update()
    {
        if (barrierOpened) return;

        foreach (EnemyHealth monster in monsters)
        {
            // Quái còn tồn tại và chưa chết
            if (monster != null && !monster.IsDead)
            {
                return;
            }
        }

        // Tất cả quái con đã chết
        barrierOpened = true;

        Debug.Log("[BossBarrier] All monsters defeated! Opening barrier.");

        gameObject.SetActive(false);
    }
}
