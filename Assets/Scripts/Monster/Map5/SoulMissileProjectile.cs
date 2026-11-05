using UnityEngine;

public class SoulMissileProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7.0f;
    [SerializeField] private int damage = 16;
    [SerializeField] private float lifeTime = 4.0f;

    private Vector2 moveDir = Vector2.right;

    public void Launch(Vector2 direction, int dmg = 16)
    {
        // Khóa hướng bay hoàn toàn theo trục ngang (Horizontal Straight Line)
        float dirX = (direction.x >= 0f) ? 1.0f : -1.0f;
        moveDir = new Vector2(dirX, 0f);
        damage = dmg;

        // Giữ thẳng góc và lật chiều theo trục X
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(dirX, 1.0f, 1.0f);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Bay thẳng ngang song song với mặt đất
        transform.position += (Vector3)(moveDir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var knight = collision.GetComponentInParent<KnightController>();
            if (knight != null) knight.TakeDamage(damage);

            var mage = collision.GetComponentInParent<PlayerHealth>();
            if (mage != null) mage.TakeDamage(damage);

            Destroy(gameObject);
        }
        else if (collision.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
