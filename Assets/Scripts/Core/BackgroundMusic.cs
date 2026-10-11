using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;

    public static BackgroundMusic Instance => instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.25f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = Mathf.Clamp01(musicVolume);
    }

    private void Start()
    {
        if (musicSource.clip == null)
        {
            Debug.LogWarning("BackgroundMusic: Chưa gán file nhạc vào AudioSource Clip.", this);
            return;
        }

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
