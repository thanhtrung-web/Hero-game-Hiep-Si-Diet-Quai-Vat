using UnityEngine;
using UnityEngine.Events;

namespace HeroGame.Map3
{
    public interface ICaveDamageable { void TakeDamage(float amount); }

    [DisallowMultipleComponent]
    public class CaveHealth : MonoBehaviour, ICaveDamageable
    {
        [Min(1)] public float maxHealth = 50;
        [Min(0)] public float invulnerabilitySeconds;
        public UnityEvent<float> onDamaged = new UnityEvent<float>();
        public UnityEvent onDeath = new UnityEvent();
        public float Current { get; private set; }
        public bool IsDead => Current <= 0;
        private float nextDamageTime;

        private void Awake() { Current = maxHealth; }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0 || Time.time < nextDamageTime) return;
            Current = Mathf.Max(0, Current - amount);
            nextDamageTime = Time.time + invulnerabilitySeconds;
            if (IsDead) onDeath.Invoke();
            else onDamaged.Invoke(amount);
        }

        public void RestoreFullHealth()
        {
            Current = maxHealth;
            nextDamageTime = 0;
        }

        public void Kill()
        {
            if (IsDead) return;
            Current = 0;
            onDeath.Invoke();
        }
    }
}
