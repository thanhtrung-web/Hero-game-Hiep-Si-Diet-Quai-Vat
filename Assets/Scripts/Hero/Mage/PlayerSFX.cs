using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSFX : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip attack1Clip;
    [SerializeField] private AudioClip attack2Clip;
    [SerializeField] private AudioClip magicClip;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            source.PlayOneShot(clip);
    }

    public void PlayHurt() => Play(hurtClip);
    public void PlayDeath() => Play(deathClip);
    public void PlayAttack1() => Play(attack1Clip);
    public void PlayAttack2() => Play(attack2Clip);
    public void PlayMagic() => Play(magicClip);
}