using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeroGame.Map3
{
    [RequireComponent(typeof(AudioSource))]
    public class GolemBoss : EnemyHealth
    {
        public RuntimeAnimatorController fxController;
        [SerializeField] private string bossTitle = "GOLEM";
        [SerializeField, Min(1)] private int bossHealth = 150;
        public int MaxHealth => bossHealth;
        public int CurrentHealth { get; private set; }
        public event System.Action<EnemyHealth> Damaged;
        public event System.Action<EnemyHealth> Died;
        public bool IsBoss => true;
        public string BossTitle => bossTitle;
        public float detectionRange = 5, leashRange = 16, speed = 1.5f;
        public float upgradedDetectionRange = 6, upgradedSpeed = 2;
        public int normalDamage = 15, upgradedDamage = 22;
        public float patrolDistance = 3;
        float DetectionRange => enraged ? upgradedDetectionRange : detectionRange;
        int Damage => enraged ? upgradedDamage : normalDamage;
        Animator bossAnimator, fxAnimator;
        AttackType? pendingImpact;
        float patrolDirection = 1f;
        float patrolWaitUntil;
        public LayerMask groundMask;
        [Header("Golem Sound Effects")]
        public AudioClip windupSound, punchSound, slamSound, waveSound, hurtSound, upgradeSound, deathSound;
        [Range(0f, 1f)]
        public float soundVolume = .7f;
        public UnityEngine.Audio.AudioMixerGroup soundMixerGroup;
        private AudioSource soundSource;
        private float nextHurtSound;
        private void PlaySound(AudioClip clip, bool replace = false)
        {
            if (!clip)
                return;
            if (replace)
                soundSource.Stop();
            Transform listener = target ? target.transform : Camera.main?.transform;
            if (!listener)
                return;
            float distance = Vector2.Distance(transform.position, listener.position);
            float gain = 1f - Mathf.InverseLerp(5f, 22f, distance);
            if (gain > 0f)
                soundSource.PlayOneShot(clip, soundVolume * gain);
        }

        public int CurrentHP => CurrentHealth;

        SpriteRenderer visual, fx;
        Rigidbody2D body;
        Collider2D shape;
        KnightController target;
        Collider2D playerCollider;
        float nextTargetSearch;
        float horizontalSpeed;

        void FixedUpdate()
        {
            if (!isDead && body.simulated)
                body.linearVelocity = new Vector2(horizontalSpeed, body.linearVelocity.y);
        }
        GameObject waveObject;
        void OnDestroy()
        {
            if (waveObject)
                Destroy(waveObject);
        }

        Vector2 home;
        bool busy, enraged, upgrading;
        float facing = 1, nextAttackTime, nextStaggerTime;
        enum AttackType { Punch, Slam, Shockwave }

        protected override void Awake()
        {
            soundSource = GetComponent<AudioSource>();
            soundSource.outputAudioMixerGroup = soundMixerGroup;
            bossAnimator = GetComponent<Animator>();
            CurrentHealth = MaxHealth;
            visual = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            home = transform.position;
            var child = new GameObject("Golem FX");
            child.transform.SetParent(transform, false);
            fx = child.AddComponent<SpriteRenderer>();
            fx.sharedMaterial = visual.sharedMaterial;
            fx.sortingOrder = visual.sortingOrder + 1;
            fxAnimator = child.AddComponent<Animator>();
            fxAnimator.runtimeAnimatorController = fxController;
            if (!fxController || !bossAnimator.runtimeAnimatorController || groundMask.value == 0)
                Debug.LogError("Assign the Golem body controller, FX controller and Ground Mask on the prefab.", this);
            fx.enabled = false;
        }

        void Patrol()
        {
            if (Time.time < patrolWaitUntil) { Idle(); return; }
            float offset = body.position.x - home.x;
            if (Mathf.Abs(offset) > patrolDistance)
                patrolDirection = -Mathf.Sign(offset);
            facing = patrolDirection;
            visual.flipX = facing < 0;
            if (!CanWalkForward())
            {
                patrolDirection = -patrolDirection;
                patrolWaitUntil = Time.time + 0.6f;
                Idle();
                return;
            }
            horizontalSpeed = facing * speed * 0.5f;
            bossAnimator.SetBool("IsMoving", true);
        }

        void Stop()
        {
            horizontalSpeed = 0;
            body.linearVelocity = new Vector2(0, body.linearVelocity.y);
            bossAnimator.SetBool("IsMoving", false);
        }

        void Update()
        {
            if (isDead)
                return;
            if ((!target || !playerCollider || !playerCollider.enabled) && Time.time >= nextTargetSearch)
            {
                nextTargetSearch = Time.time + 0.5f;
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player ? player.GetComponentInChildren<KnightController>() : null;
                playerCollider = null;
                if (target)
                {
                    foreach (var candidate in player.GetComponentsInChildren<Collider2D>())
                    {
                        if (!candidate.enabled || candidate.isTrigger) continue;
                        playerCollider = candidate;
                        break;
                    }
                }
            }

            if (busy)
                return;
            if (CurrentHP <= MaxHealth * 0.5f && !enraged)
            {
                StartCoroutine(Upgrade());
                return;
            }

            if (!target || target.CurrentHealth <= 0
                || Vector2.Distance(home, transform.position) > leashRange
                || Vector2.Distance(target.transform.position, transform.position) > DetectionRange)
            {
                Patrol();
                return;
            }

            if (!playerCollider || !playerCollider.enabled)
            {
                Idle();
                return;
            }
            Vector2 delta = (Vector2)playerCollider.bounds.center - (Vector2)shape.bounds.center;
            facing = delta.x < 0 ? -1 : 1;
            visual.flipX = facing < 0;
            bool sight = !Physics2D.Linecast(shape.bounds.center, playerCollider.bounds.center, groundMask);
            float gap = Mathf.Max(0, shape.Distance(playerCollider).distance);
            float attackRange = enraged ? 3.5f : 1.5f;
            if (sight && gap <= attackRange)
            {
                if (Time.time >= nextAttackTime)
                    StartCoroutine(Attack(ChooseAttack(gap)));
                else
                    Idle();
                return;
            }

            if (gap > 0.6f && sight && CanWalkForward())
            {
                float chaseSpeed = enraged ? upgradedSpeed : speed;
                horizontalSpeed = facing * chaseSpeed;
                bossAnimator.SetBool("IsMoving", true);
                return;
            }

            Idle();
        }

        void Idle()
        {
            Stop();
        }

        bool CanWalkForward()
        {
            Bounds bounds = shape.bounds;
            float ahead = bounds.center.x + facing * (bounds.extents.x + 0.15f);
            Vector2 foot = new Vector2(ahead, bounds.min.y + 0.15f);
            bool hasGround = Physics2D.Raycast(foot, Vector2.down, 0.7f, groundMask);
            bool blocked = Physics2D.Raycast(bounds.center, Vector2.right * facing, bounds.extents.x + 0.2f, groundMask);
            return hasGround && !blocked;
        }

        AttackType ChooseAttack(float gap)
        {
            if (gap <= 0.65f) return AttackType.Punch;
            if (!enraged || gap <= 1.5f) return AttackType.Slam;
            return AttackType.Shockwave;
        }

        // Wait for the controller to consume the trigger and follow its outgoing transition.
        static IEnumerator WaitForStateExit(Animator controller, string state, bool terminal = false)
        {
            int hash = Animator.StringToHash(state);
            bool entered = false;
            // Also covers a missing transition or a disabled/stalled Animator.
            float deadline = Time.time + 6f;
            while (controller && controller.isActiveAndEnabled && Time.time < deadline)
            {
                var info = controller.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash == hash)
                {
                    entered = true;
                    if (terminal && info.normalizedTime >= 1f) yield break;
                }
                else if (entered) yield break;
                yield return null;
            }
            Debug.LogError($"Golem timed out waiting for '{state}'. Check its Animator, transitions and clip.", controller);
        }

        IEnumerator Effect(string name, Vector2 local)
        {
            fx.transform.localPosition = local;
            fx.flipX = facing < 0;
            fx.enabled = true;
            fxAnimator.SetTrigger(name);
            yield return WaitForStateExit(fxAnimator, name);
            fx.enabled = false;
        }

        void Hit(Vector2 center, float radius, int damage, HashSet<KnightController> victims)
        {
            foreach (var collider in Physics2D.OverlapCircleAll(center, radius))
            {
                var knight = collider == playerCollider ? target : collider.GetComponentInParent<KnightController>();
                if (!knight || knight.CurrentHealth <= 0) continue;
                if (Physics2D.Linecast(shape.bounds.center, collider.bounds.center, groundMask)) continue;
                if (victims.Add(knight)) knight.TakeDamage(damage);
            }
        }

        IEnumerator Attack(AttackType attack)
        {
            busy = true;
            Stop();
            PlaySound(windupSound, true);
            string name = attack switch
            {
                AttackType.Punch => "AttackA",
                AttackType.Slam => "AttackB",
                _ => "AttackC"
            };
            pendingImpact = attack;
            bossAnimator.SetFloat("AttackSpeed", enraged ? 1.2f : 1f);
            bossAnimator.SetTrigger(name);
            // AttackA/B/C all transition to Reset when the impact clip finishes.
            yield return WaitForStateExit(bossAnimator, "Reset");
            if (pendingImpact.HasValue)
                Debug.LogError($"Golem: {name} finished without OnAttackImpact. Check its clip event.", this);
            pendingImpact = null;
            nextAttackTime = Time.time + (enraged ? .65f : 1.2f);
            busy = false;
        }

        // Called once by the impact event in each attack clip.
        public void OnAttackImpact()
        {
            if (isDead || !pendingImpact.HasValue) return;
            var attack = pendingImpact.Value;
            pendingImpact = null;
            ApplyAttack(attack);
        }

        void ApplyAttack(AttackType attack)
        {
            Vector2 center = shape.bounds.center;
            switch (attack)
            {
                case AttackType.Punch:
                    PlaySound(punchSound, true);
                    center += Vector2.right * facing * 1.05f;
                    Hit(center, 1.15f, Damage, new HashSet<KnightController>());
                    StartCoroutine(Effect("AttackA_FX", new Vector2(facing * 0.5f, 0)));
                    break;
                case AttackType.Slam:
                    PlaySound(slamSound, true);
                    Hit(center, 2.5f, Damage, new HashSet<KnightController>());
                    StartCoroutine(Effect("AttackB_FX", new Vector2(0, -0.3f)));
                    break;
                case AttackType.Shockwave:
                    PlaySound(waveSound, true);
                    StartCoroutine(Wave(facing, Damage));
                    break;
            }
        }

        IEnumerator Wave(float direction, int damage)
        {
            var wave = new GameObject("Golem stone wave");
            waveObject = wave;
            var waveRenderer = wave.AddComponent<SpriteRenderer>();
            waveRenderer.sharedMaterial = visual.sharedMaterial;
            waveRenderer.sortingOrder = fx.sortingOrder;
            waveRenderer.flipX = direction < 0;
            wave.transform.localScale = transform.lossyScale;
            var waveAnimator = wave.AddComponent<Animator>();
            waveAnimator.runtimeAnimatorController = fxController;
            waveAnimator.SetTrigger("AttackC_FX");
            Vector2 position = shape.bounds.center;
            var victims = new HashSet<KnightController>();
            float elapsed = 0;
            while (elapsed < .9f && !isDead)
            {
                float deltaTime = Time.deltaTime;
                Vector2 next = position + Vector2.right * direction * 5 * deltaTime;
                if (Physics2D.Linecast(position, next, groundMask))
                    break;
                position = next;
                wave.transform.position = position;
                Hit(position, .65f, damage, victims);
                elapsed += deltaTime;
                yield return null;
            }

            Destroy(wave);
        }

        IEnumerator Upgrade()
        {
            busy = true;
            upgrading = true;
            Stop();
            PlaySound(upgradeSound, true);
            bossAnimator.SetBool("Enraged", true);
            bossAnimator.SetTrigger("Upgrade");
            yield return WaitForStateExit(bossAnimator, "Upgrade");
            enraged = true;
            upgrading = false;
            busy = false;
        }

        public override void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            bool lethal = CurrentHealth == 0;
            isDead = lethal;
            Damaged?.Invoke(this);
            if (lethal)
            {
                Died?.Invoke(this);
                pendingImpact = null;
                StopAllCoroutines();
                if (waveObject)
                    Destroy(waveObject);
                fx.sprite = null;
                StartCoroutine(Death());
                return;
            }

            if (Time.time >= nextHurtSound)
            {
                nextHurtSound = Time.time + .18f;
                PlaySound(hurtSound);
            }

            // Attacks and transformation have poise: damage still applies, animation is not interrupted.
            if (!busy && Time.time >= nextStaggerTime)
            {
                nextStaggerTime = Time.time + 1.5f;
                StartCoroutine(Hurt());
            }
        }

        IEnumerator Hurt()
        {
            busy = true;
            Stop();
            string state = enraged ? "HitB" : "HitA";
            bossAnimator.SetTrigger(state);
            yield return WaitForStateExit(bossAnimator, state);
            busy = false;
        }

        IEnumerator Death()
        {
            Stop();
            PlaySound(deathSound, true);
            shape.enabled = false;
            body.simulated = false;
            fxAnimator.ResetTrigger("AttackA_FX");
            fxAnimator.ResetTrigger("AttackB_FX");
            StartCoroutine(Effect("Death_FX", Vector2.zero));
            foreach (var parameter in bossAnimator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                    bossAnimator.ResetTrigger(parameter.nameHash);
            string state = enraged || upgrading ? "DeathB" : "DeathA";
            bossAnimator.SetTrigger(state);
            yield return WaitForStateExit(bossAnimator, state, true);
            yield return new WaitForSeconds(2);
            Destroy(gameObject);
        }

        void OnGUI()
        {
            if (!target || isDead || Vector2.Distance(target.transform.position, transform.position) > DetectionRange + 3)
                return;
            float width = Mathf.Min(500, Screen.width * .55f);
            Rect bar = new Rect((Screen.width - width) / 2, 24, width, 24);
            GUI.Box(bar, enraged ? BossTitle + " - ENRAGED" : BossTitle);
            var old = GUI.color;
            GUI.color = enraged ? Color.red : Color.cyan;
            GUI.DrawTexture(new Rect(bar.x, bar.y + 26, width * CurrentHP / MaxHealth, 8), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
