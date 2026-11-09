using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("UI SFX")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    private AudioSource source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    public void PlayHover()
    {
        Play(hoverClip);
    }

    public void PlayClick()
    {
        Play(clickClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            source.PlayOneShot(clip);
    }
}