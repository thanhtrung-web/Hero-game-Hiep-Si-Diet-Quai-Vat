using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gan component nay len HealthBarCanvas (con cua Enemy/Boss).
/// Keo Image Fill vao o Fill Image. Tu tim EnemyHealth o GameObject cha.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHealthBar1 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Image fillImage;

    [Header("Options")]
    [SerializeField] private bool hideWhenDead = true;

    private void Awake()
    {
        // Works with EnemyHealth and controllers that inherit EnemyHealth.
        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void LateUpdate()
    {
        if (enemyHealth == null || fillImage == null)
            return;

        float ratio = enemyHealth.MaxHealth > 0
            ? (float)enemyHealth.CurrentHealth / enemyHealth.MaxHealth
            : 0f;

        fillImage.fillAmount = Mathf.Clamp01(ratio);

        if (hideWhenDead && enemyHealth.IsDead)
            gameObject.SetActive(false);
    }
}
