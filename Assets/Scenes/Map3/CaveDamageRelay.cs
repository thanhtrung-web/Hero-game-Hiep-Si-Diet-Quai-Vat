using UnityEngine;
using UnityEngine.Events;

namespace HeroGame.Map3
{
    // Optional adapter when Player already has a health system with TakeDamage(float).
    // Do not attach both this relay and CaveHealth to the same Player.
    [DisallowMultipleComponent]
    public class CaveDamageRelay : MonoBehaviour, ICaveDamageable
    {
        public UnityEvent<float> onDamage = new UnityEvent<float>();
        public void TakeDamage(float amount) { if (amount > 0) onDamage.Invoke(amount); }
    }
}
