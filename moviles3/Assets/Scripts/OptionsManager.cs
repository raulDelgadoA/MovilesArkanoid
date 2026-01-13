using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance;

    [Header("UI References")]
    public Slider soundSlider;
    public Slider musicSlider;
    public Slider sensitivitySlider;
    public Toggle vibrationToggle;

    [Header("Buttons")]
    public Button applyButton;
    public Button backButton;
    public Button resetButton;

    [Header("Default Values")]
    public float defaultSFX = 1.0f;
    public float defaultMusic = 0.5f;
    public float defaultSens = 1.0f;
    public bool defaultVib = true;

    // Variables públicas para el juego
    public float CurrentSensitivity { get; private set; }
    public bool VibrationEnabled { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadSettings();

        // Listeners de UI
        if (applyButton) applyButton.onClick.AddListener(SaveSettings);
        if (backButton) backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
        if (resetButton) resetButton.onClick.AddListener(OnResetButtonClick);

        if (soundSlider) soundSlider.onValueChanged.AddListener((v) => {
            if (AudioManager.Instance) AudioManager.Instance.SetSFXVolume(v);
        });

        if (musicSlider) musicSlider.onValueChanged.AddListener((v) => {
            if (AudioManager.Instance) AudioManager.Instance.SetMusicVolume(v);
        });

        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener((v) => CurrentSensitivity = v);
        if (vibrationToggle) vibrationToggle.onValueChanged.AddListener((v) => VibrationEnabled = v);
    }

    void LoadSettings()
    {
        // Cargar valores
        float sfx = PlayerPrefs.GetFloat("SFXVolume", defaultSFX);
        float music = PlayerPrefs.GetFloat("MusicVolume", defaultMusic);
        CurrentSensitivity = PlayerPrefs.GetFloat("Sensitivity", defaultSens);
        VibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", defaultVib ? 1 : 0) == 1;

        // Actualizar UI
        if (soundSlider) soundSlider.value = sfx;
        if (musicSlider) musicSlider.value = music;
        if (sensitivitySlider) sensitivitySlider.value = CurrentSensitivity;
        if (vibrationToggle) vibrationToggle.isOn = VibrationEnabled;

        // Sincronizar Audio Manager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(sfx);
            AudioManager.Instance.SetMusicVolume(music);
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("SFXVolume", soundSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetInt("VibrationEnabled", vibrationToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Guardado");
    }

    void OnResetButtonClick()
    {
        soundSlider.value = defaultSFX;
        musicSlider.value = defaultMusic;
        sensitivitySlider.value = defaultSens;
        vibrationToggle.isOn = defaultVib;
        SaveSettings();
    }
}