using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para ordenar listas

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance;

    void Awake()
    {
        // Singleton para poder llamarlo desde cualquier escena
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Que sobreviva entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- CLASES DE DATOS (El molde para guardar) ---
    [System.Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
    }

    [System.Serializable]
    public class LevelHighScores
    {
        public List<ScoreEntry> entryList = new List<ScoreEntry>();
    }

    // --- FUNCIONES PRINCIPALES ---

    // 1. ¿Es esta puntuación digna del Top 5?
    public bool IsNewRecord(int levelID, int scoreToCheck)
    {
        LevelHighScores data = LoadScores(levelID);

        // Si hay menos de 5, entra seguro
        if (data.entryList.Count < 5) return true;

        // Si la lista está llena, miramos si superamos al PEOR (el último)
        // (Asumimos que la lista siempre está ordenada de mejor a peor)
        if (scoreToCheck > data.entryList[data.entryList.Count - 1].score)
        {
            return true;
        }

        return false;
    }

    // 2. Guardar la puntuación
    public void AddScore(int levelID, string userName, int score)
    {
        LevelHighScores data = LoadScores(levelID);

        // Creamos la nueva entrada
        ScoreEntry newEntry = new ScoreEntry { name = userName, score = score };
        data.entryList.Add(newEntry);

        // Ordenamos la lista (Mayor a menor)
        data.entryList = data.entryList.OrderByDescending(x => x.score).ToList();

        // Si nos pasamos de 5, borramos los sobrantes
        if (data.entryList.Count > 5)
        {
            // Nos quedamos solo con los 5 primeros
            data.entryList = data.entryList.GetRange(0, 5);
        }

        // Guardamos en disco
        SaveScores(levelID, data);
    }

    // 3. Obtener la lista para mostrarla
    public List<ScoreEntry> GetHighScores(int levelID)
    {
        return LoadScores(levelID).entryList;
    }

    // --- GUARDADO INTERNO (JSON + PlayerPrefs) ---

    private LevelHighScores LoadScores(int levelID)
    {
        string key = $"Ranking_Level_{levelID}"; // Ej: "Ranking_Level_1"
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<LevelHighScores>(json);
        }
        else
        {
            return new LevelHighScores(); // Devolvemos lista vacía
        }
    }

    private void SaveScores(int levelID, LevelHighScores data)
    {
        string key = $"Ranking_Level_{levelID}";
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }
}