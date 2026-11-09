using System.Collections.Generic;
using UnityEngine;

namespace HeroGame.Map3
{
    public enum CaveEnemyKind { Goblin, Slime, Bat, Golem }
    public enum CaveEnemyState { Idle, Move, Attack, Hurt, Dead }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(CaveEnemyHealth))]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class CaveEnemy : MonoBehaviour
    {
        [Header("Enemy Type")]
        public CaveEnemyKind kind;

        [Header("Detection")]
        public LayerMask groundMask;
        public float detectionRange = 6f;
        public float leashRange = 10f;
        public bool requireLineOfSight = true;
        public bool spriteFacesRight = true;

        [Header("Movement")]
        public float patrolDistance = 3f;
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float idleSeconds = 1f;
        public float hoverAmplitude = 0.15f;

        [Header("Attack")]
        public float attackRange = 1.4f;
        public float attackRadius = 0.8f;
        public Vector2 attackOffset = new Vector2(0.65f, 0f);
        public float damage = 10f;
        public float attackCooldown = 1.2f;
        // Cac gia tri nay la thoi gian toi thieu / fallback.
        // Neu clip trong Animator dai hon, code dung thoi luong cua clip.
        public float attackDuration = 0.8f;
        public float hurtDuration = 0.25f;
        public float deathDuration = 0.6f;
        public float corpseSeconds = 1f;

        [Header("Bat")]
        public float swoopSpeed = 5f;
        [Range(0f, 1f)] public float swoopStart = 0.15f;
        [Range(0f, 1f)] public float swoopEnd = 0.75f;

        [Header("Debug")]
        public bool debugAttack = false;

        public CaveEnemyState State { get; private set; }
        public CaveEnemyHealth Health { get; private set; }
        public Vector2 Home { get; private set; }
        public bool FacingRight => facing > 0f;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private Rigidbody2D body;
        private Collider2D shape;
        private Collider2D playerCollider;
        private SpriteRenderer visual;
        private Animator anim;
        private Transform player;
        public Transform Player => player;
        private KnightController playerKnight;

        private Vector2 velocity;
        private Vector2 swoopTarget;
        private float facing = 1f;
        private float stateEnd;
        private float attackReady;
        private float attackStart;
        private float attackSeconds;
        private float hurtSeconds;
        private float deathSeconds;
        private float searchAt;
        private float nextDebugAt;
        private bool initialized;
        private bool alerted;
        private bool hitDone;
        private bool swoopSoundPlayed;
        private readonly HashSet<KnightController> victims = new HashSet<KnightController>();

        private bool PlayerAlive => player && player.gameObject.activeInHierarchy &&
                                    (!playerKnight || playerKnight.CurrentHealth > 0);

        private Vector2 PlayerPosition => playerCollider && playerCollider.enabled
            ? (Vector2)playerCollider.bounds.center
            : (player ? (Vector2)player.position : Vector2.zero);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            visual = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
            Health = GetComponent<CaveEnemyHealth>();
            // Knight filters hit colliders by Enemy tag or Enemy Layer.
            // Ensure all enemy hit colliders carry Enemy tag, not attack triggers outside enemy.
            if (!CompareTag("Enemy")) gameObject.tag = "Enemy";
            foreach (Collider2D hitCollider in GetComponentsInChildren<Collider2D>(true))
                if (hitCollider && !hitCollider.isTrigger) hitCollider.gameObject.tag = "Enemy";

            Home = body.position;

            attackSeconds = AnimationSeconds("Attack", attackDuration);
            hurtSeconds = AnimationSeconds("Hurt", hurtDuration);
            deathSeconds = AnimationSeconds("Dead", deathDuration);
        }

        // Called from Map3 health component; no edits to EnemyHealth or Knight.
        public void NotifyDamaged()
        {
            if (State == CaveEnemyState.Dead) return;
            alerted = true;
            if (State != CaveEnemyState.Hurt)
                Enter(CaveEnemyState.Hurt, hurtSeconds + 0.15f);
        }

        public void NotifyDeath()
        {
            Die();
        }

        private void Start()
        {
            ValidateAnimator();
            Enter(CaveEnemyState.Idle, idleSeconds);
        }

        // Doc do dai clip trong Animator de khong ngat Attack/Hurt qua som.
        private float AnimationSeconds(string action, float configuredSeconds)
        {
            float seconds = Mathf.Max(0.05f, configuredSeconds);
            if (!anim.runtimeAnimatorController) return seconds;

            string targetName = kind + "_" + action;
            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            {
                if (clip && clip.name == targetName)
                {
                    float speed = Mathf.Max(0.01f, Mathf.Abs(anim.speed));
                    return Mathf.Max(seconds, clip.length / speed);
                }
            }
            return seconds;
        }

        private void ValidateAnimator()
        {
            if (!anim.runtimeAnimatorController)
            {
                Debug.LogError($"[{name}] Animator chua duoc gan Controller.", this);
                return;
            }

            AnimatorControllerParameter[] parameters = anim.parameters;
            int[] required = { IsMovingHash, AttackHash, HurtHash, DeadHash };
            string[] names = { "IsMoving", "Attack", "Hurt", "Dead" };
            AnimatorControllerParameterType[] types =
            {
                AnimatorControllerParameterType.Bool,
                AnimatorControllerParameterType.Trigger,
                AnimatorControllerParameterType.Trigger,
                AnimatorControllerParameterType.Trigger
            };

            for (int i = 0; i < required.Length; i++)
            {
                bool valid = false;
                foreach (AnimatorControllerParameter parameter in parameters)
                {
                    if (parameter.nameHash == required[i] && parameter.type == types[i])
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                    Debug.LogError($"[{name}] Animator thieu Parameter {names[i]} ({types[i]}).", this);
            }
        }

        private void FindPlayer()
        {
            searchAt = Time.time + 1f;
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            player = found ? found.transform : null;
            playerKnight = found ? found.GetComponentInChildren<KnightController>() : null;
            playerCollider = null;
            if (!found) return;

            // Uu tien collider vat ly, tranh lay nham trigger/pham vi tan cong.
            Collider2D[] colliders = found.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D candidate in colliders)
            {
                if (candidate.enabled && !candidate.isTrigger)
                {
                    playerCollider = candidate;
                    break;
                }
            }
            if (!playerCollider && colliders.Length > 0)
                playerCollider = colliders[0];
        }

        private void Update()
        {
            if (State == CaveEnemyState.Dead) return;
            if (!PlayerAlive && Time.time >= searchAt) FindPlayer();

            if (State == CaveEnemyState.Attack || State == CaveEnemyState.Hurt)
            {
                // Animation Event FinishAction() ket thuc som hon.
                // Timeout chi la du phong neu Event thieu.
                if (Time.time >= stateEnd) FinishAction();
                return;
            }

            float distance = PlayerAlive
                ? Vector2.Distance(body.position, PlayerPosition)
                : float.MaxValue;

            if (PlayerAlive && distance <= detectionRange && CanSee(PlayerPosition))
                alerted = true;

            if (!PlayerAlive || distance > detectionRange * 1.5f ||
                Vector2.Distance(body.position, Home) > leashRange)
                alerted = false;

            if (alerted) ChaseOrAttack(distance);
            else Patrol();
        }

        private void ChaseOrAttack(float distance)
        {
            Vector2 delta = PlayerPosition - body.position;
            Face(delta.x);

            // Khoang cach tu mep collider den mep collider thay vi tam 2 object.
            float attackDistance = distance;
            if (shape.enabled && playerCollider && playerCollider.enabled)
                attackDistance = Mathf.Max(0f, shape.Distance(playerCollider).distance);

            bool verticalOK = true;
            if (kind != CaveEnemyKind.Bat && playerCollider && playerCollider.enabled)
            {
                Bounds a = shape.bounds;
                Bounds b = playerCollider.bounds;
                float verticalGap = Mathf.Max(0f,
                    Mathf.Max(a.min.y, b.min.y) - Mathf.Min(a.max.y, b.max.y));
                verticalOK = verticalGap <= 0.35f;
            }

            bool sightOK = CanSee(PlayerPosition);

            if (debugAttack && Time.time >= nextDebugAt)
            {
                nextDebugAt = Time.time + 1f;
                Debug.Log($"[{name}] State={State}, center={distance:F2}, edge={attackDistance:F2}, " +
                          $"range={attackRange:F2}, vertical={verticalOK}, LOS={sightOK}, " +
                          $"cooldown={Mathf.Max(0f, attackReady - Time.time):F2}", this);
            }

            if (attackDistance <= attackRange && verticalOK && sightOK)
            {
                TryStartAttack();
                return;
            }

            Vector2 destination = PlayerPosition;
            if (kind == CaveEnemyKind.Bat)
                destination += Vector2.up * 0.8f;
            Move(destination, chaseSpeed, false);
        }

        private void TryStartAttack()
        {
            velocity = Vector2.zero;
            if (Time.time < attackReady)
            {
                Enter(CaveEnemyState.Idle);
                return;
            }

            hitDone = false;
            swoopSoundPlayed = false;
            attackStart = Time.time;
            attackReady = Time.time + attackSeconds + attackCooldown;
            swoopTarget = PlayerPosition;
            Enter(CaveEnemyState.Attack, attackSeconds + 0.15f);
        }

        private void Patrol()
        {
            if (State == CaveEnemyState.Idle && Time.time < stateEnd) return;

            if ((body.position.x - Home.x) * facing >= patrolDistance)
            {
                Face(-facing);
                Enter(CaveEnemyState.Idle, idleSeconds);
                return;
            }

            Move(Home + Vector2.right * facing * patrolDistance, patrolSpeed, true);
        }

        private void FixedUpdate()
        {
            if (State == CaveEnemyState.Dead) return;

            Vector2 next = velocity;
            if (State == CaveEnemyState.Attack || State == CaveEnemyState.Hurt)
                next = Vector2.zero;

            if (kind == CaveEnemyKind.Bat && State == CaveEnemyState.Attack)
            {
                float phase = (Time.time - attackStart) / Mathf.Max(0.1f, attackSeconds);
                if (phase >= swoopStart && phase <= swoopEnd)
                {
                    if (!swoopSoundPlayed)
                    {
                        swoopSoundPlayed = true;
                        GetComponent<CaveEnemyAudio>()?.PlaySwoopSound();
                    }
                    Vector2 delta = swoopTarget - body.position;
                    next = delta.normalized * Mathf.Min(swoopSpeed,
                        delta.magnitude / Time.fixedDeltaTime);
                }
            }

            if (kind == CaveEnemyKind.Bat && State == CaveEnemyState.Idle)
                next.y = Mathf.Sin(Time.time * 2f) * hoverAmplitude;

            body.linearVelocity = kind == CaveEnemyKind.Bat
                ? next
                : new Vector2(next.x, body.linearVelocity.y);
        }

        private bool CanSee(Vector2 point)
        {
            if (!requireLineOfSight || groundMask.value == 0) return true;
            return !Physics2D.Linecast(shape.bounds.center, point, groundMask).collider;
        }

        private bool SafeGround()
        {
            if (groundMask.value == 0) return true;
            Bounds bounds = shape.bounds;
            Vector2 foot = new Vector2(
                bounds.center.x + facing * (bounds.extents.x + 0.12f),
                bounds.min.y + 0.12f);

            bool hasGround = Physics2D.Raycast(foot, Vector2.down, 0.65f, groundMask).collider;
            bool blocked = Physics2D.Raycast(bounds.center, Vector2.right * facing,
                bounds.extents.x + 0.2f, groundMask).collider;
            return hasGround && !blocked;
        }

        private void Move(Vector2 destination, float speed, bool patrol)
        {
            Vector2 delta = destination - body.position;
            Face(delta.x);

            if (kind != CaveEnemyKind.Bat && !SafeGround())
            {
                if (patrol) Face(-facing);
                Enter(CaveEnemyState.Idle, idleSeconds);
                return;
            }

            velocity = kind == CaveEnemyKind.Bat
                ? delta.normalized * Mathf.Min(speed, delta.magnitude / Time.fixedDeltaTime)
                : Vector2.right * facing * speed;
            Enter(CaveEnemyState.Move);
        }

        private void Face(float direction)
        {
            if (Mathf.Abs(direction) < 0.02f) return;
            facing = Mathf.Sign(direction);
            visual.flipX = spriteFacesRight ? facing < 0f : facing > 0f;
        }

        // Chuyen AI State va GUI PARAMETER sang Animator.
        // Khong con dung anim.Play() de ep Animator phat tung State.
        private void Enter(CaveEnemyState state, float duration = 0f)
        {
            if (initialized && State == state) return;

            initialized = true;
            State = state;
            stateEnd = Time.time + duration;
            if (state != CaveEnemyState.Move)
                velocity = Vector2.zero;

            switch (state)
            {
                case CaveEnemyState.Idle:
                    anim.SetBool(IsMovingHash, false);
                    break;
                case CaveEnemyState.Move:
                    anim.SetBool(IsMovingHash, true);
                    break;
                case CaveEnemyState.Attack:
                    anim.SetBool(IsMovingHash, false);
                    anim.ResetTrigger(HurtHash);
                    anim.SetTrigger(AttackHash);
                    break;
                case CaveEnemyState.Hurt:
                    anim.SetBool(IsMovingHash, false);
                    anim.ResetTrigger(AttackHash);
                    anim.SetTrigger(HurtHash);
                    break;
                case CaveEnemyState.Dead:
                    anim.SetBool(IsMovingHash, false);
                    anim.ResetTrigger(AttackHash);
                    anim.ResetTrigger(HurtHash);
                    anim.SetTrigger(DeadHash);
                    break;
            }

            if (debugAttack)
                Debug.Log($"[{name}] AI -> {state}", this);
        }

        // Animation Event dat trong clip *_Attack.anim.
        public void DealAttackHit()
        {
            if (State != CaveEnemyState.Attack || hitDone) return;

            hitDone = true;
            victims.Clear();
            Vector2 center = body.position +
                new Vector2(attackOffset.x * facing, attackOffset.y);

            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, attackRadius))
            {
                Transform root = hit.transform;
                while (root && !root.CompareTag("Player"))
                    root = root.parent;
                if (!root) continue;

                KnightController knight = root.GetComponentInChildren<KnightController>();
                if (!knight || knight.CurrentHealth <= 0 || !victims.Add(knight)) continue;
                if (CanSee(hit.bounds.center))
                    knight.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)));
            }
        }

        // Animation Event gan cuoi clip Attack va Hurt.
        public void FinishAction()
        {
            if (State != CaveEnemyState.Attack && State != CaveEnemyState.Hurt)
                return;

            Enter(CaveEnemyState.Idle, 0.05f);
        }

        private void Die()
        {
            if (State == CaveEnemyState.Dead) return;

            Enter(CaveEnemyState.Dead);
            body.linearVelocity = Vector2.zero;
            shape.enabled = false;
            body.simulated = false;
            // Allow a death sound event anywhere in the death clip to finish.
            CaveEnemyAudio audio = GetComponent<CaveEnemyAudio>();
            float soundSeconds = audio ? audio.DeathSoundDuration : 0f;
            Destroy(gameObject, deathSeconds + Mathf.Max(corpseSeconds, soundSeconds));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)transform.position +
                new Vector2(attackOffset.x * facing, attackOffset.y), attackRadius);
        }
    }
}
