using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider soundSlider;
    public Slider musicSlider;
    public Slider sensitivitySlider;
    public Toggle vibrationToggle;
    public Button applyButton;
    public Button backButton;
    public Button resetButton;

    [Header("Default Values")]
    public float defaultSoundVolume = 0.8f;
    public float defaultMusicVolume = 0.6f;
    public float defaultSensitivity = 1.0f;
    public bool defaultVibration = true;

    void Start()
    {
        // Cargar valores guardados
        LoadSettings();

        // Configurar botones
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyButtonClick);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClick);

        // Actualizar UI cuando cambian los sliders
        if (soundSlider != null)
            soundSlider.onValueChanged.AddListener(OnSoundChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
    }

    void LoadSettings()
    {
        soundSlider.value = PlayerPrefs.GetFloat("SoundVolume", defaultSoundVolume);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", defaultSensitivity);
        vibrationToggle.isOn = PlayerPrefs.GetInt("VibrationEnabled", defaultVibration ? 1 : 0) == 1;
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat("SoundVolume", soundSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetInt("VibrationEnabled", vibrationToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
    }

    void OnApplyButtonClick()
    {
        SaveSettings();
        ApplySettingsImmediately();

        // Mostrar feedback visual (puedes añadir un texto de "Guardado!")
        Debug.Log("Configuración guardada");
    }

    void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    void OnResetButtonClick()
    {
        // Resetear a valores por defecto
        soundSlider.value = defaultSoundVolume;
        musicSlider.value = defaultMusicVolume;
        sensitivitySlider.value = defaultSensitivity;
        vibrationToggle.isOn = defaultVibration;

        SaveSettings();
        ApplySettingsImmediately();
    }

    void ApplySettingsImmediately()
    {
        // Aplicar volumen de sonido
        AudioListener.volume = soundSlider.value;

        // Notificar otros sistemas que la configuración cambió
        // (Por ejemplo, música de fondo)
    }

    void OnSoundChanged(float value)
    {
        // Cambiar volumen en tiempo real
        AudioListener.volume = value;
    }

    void OnMusicChanged(float value)
    {
        // Cambiar volumen de música en tiempo real
        // Buscar AudioSource de música y ajustar
        GameObject musicPlayer = GameObject.FindGameObjectWithTag("MusicPlayer");
        if (musicPlayer != null)
        {
            AudioSource musicSource = musicPlayer.GetComponent<AudioSource>();
            if (musicSource != null)
                musicSource.volume = value;
        }
    }
}