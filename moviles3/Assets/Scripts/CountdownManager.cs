using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI countdownText;
    public GameObject countdownPanel;
    public GameObject gameUI;

    [Header("Audio")]
    public AudioClip countdownSound;
    public AudioClip goSound;

    private PaddleController paddleRef;
    private BallController ballRef;

    // Guardamos el tamaño original aquí
    private Vector3 originalScale;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // --- CORRECCIÓN ---
        // Guardamos la escala AQUÍ (Awake) en lugar de en Start.
        // Así aseguramos que el valor existe antes de que el GameManager lo pida.
        if (countdownText != null)
        {
            originalScale = countdownText.transform.localScale;

            // Si por algún error es cero (muy raro), forzamos a 1
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
        }
    }

    public void StartGameCountdown(PaddleController paddle, BallController ball)
    {
        this.paddleRef = paddle;
        this.ballRef = ball;
        StartCoroutine(RoutineCountdown());
    }

    IEnumerator RoutineCountdown()
    {
        // 1. BLOQUEAR CONTROLES
        if (paddleRef != null) paddleRef.enabled = false;
        if (ballRef != null) ballRef.enabled = false;

        // 2. PREPARAR UI
        if (gameUI != null) gameUI.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(true);

        // Restaurar escala original por si acaso
        if (countdownText != null) countdownText.transform.localScale = originalScale;

        // 3. CUENTA ATRÁS (3, 2, 1)
        int count = 3;
        while (count > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = count.ToString();
                // Animación de latido usando la escala original correcta
                StartCoroutine(AnimateHeartbeat(countdownText.transform));
            }

            if (AudioManager.Instance != null && countdownSound != null)
                AudioManager.Instance.PlaySFX(countdownSound);

            yield return new WaitForSeconds(1f);
            count--;
        }

        // 4. ¡GO!
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            StartCoroutine(AnimateHeartbeat(countdownText.transform));
        }

        if (AudioManager.Instance != null && goSound != null)
            AudioManager.Instance.PlaySFX(goSound);

        yield return new WaitForSeconds(0.5f);

        // 5. DESBLOQUEAR TODO Y LANZAR
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        if (paddleRef != null) paddleRef.enabled = true;

        if (ballRef != null)
        {
            ballRef.enabled = true;
            ballRef.LaunchBall();
        }
    }

    IEnumerator AnimateHeartbeat(Transform target)
    {
        float timer = 0f;
        float duration = 0.25f;

        // Calculamos el tamaño máximo (un 20% más grande que el original)
        Vector3 targetBigScale = originalScale * 1.2f;

        // Fase 1: Crecer
        while (timer < duration)
        {
            timer += Time.deltaTime;
            target.localScale = Vector3.Lerp(originalScale, targetBigScale, timer / duration);
            yield return null;
        }

        // Fase 2: Encoger (Volver al original)
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            target.localScale = Vector3.Lerp(targetBigScale, originalScale, timer / duration);
            yield return null;
        }

        // Aseguramos que queda clavado en el tamaño original al terminar
        target.localScale = originalScale;
    }
}