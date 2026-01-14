using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance;

    [Header("UI References")]
    public Slider soundSlider;
    public Slider musicSlider;

    // CAMBIO: Ahora es un Toggle, no un Slider
    public Toggle gyroscopeToggle;
    public Toggle vibrationToggle;

    [Header("Buttons")]
    public Button applyButton;
    public Button backButton;
    public Button resetButton;

    [Header("Default Values")]
    public float defaultSFX = 1.0f;
    public float defaultMusic = 0.5f;
    public bool defaultGyro = true;
    public bool defaultVib = true;

    // Variables públicas para el juego
    public bool GyroscopeEnabled { get; private set; }
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

        // CAMBIO: El listener ahora recibe un bool (v), no un float
        if (gyroscopeToggle) gyroscopeToggle.onValueChanged.AddListener((v) => GyroscopeEnabled = v);

        if (vibrationToggle) vibrationToggle.onValueChanged.AddListener((v) => VibrationEnabled = v);
    }

    void LoadSettings()
    {
        // Cargar valores
        float sfx = PlayerPrefs.GetFloat("SFXVolume", defaultSFX);
        float music = PlayerPrefs.GetFloat("MusicVolume", defaultMusic);
        GyroscopeEnabled = PlayerPrefs.GetInt("Gyroscope", defaultGyro ? 1 : 0) == 1;
        VibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", defaultVib ? 1 : 0) == 1;

        // Actualizar UI
        if (soundSlider) soundSlider.value = sfx;
        if (musicSlider) musicSlider.value = music;

        // CAMBIO: Usamos .isOn en lugar de .value
        if (gyroscopeToggle) gyroscopeToggle.isOn = GyroscopeEnabled;

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
        if (soundSlider) PlayerPrefs.SetFloat("SFXVolume", soundSlider.value);
        if (musicSlider) PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);

        // CAMBIO: Ahora podemos usar .isOn correctamente
        if (gyroscopeToggle) PlayerPrefs.SetInt("Gyroscope", gyroscopeToggle.isOn ? 1 : 0);

        if (vibrationToggle) PlayerPrefs.SetInt("VibrationEnabled", vibrationToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("Guardado");
    }

    void OnResetButtonClick()
    {
        if (soundSlider) soundSlider.value = defaultSFX;
        if (musicSlider) musicSlider.value = defaultMusic;

        // CAMBIO: Restablecer usando .isOn
        if (gyroscopeToggle) gyroscopeToggle.isOn = defaultGyro;

        if (vibrationToggle) vibrationToggle.isOn = defaultVib;

        SaveSettings();
    }
}