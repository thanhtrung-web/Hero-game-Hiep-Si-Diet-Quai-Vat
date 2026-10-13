using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace HeroGame.Map3
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CaveHealth), typeof(CavePlayerTarget))]
    public class CaveEnemyDemoPlayer : MonoBehaviour
    {
        public LayerMask groundMask;
        public float speed = 5;
        public float jumpSpeed = 8;
        public float attackDamage = 20;
        public GameObject[] enemyPrefabs;
        public Vector2[] enemySpawnPoints;
        private Rigidbody2D body;
        private Collider2D shape;
        private CaveHealth health;
        private SpriteRenderer visual;
        private Vector2 spawn;
        private float horizontal;
        private float facing = 1;
        private float attackReady;
        private bool jumpQueued;
        private Camera demoCamera;
        private ContactFilter2D terrainFilter;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); shape = GetComponent<Collider2D>();
            health = GetComponent<CaveHealth>(); visual = GetComponent<SpriteRenderer>();
            spawn = body.position; demoCamera = Camera.main;
            terrainFilter = new ContactFilter2D { useTriggers = false };
            terrainFilter.SetLayerMask(groundMask);
        }
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame) ResetDemo();
            if (health.IsDead) { horizontal = 0; return; }
            horizontal = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);
            if (horizontal != 0) facing = Mathf.Sign(horizontal);
            if (keyboard.spaceKey.wasPressedThisFrame) jumpQueued = true;
            if (keyboard.jKey.wasPressedThisFrame && Time.time >= attackReady)
            {
                attackReady = Time.time + 0.35f;
                var attacked = new HashSet<CaveHealth>();
                foreach (Collider2D c in Physics2D.OverlapCircleAll(body.position + Vector2.right * facing, 1.1f))
                {
                    CaveEnemy enemy = c.GetComponentInParent<CaveEnemy>();
                    if (!enemy || !attacked.Add(enemy.Health)) continue;
                    Vector2 delta = (Vector2)c.ClosestPoint(body.position) - body.position;
                    if (Physics2D.Raycast(body.position, delta.normalized, terrainFilter, groundHits, delta.magnitude) == 0)
                        enemy.Health.TakeDamage(attackDamage);
                }
            }
            if (keyboard.kKey.wasPressedThisFrame) health.Kill();
        }
        private void FixedUpdate()
        {
            if (health.IsDead) { body.linearVelocity = Vector2.zero; body.simulated = false; return; }
            float vertical = body.linearVelocity.y;
            Bounds b = shape.bounds;
            bool grounded = Physics2D.BoxCast(new Vector2(b.center.x, b.min.y + 0.06f),
                new Vector2(b.size.x * 0.85f, 0.05f), 0, Vector2.down, terrainFilter, groundHits, 0.15f) > 0;
            if (jumpQueued && grounded) vertical = jumpSpeed;
            jumpQueued = false;
            body.linearVelocity = new Vector2(horizontal * speed, vertical);
            if (body.position.y < -10) health.Kill();
        }
        private void LateUpdate()
        {
            if (!demoCamera) return;
            demoCamera.transform.position = Vector3.Lerp(demoCamera.transform.position,
                new Vector3(transform.position.x + 3, 3.5f, -10), Time.deltaTime * 5);
        }
        private void ResetDemo()
        {
            foreach (CaveEnemy enemy in FindObjectsByType<CaveEnemy>())
                if (enemy.gameObject.scene == gameObject.scene)
                { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            body.simulated = true; body.position = spawn; body.linearVelocity = Vector2.zero;
            health.RestoreFullHealth(); jumpQueued = false; attackReady = 0;
            for (int i = 0; i < enemyPrefabs.Length && i < enemySpawnPoints.Length; i++)
                if (enemyPrefabs[i]) Instantiate(enemyPrefabs[i], enemySpawnPoints[i], Quaternion.identity);
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 720, 82), "");
            GUI.Label(new Rect(24, 20, 690, 24), "MAP3 ENEMY DEMO | A/D: move | Space: jump | J: attack | K: die | R: reset");
            GUI.Label(new Rect(24, 44, 690, 24), "Player HP: " + Mathf.CeilToInt(health.Current) + "/" + health.maxHealth +
                (health.IsDead ? "  DEAD - press R" : "  Blue square = test player"));
            GUI.Label(new Rect(24, 66, 690, 22), "Goblin: fast melee | Slime: slow heavy melee | Bat: hovering + swoop");
            foreach (CaveEnemy enemy in FindObjectsByType<CaveEnemy>())
            {
                if (!demoCamera || !enemy.Health || enemy.gameObject.scene != gameObject.scene) continue;
                Vector3 screen = demoCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.1f);
                if (screen.z > 0) GUI.Label(new Rect(screen.x - 70, Screen.height - screen.y - 15, 150, 22),
                    enemy.kind + " " + Mathf.CeilToInt(enemy.Health.Current) + " " + enemy.State);
            }
            if (visual) visual.color = health.IsDead ? Color.red : new Color(0.25f, 0.75f, 1);
        }
    }
}
