using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFireball : MonoBehaviour
{
    [SerializeField] private FireballProjectile projectilePrefab;
    [SerializeField]
    private Vector2 spawnOffset =
        new Vector2(0.6f, 0f);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SpawnFireball()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Assign Projectile Prefab on Player.", this);
            return;
        }

        float direction = spriteRenderer.flipX ? -1f : 1f;

        Vector3 localOffset = new Vector3(
            spawnOffset.x * direction,
            spawnOffset.y,
            0f
        );

        Vector3 spawnPosition =
            transform.TransformPoint(localOffset);

        FireballProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        projectile.Launch(direction);
    }
}