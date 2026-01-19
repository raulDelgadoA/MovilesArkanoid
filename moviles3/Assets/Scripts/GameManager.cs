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
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI winScoreText;

    // RECUPERADO: Variable para el texto de sacudidas
    public TextMeshProUGUI shakeCounterText;

    public NameInputUI nameInputPanel;

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
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        currentLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        UpdateUI();
        Input.gyro.enabled = true;
        InitializeLevel();
    }

    void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Regenerando nivel...");
            if (currentBall != null) Destroy(currentBall);
            InitializeLevel();
        }
    }

    void InitializeLevel()
    {
        isGameOver = false;

        if (safetyBarrier != null) safetyBarrier.SetActive(false);

        // --- LÓGICA DE BOSS ---
        if (proceduralGenerator != null)
        {
            // Si el nivel es múltiplo de 5 (5, 10, 15...), toca JEFE
            if (currentLevel % 5 == 0)
            {
                proceduralGenerator.SpawnBossLevel();

                // IMPORTANTE: Para ganar al boss, hay que matarlo.
                // Como el Boss es 1 solo objeto, ponemos bricksRemaining a 1.
                // Cuando el Boss muera, llamará a BrickDestroyed y ganaremos.
                bricksRemaining = 1;
            }
            else
            {
                // Nivel normal
                proceduralGenerator.GenerateLevel(currentLevel);
                bricksRemaining = GameObject.FindGameObjectsWithTag("Brick").Length;
            }
        }
        // ---------------------

        Debug.Log($"Nivel {currentLevel} iniciado. Ladrillos reales: {bricksRemaining}");

        SpawnBallWithCountdown();
    }

    // --- SISTEMA DE SPAWN ---

    // Función base para crear bola (para no repetir código)
    private BallController CreateBall()
    {
        if (currentBall != null) Destroy(currentBall);

        Vector3 spawnPos = new Vector3(paddle.position.x, paddle.position.y, paddle.position.z + 0.8f);
        currentBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

        BallController ballScript = currentBall.GetComponent<BallController>();
        if (ballScript != null)
        {
            ballScript.gameManager = this;
            ballScript.paddle = paddle;
        }
        return ballScript;
    }

    // OPCIÓN A: Start Level (Con Countdown y Autolaunch)
    public void SpawnBallWithCountdown()
    {
        if (isGameOver) return;

        BallController ballScript = CreateBall();
        PaddleController paddleScript = paddle.GetComponent<PaddleController>();

        // Llamamos al CountdownManager
        if (CountdownManager.Instance != null)
        {
            CountdownManager.Instance.StartGameCountdown(paddleScript, ballScript);
        }
    }

    // OPCIÓN B: Perder Vida (Sin Countdown, Manual Launch)
    public void SpawnBallImmediate()
    {
        if (isGameOver) return;

        CreateBall();
        // NO llamamos al countdown. La bola aparece en la pala y espera tu clic.
    }

    // --- GESTIÓN DE VIDAS ---

    public void OnBallFell(GameObject ballObj)
    {
        Destroy(ballObj);
        int ballsActive = GameObject.FindGameObjectsWithTag("Ball").Length;

        // Si queda 1 (que es la que vamos a destruir), perdemos vida
        if (ballsActive <= 1)
        {
            LoseLife();
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
            // AL MORIR: Usamos Spawn Inmediato (tú la lanzas con clic)
            SpawnBallImmediate();
        }
    }

    // --- RESTO DE FUNCIONES (PowerUps, Score, UI...) ---

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

    public void ActivateSafetyNet(float duration)
    {
        if (safetyBarrier == null) return;
        safetyBarrier.SetActive(true);
        CancelInvoke("DisableSafetyNet");
        Invoke("DisableSafetyNet", duration);
    }

    void DisableSafetyNet()
    {
        if (safetyBarrier != null) safetyBarrier.SetActive(false);
    }

    public void BrickDestroyed(Vector3 brickPos)
    {
        int finalPoints = scorePerBrick;

        if (ComboEffectManager.Instance != null)
        {
            finalPoints = ComboEffectManager.Instance.CalculateScoreWithFever(scorePerBrick);
            ComboEffectManager.Instance.RegisterHit(brickPos, finalPoints);
        }

        AddScore(finalPoints);
        bricksRemaining--;
        if (bricksRemaining <= 0) LevelCompleted();
    }

    void LevelCompleted()
    {
        isGameOver = true;
        if (currentBall != null)
        {
            BallController ballScript = currentBall.GetComponent<BallController>();
            if (ballScript != null) ballScript.StopBall();
        }

        PlayerPrefs.SetInt($"Level_{currentLevel}_Completed", 1);
        PlayerPrefs.Save();

        if (RankingManager.Instance != null && RankingManager.Instance.IsNewRecord(currentLevel, score))
        {
            if (nameInputPanel != null)
            {
                nameInputPanel.Show(currentLevel, score, this);
                return;
            }
        }
        ShowWinPanel();
    }

    public void OnNameSubmitted()
    {
        ShowWinPanel();
    }

    void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winScoreText != null) winScoreText.text = $"Score: {score}";
        }
    }

    public void LoadNextLevel()
    {
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
        if (levelText != null) levelText.text = $"LEVEL: {currentLevel}";
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
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnResumeButtonClick()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnSelectButtonClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelectorScene");
    }
}