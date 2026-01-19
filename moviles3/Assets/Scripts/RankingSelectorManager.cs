using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RankingSelectorManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainPanel;        // <--- NUEVO: Arrastra aquí el objeto "ButtonPanel"
    public Transform levelGrid;
    public Button backButton;
    public GameObject levelButtonPrefab;

    [Header("Pop-Up Reference")]
    public RankingDisplayUI rankingDisplay;

    [Header("Level Settings")]
    public int totalLevels = 15;
    public Color hasRankingColor = Color.green;
    public Color noRankingColor = Color.red;

    private List<GameObject> levelButtons = new List<GameObject>();

    void Start()
    {
        CreateRankingButtons();
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);
    }

    void CreateRankingButtons()
    {
        // (Este código no cambia, lo oculto para resumir)
        foreach (Transform child in levelGrid) Destroy(child.gameObject);
        levelButtons.Clear();

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelGrid);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (buttonText != null) buttonText.text = i.ToString();

            bool hasData = CheckIfRankingExists(i);

            if (buttonImage != null)
                buttonImage.color = hasData ? hasRankingColor : noRankingColor;

            button.interactable = hasData;

            Transform lockIcon = buttonObj.transform.Find("LockIcon");
            if (lockIcon != null) lockIcon.gameObject.SetActive(false);

            int levelIndex = i;
            button.onClick.AddListener(() => OnLevelRankingSelected(levelIndex));

            levelButtons.Add(buttonObj);
        }
    }

    bool CheckIfRankingExists(int levelID)
    {
        if (RankingManager.Instance == null) return false;
        var scores = RankingManager.Instance.GetHighScores(levelID);
        return scores != null && scores.Count > 0;
    }

    void OnLevelRankingSelected(int level)
    {
        if (rankingDisplay != null)
        {
            // 1. Ocultamos el panel de botones actual
            if (mainPanel != null) mainPanel.SetActive(false); // <--- NUEVO

            // 2. Le decimos al RankingDisplay que active su panel
            // Y le pasamos la referencia de qué panel debe encender al volver
            rankingDisplay.ShowRankingForLevel(level, mainPanel); // <--- CAMBIO AQUÍ
        }
        else
        {
            Debug.LogError("¡Falta asignar el RankingDisplayUI en el inspector!");
        }
    }

    void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}