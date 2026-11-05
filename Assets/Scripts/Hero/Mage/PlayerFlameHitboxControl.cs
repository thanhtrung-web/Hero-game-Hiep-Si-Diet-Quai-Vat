using UnityEngine;

public class PlayerFlameHitboxControl : MonoBehaviour
{
    [SerializeField] private GameObject flameHitbox;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private float forwardOffset = 0.8f;

    private BoxCollider2D flameCollider;
    private float baseColliderOffsetX; // offset.x của collider khi nhân vật hướng phải

    private void Awake()
    {
        if (flameHitbox != null)
        {
            flameCollider = flameHitbox.GetComponent<BoxCollider2D>();
            if (flameCollider != null)
                baseColliderOffsetX = Mathf.Abs(flameCollider.offset.x);

            flameHitbox.SetActive(false);
        }
    }

    public void EnableFlameHitbox()
    {
        if (flameHitbox == null) return;

        if (playerSprite != null)
        {
            float direction = playerSprite.flipX ? -1f : 1f;

            // Dời cả object hitbox ra phía trước mặt...
            Vector3 position = flameHitbox.transform.localPosition;
            position.x = Mathf.Abs(forwardOffset) * direction;
            flameHitbox.transform.localPosition = position;

            // ...và lật luôn offset của collider (trước đây collider luôn lệch về bên phải)
            if (flameCollider != null)
            {
                Vector2 offset = flameCollider.offset;
                offset.x = baseColliderOffsetX * direction;
                flameCollider.offset = offset;
            }
        }

        flameHitbox.SetActive(true);
    }

    public void DisableFlameHitbox()
    {
        if (flameHitbox != null)
            flameHitbox.SetActive(false);
    }
}