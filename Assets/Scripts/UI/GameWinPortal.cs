
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

        gameEndUI.ShowWin();
    }
}
