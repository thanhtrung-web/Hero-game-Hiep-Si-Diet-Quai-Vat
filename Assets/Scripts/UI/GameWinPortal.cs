using UnityEngine;

public class GameWinPortal : MonoBehaviour
{
    [SerializeField] private GameEndUI gameEndUI;

    private void Awake()
    {
        if (gameEndUI == null)
            gameEndUI = FindAnyObjectByType<GameEndUI>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        if (gameEndUI == null)
            gameEndUI = FindAnyObjectByType<GameEndUI>();

        if (gameEndUI != null)
        {
            gameEndUI.ShowWin();
        }
        else
        {
            Debug.LogWarning("[GameWinPortal] Không tìm thấy GameEndUI trong Scene để hiển thị màn hình chiến thắng!");
        }
    }
}
