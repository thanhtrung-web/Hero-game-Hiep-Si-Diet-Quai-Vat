using UnityEngine;
using UnityEngine.Audio;

namespace HeroGame.Map3
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CaveEnemyAudio : MonoBehaviour
    {
        [Header("Enemy Sound Effects")]
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip[] attackVariants;
        [SerializeField] private AudioClip movementSound;
        [SerializeField] private AudioClip swoopSound;
        private int attackIndex;
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;
        [SerializeField] private AudioMixerGroup soundMixerGroup;

        public float DeathSoundDuration => deathSound ? deathSound.length : 0f;

        private AudioSource soundSource;
        private Rigidbody2D body;
        private CaveEnemy enemy;
        private bool deathPlayed;

        private void Awake()
        {
            enemy = GetComponent<CaveEnemy>();
            body = GetComponent<Rigidbody2D>();
            soundSource = GetComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.loop = false;
            soundSource.spatialBlend = 0f;
            soundSource.pitch = 1f;
            soundSource.volume = 1f;
            soundSource.outputAudioMixerGroup = soundMixerGroup;
        }

        // Called only by Animation Events on this GameObject's Animator.
        public void PlayAttackSound()
        {
            if (enemy && enemy.State != CaveEnemyState.Attack) return;
            AudioClip clip = attackIndex == 0 ? attackSound : attackVariants[attackIndex - 1];
            attackIndex = (attackIndex + 1) % (1 + (attackVariants?.Length ?? 0));
            PlaySound(clip);
        }

        public void PlayMovementSound()
        {
            if (!enemy || enemy.State != CaveEnemyState.Move || !body ||
                Mathf.Abs(body.linearVelocity.x) < 0.1f || soundSource.isPlaying)
                return;

            PlaySound(movementSound, 6f, 0.35f);
        }

        public void PlaySwoopSound()
        {
            if (enemy && enemy.State == CaveEnemyState.Attack)
                PlaySound(swoopSound);
        }

        public void PlayHurtSound()
        {
            if (enemy && enemy.State != CaveEnemyState.Hurt) return;
            PlaySound(hurtSound);
        }

        public void PlayDeathSound()
        {
            if (deathPlayed || (enemy && enemy.State != CaveEnemyState.Dead)) return;
            deathPlayed = true;
            PlaySound(deathSound);
        }

        private void PlaySound(AudioClip clip, float audibleRange = 16f, float gain = 1f)
        {
            if (!clip || !soundSource || !enemy || !enemy.Player) return;
            float distance = Vector2.Distance(transform.position, enemy.Player.position);
            float volume = soundVolume * gain * (1f - Mathf.InverseLerp(2f, audibleRange, distance));
            if (volume <= 0f) return;
            soundSource.Stop();
            soundSource.PlayOneShot(clip, volume);
        }
    }
}
