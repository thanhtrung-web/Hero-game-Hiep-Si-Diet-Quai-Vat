using System.Collections;
using UnityEngine;

public class SoulPillarHazard : MonoBehaviour
{
    [Header("Hazard Settings")]
    [SerializeField] private int damage = 26;
    [SerializeField] private float hitRadius = 1.0f;
    [SerializeField] private float eruptionDelay = 0.35f;
    [SerializeField] private float lifeTime = 1.1f;

    private bool hasDealtDamage = false;

    private void Start()
    {
        StartCoroutine(EruptRoutine());
        Destroy(gameObject, lifeTime);
    }

    private IEnumerator EruptRoutine()
    {
        yield return new WaitForSeconds(eruptionDelay);
        if (!hasDealtDamage)
        {
            hasDealtDamage = true;
            CheckAndDealDamage();
        }
    }

    public void TriggerDamage()
    {
        if (!hasDealtDamage)
        {
            hasDealtDamage = true;
            CheckAndDealDamage();
        }
    }

    private void CheckAndDealDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 0.5f, hitRadius);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var knight = col.GetComponentInParent<KnightController>();
                if (knight != null) knight.TakeDamage(damage);

                var mage = col.GetComponentInParent<PlayerHealth>();
                if (mage != null) mage.TakeDamage(damage);
            }
        }
    }
}
