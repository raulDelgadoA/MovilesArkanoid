using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RankingDisplayUI : MonoBehaviour
{
    public GameObject rankingPanel;
    public TextMeshProUGUI titleText;

    public TextMeshProUGUI[] nameTexts;
    public TextMeshProUGUI[] scoreTexts;

    // Variable privada para recordar qué panel hay que volver a encender
    private GameObject panelToRestore;

    // Modificamos la función para aceptar el panel anterior como parámetro
    public void ShowRankingForLevel(int levelID, GameObject panelComingFrom = null)
    {
        panelToRestore = panelComingFrom; // Guardamos la referencia

        rankingPanel.SetActive(true);
        titleText.text = $"LEVEL {levelID} - TOP 5";

        List<RankingManager.ScoreEntry> scores = RankingManager.Instance.GetHighScores(levelID);

        for (int i = 0; i < 5; i++)
        {
            if (i < scores.Count)
            {
                nameTexts[i].text = $"{i + 1}. {scores[i].name}";
                scoreTexts[i].text = scores[i].score.ToString("N0");
            }
            else
            {
                nameTexts[i].text = $"{i + 1}. -----";
                scoreTexts[i].text = "0";
            }
        }
    }

    public void ClosePanel()
    {
        rankingPanel.SetActive(false);

        // Si tenemos un panel guardado para restaurar, lo encendemos
        if (panelToRestore != null)
        {
            panelToRestore.SetActive(true);
        }
    }
}