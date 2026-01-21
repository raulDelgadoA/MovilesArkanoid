using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameInputUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public Button confirmButton;

    private int pendingLevelID;
    private int pendingScore;
    private GameManager gm; // Referencia para avisar cuando acabemos

    void Start()
    {
        // Opcional: Validar que solo se active el botón si hay texto
        confirmButton.onClick.AddListener(SubmitName);
    }

    public void Show(int level, int score, GameManager gameManagerRef)
    {
        pendingLevelID = level;
        pendingScore = score;
        gm = gameManagerRef;

        nameInputField.text = ""; // Limpiar campo
        gameObject.SetActive(true); // Mostrar panel

        // Poner el foco para escribir directo
        nameInputField.Select();
        nameInputField.ActivateInputField();
    }

    void SubmitName()
    {
        string playerName = nameInputField.text.ToUpper(); // Forzamos mayúsculas tipo Arcade

        if (string.IsNullOrEmpty(playerName)) playerName = "?????"; // Por si acaso

        // 1. Guardamos el dato real
        RankingManager.Instance.AddScore(pendingLevelID, playerName, pendingScore);

        // 2. Cerramos este panel
        gameObject.SetActive(false);

        // 3. Avisamos al GameManager para que siga su curso (mostrar Win Panel normal)
        if (gm != null) gm.OnNameSubmitted();
    }
}