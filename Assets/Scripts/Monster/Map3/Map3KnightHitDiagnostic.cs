using System.Collections;
using UnityEngine;

namespace HeroGame.Map3
{
    /// <summary>
    /// Diagnostic-only component for Map 3. Attach to Knight in scene temporarily.
    /// Does not modify KnightController, enemy health, or deal any damage.
    /// Logs the same overlap/target checks used by the supplied KnightController.
    /// </summary>
    public sealed class Map3KnightHitDiagnostic : MonoBehaviour
    {
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float hitDelay = 0.1f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private bool drawGizmos = true;

        private void Update()
        {
            // Use J for an exact one-shot sample. KnightController's first hit uses a ~0.1s delay.
            if (Input.GetKeyDown(KeyCode.J))
                StartCoroutine(CheckAfterDelay());
        }

        private IEnumerator CheckAfterDelay()
        {
            yield return new WaitForSeconds(hitDelay);
            Vector2 center = GetAttackCenter();
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRange);
            Debug.Log($"[Map3 Hit Diagnostic] center={center}, radius={attackRange}, overlap hits={hits.Length}", this);

            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;
                bool tagMatch = hit.CompareTag("Enemy");
                bool layerMatch = (enemyLayer.value & (1 << hit.gameObject.layer)) != 0;
                EnemyHealth health = hit.GetComponentInParent<EnemyHealth>();
                Debug.Log(
                    $"[Map3 Hit Diagnostic] hit={hit.name}, tag={hit.tag}, layer={LayerMask.LayerToName(hit.gameObject.layer)} " +
                    $"(id={hit.gameObject.layer}), EnemyTag={tagMatch}, EnemyLayer={layerMatch}, " +
                    $"EnemyHealth={(health ? health.GetType().Name : "NOT FOUND")}, " +
                    $"eligible={(tagMatch || layerMatch) && health != null}", hit);
            }
        }

        private Vector2 GetAttackCenter()
        {
            float direction = transform.localScale.x < 0f ? -1f : 1f;
            return (Vector2)transform.position + new Vector2(direction * 0.9f, 0.2f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetAttackCenter(), attackRange);
        }
    }
}
