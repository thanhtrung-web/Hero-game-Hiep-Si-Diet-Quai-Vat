using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    public static int Collected { get; private set; }

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag(playerTag)) return;

        collected = true;
        Collected++;
        Debug.Log("Đã nhặt key. Tổng key: " + Collected);
        Destroy(gameObject);
    }
}