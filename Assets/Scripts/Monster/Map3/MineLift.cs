using UnityEngine;

namespace HeroGame.Map3
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MineLift : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float travelHeight = 8f;
        [SerializeField, Min(1f)] private float travelSeconds = 4f;
        [SerializeField, Min(0f)] private float dwellSeconds = 1.4f;

        private Rigidbody2D platform;
        private Vector2 lowerStop;
        private float elapsed;

        private void Awake()
        {
            platform = GetComponent<Rigidbody2D>();
            lowerStop = platform.position;
            // A rider can jump onto the deck from below and step off at either landing.
            var effector = gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            GetComponent<BoxCollider2D>().usedByEffector = true;
        }

        private void FixedUpdate()
        {
            float halfCycle = dwellSeconds + travelSeconds;
            elapsed = Mathf.Repeat(elapsed + Time.fixedDeltaTime, 2f * halfCycle);
            bool descending = elapsed >= halfCycle;
            float progress = Mathf.Clamp01(((descending ? elapsed - halfCycle : elapsed) - dwellSeconds) / travelSeconds);
            float height = Mathf.SmoothStep(0f, travelHeight, progress);
            platform.MovePosition(lowerStop + Vector2.up * (descending ? travelHeight - height : height));
        }
    }
}
