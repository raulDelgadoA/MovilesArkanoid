using UnityEngine;



public class BallController : MonoBehaviour

{

    [Header("Ball Physics")]

    public float initialSpeed = 15f;

    public float maxSpeed = 25f;

    public float minVerticalSpeed = 2f;



    [Header("Blow Ability (Freno por Soplido)")]

    public float blowThreshold = 0.15f; // Sensibilidad del micro

    public float slowDownFactor = 2.5f; // Fuerza del freno (Drag)



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

    private Vector3 lastVelocity;

    private bool isLaunched = false;

    private float offsetZ;



    // Variables para el Micrófono (Soplido)

    //private AudioClip _micClip;

    //private string _deviceName;



    void Awake()

    {

        rb = GetComponent<Rigidbody>();

    }



    void Start()

    {

        // --- BLOQUE DE SEGURIDAD ---

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



        // --- INICIALIZAR MICRÓFONO PARA SOPLIDO ---

        /*if (Microphone.devices.Length > 0)

        {

            _deviceName = Microphone.devices[0];

            // Grabación en bucle de 1 segundo a 44.1kHz

            _micClip = Microphone.Start(_deviceName, true, 1, 44100);

        }*/

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

        else

        {

            // Solo procesamos el soplido si la bola ya ha sido lanzada

            //HandleBlowBrake();

        }



        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f && !float.IsNaN(rb.linearVelocity.x))

            lastVelocity = rb.linearVelocity;

    }



    void FixedUpdate()

    {

        if (isLaunched && rb != null)

        {

            Vector3 currentVelocity = rb.linearVelocity;

            // Aplicamos la velocidad constante normal

            rb.linearVelocity = currentVelocity.normalized * initialSpeed;

            PreventFlatAngles();

        }

    }



    // --- NUEVA FUNCIÓN: DETECCIÓN DE SOPLIDO ---

    /*void HandleBlowBrake()

    {

        if (_micClip == null) return;



        // Analizamos las últimas muestras del micrófono

        float[] waveData = new float[128];

        int micPos = Microphone.GetPosition(_deviceName) - 128;

        if (micPos < 0) return;



        _micClip.GetData(waveData, micPos);

        float sum = 0;

        for (int i = 0; i < 128; i++) sum += waveData[i] * waveData[i];

        float level = Mathf.Sqrt(sum / 128); // Valor RMS del volumen



        // Si el nivel de soplido supera el umbral, aumentamos el drag (fricción con el aire)

        if (level > blowThreshold)

        {

            rb.linearDamping = slowDownFactor; // La bola "flota" o cae lento

        }

        else

        {

            rb.linearDamping = 0f; // Vuelve a su física normal de rebote

        }

    }*/



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

            // 1. ¿ES EL BOSS?

            BossController boss = collision.gameObject.GetComponent<BossController>();

            if (boss != null)

            {

                boss.TakeDamage();

                PlayCollisionSound(collision.gameObject.tag);

                if (gameManager != null) gameManager.AddScore(500);

                if (ComboEffectManager.Instance != null)

                    ComboEffectManager.Instance.RegisterHit(collision.transform.position, 500);



#if UNITY_ANDROID || UNITY_IOS

                        Handheld.Vibrate();

#endif

                return;

            }



            // 2. ¿ES UN PROYECTIL? ---

            // Si es un proyectil, lo tratamos como ladrillo (PowerUps)

            // para que no reste del contador de victoria.

            if (collision.gameObject.GetComponent<BossProjectile>() != null)

            {

                // Lógica de PowerUps (igual que un ladrillo)

                BrickController brickProj = collision.gameObject.GetComponent<BrickController>();

                if (brickProj != null && brickProj.currentPowerUp != PowerUpType.None)

                {

                    ActivatePowerUp(brickProj.currentPowerUp, collision.transform.position);

                }



                // Damos unos puntitos extra por la habilidad

                if (gameManager != null) gameManager.AddScore(100);

                if (ComboEffectManager.Instance != null)

                    ComboEffectManager.Instance.RegisterHit(collision.transform.position, 100);

                // Destruimos el proyectil

                Destroy(collision.gameObject);



#if UNITY_ANDROID || UNITY_IOS

                        Handheld.Vibrate();

#endif



                //Hacemos return para NO ejecutar el BrickDestroyed de abajo

                return;

            }

            // ---------------------------------------



            // 3. ES UN LADRILLO NORMAL DEL NIVEL

            BrickController brick = collision.gameObject.GetComponent<BrickController>();

            if (brick != null && brick.currentPowerUp != PowerUpType.None)

            {

                ActivatePowerUp(brick.currentPowerUp, collision.transform.position);

            }



            if (gameManager != null)

            {

                // Este sí cuenta para ganar el nivel

                gameManager.BrickDestroyed(collision.transform.position);

#if UNITY_ANDROID || UNITY_IOS

                        Handheld.Vibrate();

#endif

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

}