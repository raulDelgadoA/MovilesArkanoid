using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CountdownManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI countdownText;
    public GameObject countdownPanel;
    public GameObject gameUI;

    [Header("Countdown Settings")]
    public float countdownDuration = 3f;
    public AudioClip countdownSound;
    public AudioClip goSound;

    [Header("Game References")]
    public GameObject ball;
    public BallController ballController;
    public PaddleController paddleController;

    private AudioSource audioSource;
    private GameManager gameManager;

    void Start()
    {
        // Obtener referencias
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        gameManager = FindObjectOfType<GameManager>();

        // Iniciar countdown
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        // Ocultar UI de juego
        if (gameUI != null)
            gameUI.SetActive(false);

        // Desactivar controles
        if (paddleController != null)
            paddleController.enabled = false;

        if (ballController != null)
            ballController.enabled = false;

        // Activar panel de countdown
        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        // Countdown: 3, 2, 1
        for (int i = (int)countdownDuration; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.fontSize = 150;

                // Animación simple
                StartCoroutine(ScaleText(countdownText.transform, 1.5f, 0.2f));
            }

            // Sonido
            if (audioSource != null && countdownSound != null)
                audioSource.PlayOneShot(countdownSound);

            yield return new WaitForSeconds(1f);
        }

        // "GO!"
        if (countdownText != null)
        {
            countdownText.text = "¡GO!";
            countdownText.fontSize = 120;
            StartCoroutine(ScaleText(countdownText.transform, 1.8f, 0.3f));
        }

        // Sonido GO
        if (audioSource != null && goSound != null)
            audioSource.PlayOneShot(goSound);

        yield return new WaitForSeconds(0.5f);

        // Desactivar panel de countdown
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // Activar UI de juego
        if (gameUI != null)
            gameUI.SetActive(true);

        // Activar controles
        if (paddleController != null)
            paddleController.enabled = true;

        if (ballController != null)
            ballController.enabled = true;

        // Lanzar la bola
        LaunchBall();
    }

    void LaunchBall()
    {
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Dirección aleatoria hacia arriba
                Vector3 direction = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    1f,
                    0f
                ).normalized;

                rb.linearVelocity = direction * 5f;
            }
        }
    }

    IEnumerator ScaleText(Transform textTransform, float targetScale, float duration)
    {
        Vector3 originalScale = textTransform.localScale;
        Vector3 target = originalScale * targetScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            textTransform.localScale = Vector3.Lerp(originalScale, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Volver a tamaño normal
        elapsed = 0f;
        while (elapsed < duration)
        {
            textTransform.localScale = Vector3.Lerp(target, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        textTransform.localScale = originalScale;
    }

    // Para reiniciar countdown (cuando pierdes una vida)
    public void RestartCountdown()
    {
        StartCoroutine(StartCountdown());
    }
}