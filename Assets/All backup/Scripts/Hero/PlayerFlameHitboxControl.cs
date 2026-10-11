using UnityEngine;

public class PlayerFlameHitboxControl : MonoBehaviour
{
    [SerializeField] private GameObject flameHitbox;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private float forwardOffset = 0.8f;

    private void Awake()
    {
        if (flameHitbox != null)
            flameHitbox.SetActive(false);
    }

    public void EnableFlameHitbox()
    {
        if (flameHitbox == null) return;

        if (playerSprite != null)
        {
            Vector3 position = flameHitbox.transform.localPosition;
            position.x = Mathf.Abs(forwardOffset) *
                         (playerSprite.flipX ? -1f : 1f);
            flameHitbox.transform.localPosition = position;
        }

        flameHitbox.SetActive(true);
    }

    public void DisableFlameHitbox()
    {
        if (flameHitbox != null)
            flameHitbox.SetActive(false);
    }
}