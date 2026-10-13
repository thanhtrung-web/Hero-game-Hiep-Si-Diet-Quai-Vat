using UnityEngine;

namespace HeroGame.Map3
{
    // Attach this marker to the root Player; a Player tag is not required.
    [DisallowMultipleComponent]
    public class CavePlayerTarget : MonoBehaviour
    {
        public Transform aimPoint;
        public CaveHealth health;
        public Vector2 Position => aimPoint ? (Vector2)aimPoint.position : (Vector2)transform.position;
        public bool IsAlive => isActiveAndEnabled && (!health || !health.IsDead);
        private void Awake() { if (!health) health = GetComponent<CaveHealth>(); }
        public void MarkDead() { enabled = false; }
        public void MarkAlive() { enabled = true; }
    }
}
