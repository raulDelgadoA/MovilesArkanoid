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
    public AudioClip bounceSound;
    public AudioClip paddleSound;
    public AudioClip brickSound;
    public AudioClip powerUpSound;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 lastVelocity;
    private bool isLaunched = false;
    private float offsetZ;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // --- BLOQUE DE SEGURIDAD NUEVO ---
        // Si nadie me ha asignado un GameManager, lo busco yo mismo
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;

            // Si el Singleton fallara, lo buscamos por fuerza bruta
            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();
        }
        // ---------------------------------

        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (trailEffect != null) trailEffect.enabled = false;

        // Si la raqueta no está asignada, intenta buscarla también (opcional pero útil)
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

        PlayCollisionSound(collision.gameObject.tag);

        if (collision.gameObject.CompareTag("Paddle")) HandlePaddleCollision(collision);

        if (collision.gameObject.CompareTag("Brick"))
        {
            BrickController brick = collision.gameObject.GetComponent<BrickController>();

            if (brick != null && brick.currentPowerUp != PowerUpType.None)
            {
                ActivatePowerUp(brick.currentPowerUp, collision.transform.position);
            }

            if (gameManager != null) gameManager.BrickDestroyed();
            Destroy(collision.gameObject);
        }
    }

    // --- AQUÍ ESTABA LA CLAVE QUE FALTABA ---
    void ActivatePowerUp(PowerUpType type, Vector3 position)
    {
        if (powerUpSound != null && audioSource != null) audioSource.PlayOneShot(powerUpSound);

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

            // --- ESTE ES EL CASO QUE TE FALTABA ---
            case PowerUpType.SafetyNet:
                Debug.Log("Bola: ¡He tocado un ladrillo SafetyNet!");
                if (gameManager != null)
                    gameManager.ActivateSafetyNet(10f); // Activa la barrera por 10 segundos
                else
                    Debug.LogError("Bola: No encuentro el GameManager para activar la barrera.");
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

    void PlayCollisionSound(string tag)
    {
        if (audioSource == null) return;
        switch (tag)
        {
            case "Paddle": if (paddleSound != null) audioSource.PlayOneShot(paddleSound); break;
            case "Brick": if (brickSound != null) audioSource.PlayOneShot(brickSound); break;
            default: if (bounceSound != null) audioSource.PlayOneShot(bounceSound); break;
        }
    }
}