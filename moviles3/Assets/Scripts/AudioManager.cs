using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Referencias")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips Globales")]
    public AudioClip uiClickSound;

    private const string PREF_MUSIC = "MusicVolume";
    private const string PREF_SFX = "SFXVolume";
    private const float DEFAULT_MUSIC = 0.5f;
    private const float DEFAULT_SFX = 1.0f;

    private void Awake()
    {
        // 1. Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 2. CARGAMOS AQUÍ, LO PRIMERO DE TODO
            CargarVolumenInicial();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CargarVolumenInicial()
    {
        // Leemos los datos
        float savedMusic = PlayerPrefs.GetFloat(PREF_MUSIC, DEFAULT_MUSIC);
        float savedSFX = PlayerPrefs.GetFloat(PREF_SFX, DEFAULT_SFX);

        // Aplicamos el volumen
        if (musicSource != null) musicSource.volume = savedMusic;
        if (sfxSource != null) sfxSource.volume = savedSFX;

        // 3. TRUCO FINAL: Iniciamos la música AHORA, que ya sabemos que el volumen es correcto
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void SetMusicVolume(float volume) { if (musicSource) musicSource.volume = volume; }
    public void SetSFXVolume(float volume) { if (sfxSource) sfxSource.volume = volume; }

    public void PlayUIClick()
    {
        if (uiClickSound != null && sfxSource != null) sfxSource.PlayOneShot(uiClickSound);
    }

    public void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip);
            sfxSource.pitch = 1f;
        }
    }
}