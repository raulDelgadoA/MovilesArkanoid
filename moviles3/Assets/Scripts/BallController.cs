using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Ball Physics")]
    public float initialSpeed = 15f;
    public float maxSpeed = 25f;
    public float minVerticalSpeed = 2f;

    [Header("References")]
    public GameManager gameManager;
    public Transform paddle;
    public TrailRenderer trailEffect;

    [Header("Sound Effects")]
    public AudioClip bounceSound;   // Rebote pared
    public AudioClip paddleSound;   // Rebote pala
    public AudioClip brickSound;    // Rebote ladrillo (si no se rompe o genérico)
    public AudioClip powerUpSound;  // Al coger powerup

    private Rigidbody rb;
    // private AudioSource audioSource; // <--- BORRADO: Ya no lo necesitamos
    private Vector3 lastVelocity;
    private bool isLaunched = false;
    private float offsetZ;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // audioSource = GetComponent<AudioSource>(); // <--- BORRADO
    }

    void Start()
    {
        // --- BLOQUE DE SEGURIDAD ---
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        }

        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (trailEffect != null) trailEffect.enabled = false;

        if (paddle == null)
        {
            PaddleController paddleScript = FindObjectOfType<PaddleController>();
            if (paddleScript != null) paddle = paddleScript.transform;
        }

        if (paddle != null) offsetZ = transform.position.z - paddle.position.z;
    }

    void Update()
    {
        if (!isLaunched)
        {
            if (paddle != null)
            {
                Vector3 newPos = new Vector3(paddle.position.x, transform.position.y, paddle.position.z + offsetZ);
                transform.position = newPos;
            }

            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                LaunchBall();
            }
        }

        // Nota: rb.linearVelocity es para Unity 6. Si usas una versión anterior, cámbialo a rb.velocity
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f && !float.IsNaN(rb.linearVelocity.x))
            lastVelocity = rb.linearVelocity;
    }

    void FixedUpdate()
    {
        if (isLaunched && rb != null)
        {
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = currentVelocity.normalized * initialSpeed;
            PreventFlatAngles();
        }
    }

    public void LaunchBall()
    {
        if (!isLaunched && rb != null)
        {
            isLaunched = true;
            float xRandom = Random.Range(-0.5f, 0.5f);
            Vector3 launchDirection = new Vector3(xRandom, 0, 1).normalized;
            rb.linearVelocity = launchDirection * initialSpeed;
            if (trailEffect != null) trailEffect.enabled = true;
        }
    }

    public void LaunchImmediate(Vector3 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        isLaunched = true;
        rb.linearVelocity = direction.normalized * initialSpeed;
        if (trailEffect != null) trailEffect.enabled = true;
    }

    public void ApplySpeedModifier(float multiplier)
    {
        initialSpeed *= multiplier;
        initialSpeed = Mathf.Clamp(initialSpeed, 10f, 35f);
    }

    void PreventFlatAngles()
    {
        if (rb == null) return;
        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.1f) return;

        if (Mathf.Abs(v.z) < minVerticalSpeed)
        {
            float signZ = (v.z >= 0) ? 1f : -1f;
            Vector3 newDir = new Vector3(v.x, 0, signZ * minVerticalSpeed).normalized;
            rb.linearVelocity = newDir * initialSpeed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (rb == null) return;

        Vector3 normal = collision.contacts[0].normal;
        if (lastVelocity == Vector3.zero || float.IsNaN(lastVelocity.x)) lastVelocity = transform.forward * initialSpeed;

        Vector3 reflected = Vector3.Reflect(lastVelocity, normal);
        reflected.y = 0;
        rb.linearVelocity = reflected.normalized * initialSpeed;

        // Reproducir sonido usando el Manager
        PlayCollisionSound(collision.gameObject.tag);

        if (collision.gameObject.CompareTag("Paddle")) HandlePaddleCollision(collision);

        if (collision.gameObject.CompareTag("Brick"))
        {
            BrickController brick = collision.gameObject.GetComponent<BrickController>();

            if (brick != null && brick.currentPowerUp != PowerUpType.None)
            {
                ActivatePowerUp(brick.currentPowerUp, collision.transform.position);
            }

            if (gameManager != null)
            {
                gameManager.BrickDestroyed(collision.transform.position);
            }

            Destroy(collision.gameObject);
        }
    }

    void ActivatePowerUp(PowerUpType type, Vector3 position)
    {
        // --- CAMBIO: Usar AudioManager ---
        if (powerUpSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(powerUpSound);
        }

        switch (type)
        {
            case PowerUpType.ExtraBall:
                if (gameManager != null) gameManager.SpawnExtraBall(position);
                break;

            case PowerUpType.SpeedUp:
                ApplySpeedModifier(1.3f);
                break;

            case PowerUpType.SlowDown:
                ApplySpeedModifier(0.8f);
                break;

            case PowerUpType.SafetyNet:
                Debug.Log("Bola: ¡He tocado un ladrillo SafetyNet!");
                if (gameManager != null)
                    gameManager.ActivateSafetyNet(10f);
                break;
        }
    }

    public void StopBall()
    {
        isLaunched = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void HandlePaddleCollision(Collision collision)
    {
        float hitPoint = transform.position.x - collision.transform.position.x;
        float paddleWidth = collision.collider.bounds.size.x;
        if (paddleWidth <= 0) paddleWidth = 1f;
        float normalizedHit = Mathf.Clamp(hitPoint / (paddleWidth / 2), -0.9f, 0.9f);
        Vector3 newDir = new Vector3(normalizedHit, 0, 1).normalized;
        rb.linearVelocity = newDir * initialSpeed;
    }

    // --- CAMBIO COMPLETO EN ESTA FUNCIÓN ---
    void PlayCollisionSound(string tag)
    {
        if (AudioManager.Instance == null) return;

        switch (tag)
        {
            case "Paddle":
                if (paddleSound != null) AudioManager.Instance.PlaySFX(paddleSound);
                break;
            case "Brick":
                // Nota: Si el ladrillo se rompe, a veces el sonido lo controla el ComboManager.
                // Si quieres que suene doble (golpe + combo), deja esto. Si no, quítalo.
                if (brickSound != null) AudioManager.Instance.PlaySFX(brickSound);
                break;
            default:
                // Paredes y otros obstáculos
                if (bounceSound != null) AudioManager.Instance.PlaySFX(bounceSound);
                break;
        }
    }
}