using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int score = 0;
    public int lives = 3;
    public int currentLevel = 1;
    public bool isGameOver = false;

    [Header("Level Settings")]
    public int scorePerBrick = 100;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public GameObject winPanel;
    public TextMeshProUGUI finalScoreText;

    [Header("Game Objects")]
    public GameObject ballPrefab;
    public Transform paddle;
    public ProceduralLevelGenerator proceduralGenerator;

    public GameObject safetyBarrier;

    private GameObject currentBall;
    private int bricksRemaining;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // --- CÓDIGO NUEVO PARA MÓVIL ---
        // 1. Desbloquear FPS (Para que vaya a 60 o 120fps fluido)
        Application.targetFrameRate = 60;

        // 2. Evitar que la pantalla se apague si no tocas nada
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // -------------------------------

        currentLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        UpdateUI();
        Input.gyro.enabled = true; // Fuerza el encendido del giroscopio
        InitializeLevel();
    }

    void Update()
    {
        // TRUCO DE DEBUG: Si pulsas 'R', reinicia el nivel al instante
        // Solo funcionará en el editor de Unity, no en el móvil final
        if (Application.isEditor && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Regenerando nivel...");
            // Borramos bolas viejas
            if (currentBall != null) Destroy(currentBall);

            // Volvemos a inicializar todo (esto leerá tus casillas marcadas nuevas)
            InitializeLevel();
        }
    }

    void InitializeLevel()
    {
        isGameOver = false;

        // 1. Aseguramos que la barrera esté apagada
        if (safetyBarrier != null) safetyBarrier.SetActive(false);

        // 2. Generamos el nivel (Con DestroyImmediate ya no habrá fantasmas)
        if (proceduralGenerator != null)
        {
            proceduralGenerator.GenerateLevel(currentLevel);
        }

        // 3. Ahora sí contamos. Como usamos DestroyImmediate, la cuenta será exacta.
        bricksRemaining = GameObject.FindGameObjectsWithTag("Brick").Length;

        Debug.Log($"Nivel {currentLevel} iniciado. Ladrillos reales: {bricksRemaining}");

        SpawnBall();
    }

    public void SpawnBall()
    {
        if (isGameOver) return;
        if (currentBall != null) Destroy(currentBall);

        Vector3 spawnPos = new Vector3(paddle.position.x, paddle.position.y, paddle.position.z + 0.8f);
        currentBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

        BallController ballScript = currentBall.GetComponent<BallController>();
        if (ballScript != null)
        {
            ballScript.gameManager = this;
            ballScript.paddle = paddle;
        }
    }

    // --- FUNCIÓN PARA BOLA EXTRA ---
    public void SpawnExtraBall(Vector3 position)
    {
        GameObject extraBall = Instantiate(ballPrefab, position, Quaternion.identity);
        BallController ballScript = extraBall.GetComponent<BallController>();
        if (ballScript != null)
        {
            ballScript.gameManager = this;
            ballScript.paddle = paddle;
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, -0.2f)).normalized;
            ballScript.LaunchImmediate(randomDir);
        }
    }

    public void OnBallFell(GameObject ballObj)
    {
        // 1. Destruimos la bola que ha tocado el suelo
        Destroy(ballObj);

        // 2. Contamos cuántas bolas hay AHORA MISMO en la escena.
        // OJO: Como Destroy no es inmediato (ocurre al final del frame), 
        // Unity todavía encontrará la bola que acabamos de mandar destruir.
        // Por eso, si contamos 1 bola, significa que es la que se está muriendo -> Perder Vida.
        // Si contamos 2 o más, significa que quedan otras vivas -> Seguimos jugando.

        int ballsActive = GameObject.FindGameObjectsWithTag("Ball").Length;

        if (ballsActive <= 1)
        {
            // Era la última bola
            LoseLife();
        }
        else
        {
            Debug.Log($"Una bola cayó, pero quedan {ballsActive - 1} en juego.");
        }
    }

    public void LoseLife()
    {
        lives--;
        UpdateUI();

        if (lives <= 0)
        {
            GameOver();
        }
        else
        {
            // Si perdemos vida, volvemos a sacar una bola nueva desde la raqueta
            SpawnBall();
        }
    }

    // --- FUNCIÓN DE LA BARRERA ---
    public void ActivateSafetyNet(float duration)
    {
        // DEBUG: Comprobar si tenemos la barrera asignada
        if (safetyBarrier == null)
        {
            Debug.LogError("¡ERROR! El GameManager intenta activar la barrera, pero la variable 'Safety Barrier' está vacía (None). Arrastra el cubo al Inspector.");
            return;
        }

        safetyBarrier.SetActive(true);
        Debug.Log($"Barrera ACTIVADA por {duration} segundos.");

        CancelInvoke("DisableSafetyNet");
        Invoke("DisableSafetyNet", duration);
    }

    void DisableSafetyNet()
    {
        if (safetyBarrier != null)
        {
            safetyBarrier.SetActive(false);
            Debug.Log("Barrera desactivada.");
        }
    }


    public void BrickDestroyed(Vector3 brickPos)
    {
        AddScore(scorePerBrick);

        // Pasamos la posición y la puntuación al ComboManager
        if (ComboEffectManager.Instance != null)
        {
            ComboEffectManager.Instance.RegisterHit(brickPos, scorePerBrick);
        }

        bricksRemaining--;
        if (bricksRemaining <= 0) LevelCompleted();
    }

    void LevelCompleted()
    {
        isGameOver = true;
        Debug.Log("¡NIVEL COMPLETADO!");

        if (currentBall != null)
        {
            BallController ballScript = currentBall.GetComponent<BallController>();
            if (ballScript != null) ballScript.StopBall();
        }

        // Guardamos progreso
        PlayerPrefs.SetInt($"Level_{currentLevel}_Completed", 1);
        PlayerPrefs.Save();

        if (winPanel != null)
        {
            winPanel.SetActive(true);

            if (finalScoreText != null)
                finalScoreText.text = $"Score: {score}";
        }
    }

    // Método para cargar el siguiente nivel infinito
    public void LoadNextLevel()
    {
        // Subimos el nivel
        int nextLevel = currentLevel + 1;
        PlayerPrefs.SetInt("SelectedLevel", nextLevel);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
        if (livesText != null) livesText.text = $"LIVES: {lives}";
    }

    void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = $"Score: {score}";
        }
        if (currentBall != null) Destroy(currentBall);
    }

    public void OnRestartButtonClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMenuButtonClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnPauseButtonClick()
    {
        if (isGameOver) return;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void OnResumeButtonClick()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void OnSelectButtonClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelectorScene");
    }
}