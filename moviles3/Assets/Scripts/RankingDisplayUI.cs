using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RankingDisplayUI : MonoBehaviour
{
    public GameObject rankingPanel; // El panel que se abre
    public TextMeshProUGUI titleText; // "NIVEL 1 - TOP 5"

    // 5 Textos para los nombres y 5 para los puntos
    // Arrastralos en orden en el inspector
    public TextMeshProUGUI[] nameTexts;
    public TextMeshProUGUI[] scoreTexts;

    public void ShowRankingForLevel(int levelID)
    {
        rankingPanel.SetActive(true);
        titleText.text = $"TOP 5 - NIVEL {levelID}";

        // Obtenemos datos
        List<RankingManager.ScoreEntry> scores = RankingManager.Instance.GetHighScores(levelID);

        // Rellenamos los textos
        for (int i = 0; i < 5; i++)
        {
            if (i < scores.Count)
            {
                // Si hay dato
                nameTexts[i].text = $"{i + 1}. {scores[i].name}";
                scoreTexts[i].text = scores[i].score.ToString("N0"); // Formato número (10,000)
            }
            else
            {
                // Si está vacío
                nameTexts[i].text = $"{i + 1}. -----";
                scoreTexts[i].text = "0";
            }
        }
    }

    public void ClosePanel()
    {
        rankingPanel.SetActive(false);
    }
}