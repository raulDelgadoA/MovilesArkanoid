using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RankingSelectorManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform levelGrid;          // Dónde se ponen los botones
    public Button backButton;            // Botón para volver al menú
    public GameObject levelButtonPrefab; // El mismo prefab que usas en el selector de niveles

    [Header("Pop-Up Reference")]
    public RankingDisplayUI rankingDisplay; // <--- ARRASTRA AQUÍ EL PANEL DEL VISOR

    [Header("Level Settings")]
    public int totalLevels = 12;
    public Color hasRankingColor = Color.green; // Verde si hay datos
    public Color noRankingColor = Color.red;    // Rojo si está vacío

    private List<GameObject> levelButtons = new List<GameObject>();

    void Start()
    {
        // Crear botones de niveles
        CreateRankingButtons();

        // Configurar botón de volver
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);
    }

    void CreateRankingButtons()
    {
        // Limpiar grid por si acaso
        foreach (Transform child in levelGrid)
            Destroy(child.gameObject);

        levelButtons.Clear();

        // Crear botones para cada nivel
        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelGrid);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            // 1. Poner número
            if (buttonText != null)
                buttonText.text = i.ToString();

            // 2. Comprobar si existe ranking para este nivel
            bool hasData = CheckIfRankingExists(i);

            // 3. Configurar Color e Interactividad
            if (buttonImage != null)
            {
                buttonImage.color = hasData ? hasRankingColor : noRankingColor;
            }

            button.interactable = hasData; // Solo clicable si hay datos

            // 4. Ocultar candados (reutilizamos tu prefab, así que ocultamos el icono de candado si existe)
            Transform lockIcon = buttonObj.transform.Find("LockIcon");
            if (lockIcon != null) lockIcon.gameObject.SetActive(false); // En ranking no usamos candados visuales, usamos colores

            // 5. Asignar acción al clicar
            int levelIndex = i;
            button.onClick.AddListener(() => OnLevelRankingSelected(levelIndex));

            levelButtons.Add(buttonObj);
        }
    }

    // Función auxiliar para preguntar al RankingManager si hay lista
    bool CheckIfRankingExists(int levelID)
    {
        if (RankingManager.Instance == null) return false;

        // Obtenemos la lista. Si tiene más de 0 elementos, es que hay ranking.
        var scores = RankingManager.Instance.GetHighScores(levelID);
        return scores != null && scores.Count > 0;
    }

    void OnLevelRankingSelected(int level)
    {
        // En vez de cargar escena, abrimos el Pop-Up
        if (rankingDisplay != null)
        {
            rankingDisplay.ShowRankingForLevel(level);
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