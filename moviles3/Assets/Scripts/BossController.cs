using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossController : MonoBehaviour
{
    [Header("Configuración Boss")]
    public int maxHealth = 20;
    private int currentHealth;

    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float moveRange = 2.5f; // Cuanto se mueve a los lados

    [Header("Ataque")]
    public GameObject projectilePrefab; // Arrastra aquí un prefab de una bolita roja o cubo
    public float attackRate = 2f; // Dispara cada 2 segundos

    [Header("Visuales - Barra de Vida")]
    public Slider healthSlider; // ARRASTRA AQUÍ TU SLIDER CREADO
    public float smoothSpeed = 5f; // Velocidad de la animación de la barra

    [Header("Visuales")]
    public TextMeshPro hpText; // Texto encima del boss con la vida (Opcional)
    private Renderer rend;
    private Color baseColor;

    private float startX;

    private float targetHealthValue;

    void Start()
    {
        currentHealth = maxHealth;

        targetHealthValue = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        startX = transform.position.x;
        rend = GetComponent<Renderer>();
        if (rend != null) baseColor = rend.material.color;

        UpdateUI();

        InvokeRepeating("ShootProjectile", 1f, attackRate);
    }

    void Update()
    {
        // Movimiento Senoidal (PingPong suave) de lado a lado
        float x = startX + Mathf.Sin(Time.time * moveSpeed) * moveRange;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        // 2. Animación Suave de la Barra de Vida (Juice Effect)
        if (healthSlider != null)
        {
            // Lerp mueve el valor actual hacia el objetivo suavemente
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealthValue, Time.deltaTime * smoothSpeed);
        }
    }

    public void TakeDamage()
    {
        currentHealth--;

        // Actualizamos el objetivo de la barra (el Update se encarga de moverlo suave)
        targetHealthValue = currentHealth;
        UpdateUI();

        // Feedback visual: Parpadeo rojo
        if (rend != null)
        {
            rend.material.color = Color.red;
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", Color.red * 2f);
            Invoke("ResetColor", 0.1f);
        }

        // Feedback de sonido y vibración
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClickSound); // Usa un sonido de golpe fuerte

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ResetColor()
    {
        if (rend != null)
        {
            rend.material.color = baseColor;
            rend.material.SetColor("_EmissionColor", baseColor * 0.5f);
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null)
        {
            // 1. Calculamos la posición X y Z basadas en el Boss
            float spawnX = transform.position.x;

            // Un poco hacia el jugador (Z - 1.5) para que salga "de la boca" o del frente
            float spawnZ = transform.position.z - 1.5f;

            // 2. --- CORRECCIÓN CLAVE ---
            // Ignoramos la altura del Boss y FORZAMOS la altura de juego.
            // Si tus ladrillos normales están en Y=0.5, pon 0.5f. Si están en 0, pon 0f.
            // Por defecto en tus scripts anteriores usabas 0.5f.
            float fixedHeight = 0.71f;

            Vector3 spawnPos = new Vector3(spawnX, fixedHeight, spawnZ);

            Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }
    }

    void Die()
    {
        CancelInvoke();
        // Avisar al GameManager de que el nivel (el boss) ha terminado
        // Usamos la misma función que si rompieras el último ladrillo
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BrickDestroyed(transform.position);
        }

        // Efectos de muerte (puedes añadir explosiones aquí)
        Destroy(gameObject);
    }

    void UpdateUI()
    {
        if (hpText != null) hpText.text = currentHealth.ToString();
    }
}