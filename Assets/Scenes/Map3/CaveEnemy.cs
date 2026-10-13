using UnityEngine;
using System.Collections.Generic;

namespace HeroGame.Map3
{
    public enum CaveEnemyKind { Goblin, Slime, Bat }
    public enum CaveEnemyState { Idle, Move, Attack, Hurt, Dead }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(CaveHealth))]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class CaveEnemy : MonoBehaviour
    {
        public CaveEnemyKind kind;
        [Header("Target and terrain")]
        public CavePlayerTarget target;
        public LayerMask groundMask;
        [Min(0.1f)] public float detectionRange = 6;
        [Min(0.1f)] public float leashRange = 10;
        [Min(0.1f)] public float patrolDistance = 3;
        public bool requireLineOfSight = true;
        public bool spriteFacesRight = true;
        [Header("Movement")]
        [Min(0)] public float patrolSpeed = 1;
        [Min(0)] public float chaseSpeed = 2;
        [Min(0)] public float idleSeconds = 1;
        [Min(0)] public float hoverAmplitude = 0.15f;
        [Header("Attack")]
        [Min(0.1f)] public float attackRange = 1.4f;
        [Min(0.05f)] public float attackRadius = 0.8f;
        public Vector2 attackOffset = new Vector2(0.65f, 0);
        [Min(1)] public float damage = 10;
        [Min(0.1f)] public float attackCooldown = 1.2f;
        [Min(0.1f)] public float attackDuration = 0.8f;
        [Min(0.1f)] public float hurtDuration = 0.25f;
        [Min(0.1f)] public float deathDuration = 0.6f;
        [Min(0)] public float corpseSeconds = 1;
        [Header("Bat swoop")]
        [Min(0.1f)] public float swoopSpeed = 4;
        [Range(0, 1)] public float swoopStart = 0.15f;
        [Range(0, 1)] public float swoopEnd = 0.65f;
        public CaveEnemyState State { get; private set; }
        public Vector2 Home { get; private set; }
        public CaveHealth Health { get; private set; }
        public bool FacingRight => facing > 0;
        private Rigidbody2D body;
        private Collider2D shape;
        private SpriteRenderer visual;
        private Animator animator;
        private float facing = 1;
        private float stateEnds;
        private float attackReady;
        private float attackStarted;
        private float reacquireAt;
        private bool alerted;
        private bool hitThisAttack;
        private bool stateInitialized;
        private Vector2 requestedVelocity;
        private Vector2 swoopDestination;
        private ContactFilter2D terrainFilter;
        private readonly RaycastHit2D[] rayHits = new RaycastHit2D[4];

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); shape = GetComponent<Collider2D>();
            visual = GetComponent<SpriteRenderer>(); animator = GetComponent<Animator>();
            Health = GetComponent<CaveHealth>(); Home = body.position;
            terrainFilter = new ContactFilter2D { useTriggers = false };
            terrainFilter.SetLayerMask(groundMask);
        }

        private void OnEnable()
        {
            Health.onDamaged.AddListener(OnDamaged);
            Health.onDeath.AddListener(Die);
        }
        private void OnDisable()
        {
            Health.onDamaged.RemoveListener(OnDamaged);
            Health.onDeath.RemoveListener(Die);
        }
        private void Start() { Enter(CaveEnemyState.Idle, idleSeconds); }

        private void Update()
        {
            if (State == CaveEnemyState.Dead) return;
            if ((!target || !target.IsAlive) && Time.time >= reacquireAt)
            {
                target = FindAnyObjectByType<CavePlayerTarget>();
                reacquireAt = Time.time + 1;
            }
            if (State == CaveEnemyState.Attack || State == CaveEnemyState.Hurt)
            {
                if (Time.time >= stateEnds) FinishAction();
                return;
            }
            bool visible = target && target.IsAlive &&
                Vector2.Distance(body.position, target.Position) <= detectionRange && HasSight();
            if (visible) alerted = true;
            if (!target || !target.IsAlive || Vector2.Distance(body.position, Home) > leashRange ||
                Vector2.Distance(body.position, target.Position) > detectionRange * 1.5f) alerted = false;
            if (alerted)
            {
                Vector2 delta = target.Position - body.position;
                Face(delta.x);
                bool verticalReach = kind == CaveEnemyKind.Bat || Mathf.Abs(delta.y) < 1.6f;
                if (delta.magnitude <= attackRange && verticalReach && HasSight())
                {
                    requestedVelocity = Vector2.zero;
                    if (Time.time >= attackReady) BeginAttack();
                    else Enter(CaveEnemyState.Idle, 0);
                }
                else if (kind == CaveEnemyKind.Bat)
                    MoveTowards(target.Position + Vector2.up * 0.8f, chaseSpeed);
                else MoveGround(delta.x, chaseSpeed, false);
                return;
            }
            if (State == CaveEnemyState.Idle && Time.time < stateEnds)
            {
                requestedVelocity = kind == CaveEnemyKind.Bat
                    ? new Vector2(0, Mathf.Sin(Time.time * 2) * hoverAmplitude) : Vector2.zero;
                return;
            }
            if ((body.position.x - Home.x) * facing >= patrolDistance)
            {
                facing *= -1; UpdateFacing();
                Enter(CaveEnemyState.Idle, idleSeconds); requestedVelocity = Vector2.zero; return;
            }
            if (kind == CaveEnemyKind.Bat)
                MoveTowards(new Vector2(Home.x + facing * patrolDistance, Home.y), patrolSpeed);
            else MoveGround(facing, patrolSpeed, true);
        }

        private void FixedUpdate()
        {
            if (State == CaveEnemyState.Dead) return;
            Vector2 velocity = requestedVelocity;
            if (State == CaveEnemyState.Attack || State == CaveEnemyState.Hurt)
            {
                velocity = Vector2.zero;
                float phase = (Time.time - attackStarted) / attackDuration;
                if (State == CaveEnemyState.Attack && kind == CaveEnemyKind.Bat &&
                    phase >= swoopStart && phase <= swoopEnd)
                {
                    Vector2 delta = swoopDestination - body.position;
                    velocity = delta.normalized * Mathf.Min(swoopSpeed, delta.magnitude / Time.fixedDeltaTime);
                }
            }
            body.linearVelocity = kind == CaveEnemyKind.Bat ? velocity :
                new Vector2(velocity.x, body.linearVelocity.y);
        }

        private bool HasSight()
        {
            if (!requireLineOfSight || groundMask.value == 0 || !target) return true;
            Vector2 origin = shape.bounds.center;
            Vector2 delta = target.Position - origin;
            return Physics2D.Raycast(origin, delta.normalized, terrainFilter, rayHits, delta.magnitude) == 0;
        }
        private bool SafeGroundAhead()
        {
            if (groundMask.value == 0) return true;
            Bounds b = shape.bounds;
            Vector2 foot = new Vector2(b.center.x + facing * (b.extents.x + 0.12f), b.min.y + 0.12f);
            bool floor = Physics2D.Raycast(foot, Vector2.down, terrainFilter, rayHits, 0.65f) > 0;
            Vector2 wallOrigin = new Vector2(b.center.x, b.min.y + Mathf.Min(0.55f, b.size.y * 0.5f));
            bool wall = Physics2D.Raycast(wallOrigin, Vector2.right * facing, terrainFilter, rayHits, b.extents.x + 0.2f) > 0;
            return floor && !wall;
        }
        private void MoveGround(float direction, float speed, bool patrolling)
        {
            Face(direction);
            if (!SafeGroundAhead())
            {
                requestedVelocity = Vector2.zero;
                if (patrolling) { facing *= -1; UpdateFacing(); }
                Enter(CaveEnemyState.Idle, idleSeconds); return;
            }
            requestedVelocity = new Vector2(facing * speed, 0);
            Enter(CaveEnemyState.Move, 0);
        }
        private void MoveTowards(Vector2 destination, float speed)
        {
            Vector2 delta = destination - body.position; Face(delta.x);
            requestedVelocity = delta.normalized * Mathf.Min(speed, delta.magnitude / Time.fixedDeltaTime);
            Enter(CaveEnemyState.Move, 0);
        }
        private void Face(float x)
        {
            if (Mathf.Abs(x) < 0.02f) return;
            facing = Mathf.Sign(x); UpdateFacing();
        }
        private void UpdateFacing() { visual.flipX = spriteFacesRight ? facing < 0 : facing > 0; }
        private void Enter(CaveEnemyState next, float duration)
        {
            if (stateInitialized && State == next && animator.GetCurrentAnimatorStateInfo(0).IsName(next.ToString())) return;
            stateInitialized = true; State = next; stateEnds = Time.time + duration;
            animator.Play(next.ToString(), 0, 0);
        }
        private void BeginAttack()
        {
            hitThisAttack = false; attackStarted = Time.time;
            attackReady = Time.time + attackDuration + attackCooldown;
            swoopDestination = target.Position;
            Enter(CaveEnemyState.Attack, attackDuration + 0.1f);
        }

        // Called once by the Attack animation event, after its windup.
        public void DealAttackHit()
        {
            if (State != CaveEnemyState.Attack || hitThisAttack) return;
            hitThisAttack = true;
            Vector2 center = body.position + new Vector2(attackOffset.x * facing, attackOffset.y);
            var victims = new HashSet<ICaveDamageable>();
            foreach (Collider2D c in Physics2D.OverlapCircleAll(center, attackRadius))
            {
                CavePlayerTarget marker = c.GetComponentInParent<CavePlayerTarget>();
                if (!marker || !marker.IsAlive) continue;
                Vector2 origin = shape.bounds.center;
                Vector2 delta = (Vector2)c.ClosestPoint(center) - origin;
                if (requireLineOfSight && groundMask.value != 0 &&
                    Physics2D.Raycast(origin, delta.normalized, terrainFilter, rayHits, delta.magnitude) > 0) continue;
                foreach (MonoBehaviour component in c.GetComponentsInParent<MonoBehaviour>())
                    if (component is ICaveDamageable victim && victims.Add(victim))
                    { victim.TakeDamage(damage); break; }
            }
        }
        public void FinishAction()
        {
            if (State != CaveEnemyState.Attack && State != CaveEnemyState.Hurt) return;
            requestedVelocity = Vector2.zero; Enter(CaveEnemyState.Idle, 0.05f);
        }
        private void OnDamaged(float amount)
        {
            if (State == CaveEnemyState.Dead) return;
            requestedVelocity = Vector2.zero; alerted = true;
            Enter(CaveEnemyState.Hurt, hurtDuration + 0.1f);
        }
        private void Die()
        {
            if (State == CaveEnemyState.Dead) return;
            Enter(CaveEnemyState.Dead, deathDuration);
            requestedVelocity = Vector2.zero; body.linearVelocity = Vector2.zero;
            shape.enabled = false; body.simulated = false;
            Destroy(gameObject, deathDuration + corpseSeconds);
        }
        private void OnDrawGizmosSelected()
        {
            Vector2 p = Application.isPlaying ? Home : (Vector2)transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p + Vector2.left * patrolDistance, p + Vector2.right * patrolDistance);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y), attackRadius);
        }
    }
}
