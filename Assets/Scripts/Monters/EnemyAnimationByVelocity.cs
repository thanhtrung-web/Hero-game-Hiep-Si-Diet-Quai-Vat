using UnityEngine;

public class EnemyAnimationByVelocity : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private float moveThreshold = 0.05f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (rb == null || animator == null)
            return;

        bool isMoving = rb.linearVelocity.magnitude > moveThreshold;
        animator.SetBool("IsMoving", isMoving);
    }
}