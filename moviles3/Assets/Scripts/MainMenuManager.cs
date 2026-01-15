using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button exitButton;

    [Header("Sound")]
    public AudioClip buttonClickSound;
    private AudioSource audioSource;

    void Start()
    {
        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Configurar botones
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClick);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsButtonClick);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsButtonClick);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClick);

        // Configurar UI responsive
        SetupResponsiveUI();
    }

    void SetupResponsiveUI()
    {
        // Si el título es demasiado grande en pantallas pequeñas
        if (titleText != null)
        {
            if (Screen.width < 1080)
                titleText.fontSize = 80;
            else if (Screen.width < 720)
                titleText.fontSize = 60;
        }
    }

    void OnPlayButtonClick()
    {
        PlayButtonSound();
        SceneManager.LoadScene("LevelSelectorScene");
    }

    void OnOptionsButtonClick()
    {
        PlayButtonSound();
        SceneManager.LoadScene("OptionsScene");
    }

    void OnCreditsButtonClick()
    {
        PlayButtonSound();
        SceneManager.LoadScene("CreditsScene");
    }

    void OnExitButtonClick()
     {
         PlayButtonSound();
         Application.Quit();
     }

    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}