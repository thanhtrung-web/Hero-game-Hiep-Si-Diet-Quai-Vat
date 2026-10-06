using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class SlimePatrol : MonoBehaviour
{
    [SerializeField] private float leftLimit;
    [SerializeField] private float rightLimit;
    [SerializeField] private float moveSpeed = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int direction = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (rb.position.x >= rightLimit)
            direction = -1;
        else if (rb.position.x <= leftLimit)
            direction = 1;

        rb.linearVelocityX = direction * moveSpeed;
        spriteRenderer.flipX = direction < 0;
    }
}