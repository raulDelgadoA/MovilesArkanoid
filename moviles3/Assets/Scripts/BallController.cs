using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Ball Physics")]
    public float initialSpeed = 15f;
    public float maxSpeed = 25f;
    public float minVerticalSpeed = 2f;

    [Header("Blow Ability (Freno por Soplido)")]
    public float blowThreshold = 0.15f;
    public float slowDownFactor = 2.5f;

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
    private Vector3 lastVelocity;
    private bool isLaunched = false;
    private float offsetZ;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
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
            PaddleController paddleScript = FindFirstObjectByType<PaddleController>();
            if (paddleScript != null) paddle = paddleScript.transform;
        }

        if (paddle != null) offsetZ = transform.position.z - paddle.position.z;
    }

    void Update()
    {
        if (!isLaunched)
        {
            // Solo actualizamos la posición si NO estamos usando el giroscopio o si la bola está pegada
            // (La lógica de movimiento la lleva el Paddle, aquí solo seguimos)
            if (paddle != null)
            {
                Vector3 newPos = new Vector3(paddle.position.x, transform.position.y, paddle.position.z + offsetZ);
                transform.position = newPos;
            }

            // Lanzamiento: Permitimos clic o toque
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
        Vector3 reflected = Vector3.Reflect(lastVelocity.normalized, normal);
        reflected.y = 0;
        rb.linearVelocity = reflected.normalized * initialSpeed;

        PlayCollisionSound(collision.gameObject.tag);

        if (collision.gameObject.CompareTag("Paddle"))
        {
            HandlePaddleCollision(collision);
        }
        else if (collision.gameObject.CompareTag("Brick"))
        {
            BossController boss = collision.gameObject.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage();
                if (gameManager != null) gameManager.AddScore(500);
                if (ComboEffectManager.Instance != null)
                    ComboEffectManager.Instance.RegisterHit(collision.transform.position, 500);

                VibrarSuave(120, 150);
                return;
            }

            if (collision.gameObject.GetComponent<BossProjectile>() != null)
            {
                BrickController brickProj = collision.gameObject.GetComponent<BrickController>();
                if (brickProj != null && brickProj.currentPowerUp != PowerUpType.None)
                {
                    ActivatePowerUp(brickProj.currentPowerUp, collision.transform.position);
                }

                if (gameManager != null) gameManager.AddScore(100);
                if (ComboEffectManager.Instance != null)
                    ComboEffectManager.Instance.RegisterHit(collision.transform.position, 100);

                Destroy(collision.gameObject);
                VibrarSuave(50, 80);
                return;
            }

            BrickController brick = collision.gameObject.GetComponent<BrickController>();
            if (brick != null && brick.currentPowerUp != PowerUpType.None)
            {
                ActivatePowerUp(brick.currentPowerUp, collision.transform.position);
            }

            if (gameManager != null)
            {
                gameManager.BrickDestroyed(collision.transform.position);
                VibrarSuave(40, 45);
            }
            Destroy(collision.gameObject);
        }
    }

    void ActivatePowerUp(PowerUpType type, Vector3 position)
    {
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

    void PlayCollisionSound(string tag)
    {
        if (AudioManager.Instance == null) return;

        switch (tag)
        {
            case "Paddle":
                if (paddleSound != null) AudioManager.Instance.PlaySFX(paddleSound);
                break;
            case "Brick":
                if (brickSound != null) AudioManager.Instance.PlaySFX(brickSound);
                break;
            default:
                if (bounceSound != null) AudioManager.Instance.PlaySFX(bounceSound);
                break;
        }
    }

    // =========================================================
    // MODIFICADO: COMPROBACIÓN DE OPCIONES
    // =========================================================
    void VibrarSuave(long milisegundos, int fuerza)
    {
        // 1. CHEQUEO DE SEGURIDAD: ¿Está la vibración activada en opciones?
        if (OptionsManager.Instance != null && !OptionsManager.Instance.VibrationEnabled)
        {
            return; // Si está desactivado, salimos inmediatamente
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    int sdkVersion = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                    
                    if (sdkVersion >= 26) 
                    {
                        using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milisegundos, fuerza))
                        {
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else 
                    {
                        vibrator.Call("vibrate", milisegundos);
                    }
                }
            }
        }
        catch (System.Exception) { }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}