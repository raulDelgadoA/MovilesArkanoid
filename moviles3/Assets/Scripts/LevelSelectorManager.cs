using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelSelectorManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform levelGrid;
    public Button backButton;
    public TextMeshProUGUI levelInfoText;
    public GameObject levelButtonPrefab;

    [Header("Level Settings")]
    public int totalLevels = 12;
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    public Color completedColor = Color.green;

    private List<GameObject> levelButtons = new List<GameObject>();

    void Start()
    {
        // Crear botones de niveles
        CreateLevelButtons();

        // Configurar botón de volver
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);

        // Configurar información inicial
        UpdateLevelInfo(1);
    }

    void CreateLevelButtons()
    {
        // Limpiar grid si ya hay botones
        foreach (Transform child in levelGrid)
            Destroy(child.gameObject);

        levelButtons.Clear();

        // Crear botones para cada nivel
        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelGrid);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = i.ToString();

            // Configurar según estado del nivel
            bool isUnlocked = IsLevelUnlocked(i);
            bool isCompleted = IsLevelCompleted(i);

            button.interactable = isUnlocked;

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (isCompleted)
                    buttonImage.color = completedColor;
                else if (isUnlocked)
                    buttonImage.color = unlockedColor;
                else
                    buttonImage.color = lockedColor;
            }

            // Asignar listener
            int levelIndex = i;
            button.onClick.AddListener(() => OnLevelSelected(levelIndex));

            // Mostrar candado si está bloqueado
            Transform lockIcon = buttonObj.transform.Find("LockIcon");
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!isUnlocked);

            levelButtons.Add(buttonObj);
        }
    }

    bool IsLevelUnlocked(int level)
    {
        // El nivel 1 siempre está desbloqueado
        if (level == 1) return true;

        // Los niveles se desbloquean completando el anterior
        return PlayerPrefs.GetInt($"Level_{level - 1}_Completed", 0) == 1;
    }

    bool IsLevelCompleted(int level)
    {
        return PlayerPrefs.GetInt($"Level_{level}_Completed", 0) == 1;
    }

    void OnLevelSelected(int level)
    {
        if (IsLevelUnlocked(level))
        {
            PlayerPrefs.SetInt("SelectedLevel", level);
            SceneManager.LoadScene("GameScene");
        }
    }

    void UpdateLevelInfo(int level)
    {
        if (levelInfoText != null)
        {
            int highScore = PlayerPrefs.GetInt($"Level_{level}_HighScore", 0);
            levelInfoText.text = $"Nivel {level}\nRecord: {highScore}";
        }
    }

    void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}