using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class FireballProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float flightDuration = 1.2f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float remainingTime;
    private bool launched;
    private bool ending;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Launch(float direction)
    {
        direction = direction < 0f ? -1f : 1f;

        spriteRenderer.flipX = direction < 0f;
        rb.linearVelocity = new Vector2(direction * speed, 0f);

        remainingTime = flightDuration;
        launched = true;
    }

    private void Update()
    {
        if (!launched || ending)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            ending = true;
            rb.linearVelocity = Vector2.zero;
            animator.SetTrigger("End");

            // Cleanup if the Finish animation event is missing.
            Destroy(gameObject, 2f);
        }
    }

    public void Finish()
    {
        Destroy(gameObject);
    }
}