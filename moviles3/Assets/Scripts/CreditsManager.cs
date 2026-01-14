using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CreditsManager : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton;

    void Start()
    {
        // Configurar botón de volver
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);
    }

 
    void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}