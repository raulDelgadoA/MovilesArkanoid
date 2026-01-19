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
    public int totalLevels = 20;

    [Header("Standard Colors")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    public Color completedColor = Color.green;

    [Header("Boss Colors")]
    public Color bossLevelColor = new Color(1f, 0.4f, 0.4f);      // Rojo claro (Peligro)
    public Color bossCompletedColor = new Color(0.7f, 0f, 0f);    // Rojo oscuro (Boss Muerto)

    private List<GameObject> levelButtons = new List<GameObject>();

    void Start()
    {
        CreateLevelButtons();

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);
    }

    void CreateLevelButtons()
    {
        foreach (Transform child in levelGrid)
            Destroy(child.gameObject);

        levelButtons.Clear();

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelGrid);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = i.ToString();

            bool isUnlocked = IsLevelUnlocked(i);
            bool isCompleted = IsLevelCompleted(i);
            bool isBossLevel = (i % 5 == 0); // ¿Es múltiplo de 5?

            button.interactable = isUnlocked;

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (isCompleted)
                {
                    // --- CAMBIO AQUÍ ---
                    if (isBossLevel)
                    {
                        // Si es Boss y ya lo mataste -> Color especial de Boss Completado
                        buttonImage.color = bossCompletedColor;
                    }
                    else
                    {
                        // Nivel normal completado -> Verde
                        buttonImage.color = completedColor;
                    }
                }
                else if (isUnlocked)
                {
                    // Está disponible para jugar
                    if (isBossLevel)
                    {
                        // Es Boss -> Color de Peligro
                        buttonImage.color = bossLevelColor;
                    }
                    else
                    {
                        // Nivel normal -> Blanco
                        buttonImage.color = unlockedColor;
                    }
                }
                else
                {
                    // Bloqueado -> Gris
                    buttonImage.color = lockedColor;
                }
            }

            int levelIndex = i;
            button.onClick.AddListener(() => OnLevelSelected(levelIndex));

            Transform lockIcon = buttonObj.transform.Find("LockIcon");
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!isUnlocked);

            levelButtons.Add(buttonObj);
        }
    }

    bool IsLevelUnlocked(int level)
    {
        if (level == 1) return true;
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

    void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}