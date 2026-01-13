using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Referencias (Arrastra los AudioSources)")]
    public AudioSource musicSource; // Para la música de fondo (Loop)
    public AudioSource sfxSource;   // Para efectos (UI, saltos, disparos)

    [Header("Sonidos Globales")]
    public AudioClip uiClickSound;  // El sonido del botón

    private void Awake()
    {
        // Patrón Singleton para que solo haya UN AudioManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ¡Esto hace que sobreviva al cambiar de escena!
        }
        else
        {
            Destroy(gameObject); // Si ya existe uno (ej. al volver al menú), destruye el nuevo
        }
    }

    // --- MÉTODOS DE VOLUMEN (Llamados por OptionsManager) ---

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        // Cambiamos el volumen del canal de efectos
        if (sfxSource != null) sfxSource.volume = volume;
    }

    // --- MÉTODOS PARA REPRODUCIR ---

    public void PlayUIClick()
    {
        if (uiClickSound != null && sfxSource != null)
        {
            // PlayOneShot permite superponer sonidos sin cortar el anterior
            sfxSource.PlayOneShot(uiClickSound);
        }
    }

    public void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip);
            sfxSource.pitch = 1f; // Resetear pitch después
        }
    }
}