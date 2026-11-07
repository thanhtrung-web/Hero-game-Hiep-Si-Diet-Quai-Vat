using UnityEngine;
using UnityEngine.Events;

namespace HeroGame.Map3
{
    public interface ICaveDamageable
    {
        void TakeDamage(float amount);
    }

    public interface ICaveDamageBlocker
    {
        bool BlocksDamage { get; }
    }

    [DisallowMultipleComponent]
    public class CaveHealth : MonoBehaviour, ICaveDamageable
    {
        [Min(1)] public float maxHealth = 100f;
        [Min(0)] public float invulnerabilitySeconds;

        [SerializeField] private float currentHealth;

        public UnityEvent<float> onDamaged = new UnityEvent<float>();
        public UnityEvent onDeath = new UnityEvent();

        public float Current => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        private float nextDamageTime;

        private void Awake()
        {
            RestoreFullHealth();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f || Time.time < nextDamageTime)
                return;

            // Kiểm tra khiên trước khi trừ HP.
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component && component.isActiveAndEnabled &&
                    component is ICaveDamageBlocker blocker &&
                    blocker.BlocksDamage)
                    return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            nextDamageTime = Time.time + invulnerabilitySeconds;

            if (IsDead)
                onDeath.Invoke();
            else
                onDamaged.Invoke(amount);
        }

        public void RestoreFullHealth()
        {
            currentHealth = maxHealth;
            nextDamageTime = 0f;
        }

        public void Kill()
        {
            if (IsDead) return;

            currentHealth = 0f;
            onDeath.Invoke();
        }
    }
}