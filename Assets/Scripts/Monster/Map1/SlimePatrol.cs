using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class SlimePatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float leftLimit;
    [SerializeField] private float rightLimit;
    [SerializeField] private float moveSpeed = 1f;

    [Header("Sprite")]
    [Tooltip("Bật nếu sprite gốc của boss đang nhìn sang trái.")]
    [SerializeField] private bool spriteFacesLeftByDefault = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int direction = 1; // 1 = sang phải, -1 = sang trái

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (rb.position.x >= rightLimit)
        {
            direction = -1;
        }
        else if (rb.position.x <= leftLimit)
        {
            direction = 1;
        }

        rb.linearVelocityX = direction * moveSpeed;

        // Sprite gốc nhìn trái: lật hình khi đi sang phải.
        // Nếu sprite gốc nhìn phải thì dùng quy tắc ngược lại.
        spriteRenderer.flipX = spriteFacesLeftByDefault
            ? direction > 0
            : direction < 0;
    }
}