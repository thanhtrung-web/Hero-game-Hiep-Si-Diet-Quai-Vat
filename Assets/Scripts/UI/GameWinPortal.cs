
using UnityEngine;

public class GameWinPortal : MonoBehaviour
{
    [SerializeField] private GameEndUI gameEndUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        if (gameEndUI == null)
            gameEndUI = FindAnyObjectByType<GameEndUI>(FindObjectsInactive.Include);

        if (gameEndUI != null)
            gameEndUI.ShowWin();
    }
}
