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
    public AudioClip countdownSound; // Arrastra aquí el sonido "3, 2, 1"
    public AudioClip goSound;        // Arrastra aquí el sonido "GO!"

    [Header("Game References")]
    public GameObject ball;
    public BallController ballController;
    public PaddleController paddleController;

    // Ya no necesitamos audioSource ni gameManager aquí para el sonido

    void Start()
    {
        // Iniciar countdown
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        // Ocultar UI de juego
        if (gameUI != null) gameUI.SetActive(false);

        // Desactivar controles
        if (paddleController != null) paddleController.enabled = false;
        if (ballController != null) ballController.enabled = false;

        // Activar panel de countdown
        if (countdownPanel != null) countdownPanel.SetActive(true);

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

            // --- CAMBIO: Usar AudioManager ---
            if (AudioManager.Instance != null && countdownSound != null)
            {
                AudioManager.Instance.PlaySFX(countdownSound);
            }

            yield return new WaitForSeconds(1f);
        }

        // "GO!"
        if (countdownText != null)
        {
            countdownText.text = "¡GO!";
            countdownText.fontSize = 120;
            StartCoroutine(ScaleText(countdownText.transform, 1.8f, 0.3f));
        }

        // --- CAMBIO: Usar AudioManager para el GO ---
        if (AudioManager.Instance != null && goSound != null)
        {
            AudioManager.Instance.PlaySFX(goSound);
        }

        yield return new WaitForSeconds(0.5f);

        // Desactivar panel de countdown
        if (countdownPanel != null) countdownPanel.SetActive(false);

        // Activar UI de juego
        if (gameUI != null) gameUI.SetActive(true);

        // Activar controles
        if (paddleController != null) paddleController.enabled = true;
        if (ballController != null) ballController.enabled = true;

        // Lanzar la bola
        LaunchBall();
    }

    void LaunchBall()
    {
        if (ball != null)
        {
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>(); // Ojo: Si es 2D usa Rigidbody2D, si es 3D Rigidbody
            if (rb != null)
            {
                // Dirección aleatoria hacia arriba
                Vector2 direction = new Vector2(
                    Random.Range(-0.5f, 0.5f),
                    1f
                ).normalized;

                // Nota: linearVelocity es de Unity 6/Preview, si usas versiones anteriores usa .velocity
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

    public void RestartCountdown()
    {
        StartCoroutine(StartCountdown());
    }
}