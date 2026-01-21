using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    [Header("Settings")]
    public float padding = 0f;

    [Header("Gyroscope Settings (Vertical)")]
    public bool useGyroscope = true;
    public float gyroSpeed = 35f;
    public float deadZone = 0.02f;

    [Header("Shake Ability (Habilidad Agitar)")]
    public int maxShakes = 2;          // Límite de 2 usos por nivel
    public float shakeCooldown = 5f;   // Cooldown de 5 segundos
    public float shakeThreshold = 2.8f;// Sensibilidad del sacudón
    public float upwardForce = 18f;    // Fuerza del impulso hacia arriba (Z)

    private int currentShakes;
    private float lastShakeTime;

    [Header("References")]
    public Transform leftWall;
    public Transform rightWall;

    // Referencias internas
    private Camera mainCamera;
    private Rigidbody rb;

    // Drag Anywhere
    private bool isDragging = false;
    private int touchFingerId = -1;
    private float dragStartX;
    private float paddleStartX;

    // Límites
    private float minXBound;
    private float maxXBound;
    private float paddleHalfWidth;

    // Plano matemático
    private Plane movementPlane;

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        paddleHalfWidth = transform.localScale.x / 2;

        // Plano para detectar toques
        movementPlane = new Plane(Vector3.back, transform.position);

        // Inicializar usos de agitación
        currentShakes = maxShakes;
        UpdateShakeUI();

        CalculateBounds();
    }

    void Update()
    {
        if (Application.isEditor && !isDragging)
        {
            CalculateBounds();
        }

        // INPUT TÁCTIL (El dedo siempre manda sobre el giro)
        if (Application.isMobilePlatform)
            HandleTouchInput();
        else
            HandleMouseInput();

        // DETECCIÓN DE AGITACIÓN (Independiente del giroscopio)
        if (Application.isMobilePlatform)
        {
            HandleShakeDetection();
        }
    }

    void FixedUpdate()
    {
        // Si NO tocas la pantalla, usamos el giroscopio para movimiento suave
        if (!isDragging && useGyroscope && Application.isMobilePlatform)
        {
            HandlePortraitGyro();
        }
    }

    // --- DETECCIÓN DE AGITACIÓN BRUSCA ---
    void HandleShakeDetection()
    {
        // Solo procesamos si quedan usos, no estamos en cooldown y el juego sigue activo
        if (currentShakes > 0 && Time.time >= lastShakeTime + shakeCooldown)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
            {
                // Usamos sqrMagnitude para detectar fuerza brusca en cualquier eje
                // Esto no interfiere con el giroscopio porque el giro es una inclinación leve (X), 
                // mientras que esto requiere un pico de energía física.
                if (Input.acceleration.sqrMagnitude >= Mathf.Pow(shakeThreshold, 2))
                {
                    ExecuteShakeImpulse();
                }
            }
        }
    }

    void ExecuteShakeImpulse()
    {
        lastShakeTime = Time.time;
        currentShakes--;
        UpdateShakeUI();

        // Impulsar todas las bolas activas hacia arriba (Eje Z)
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ballObj in balls)
        {
            Rigidbody ballRb = ballObj.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // Reseteamos velocidad vertical (Z) y aplicamos el impulso
                ballRb.linearVelocity = new Vector3(ballRb.linearVelocity.x, 0, upwardForce);
            }
        }

        if (ComboEffectManager.Instance != null)
        {
            ComboEffectManager.Instance.RegisterHit(Vector3.zero, 0);
        }

        Debug.Log("¡Agitación detectada! Usos restantes: " + currentShakes);
    }

    void UpdateShakeUI()
    {
        // Actualizamos el contador en el GameManager
        if (GameManager.Instance != null && GameManager.Instance.shakeCounterText != null)
        {
            GameManager.Instance.shakeCounterText.text = "SHAKES: " + currentShakes;
        }
    }

    // --- MOVIMIENTO GIROSCOPIO (Inclinación suave) ---
    void HandlePortraitGyro()
    {
        // Input.acceleration.x detecta solo la inclinación lateral
        float tiltInput = Input.acceleration.x;

        if (Mathf.Abs(tiltInput) < deadZone)
        {
            tiltInput = 0;
            rb.linearVelocity = Vector3.zero;
        }

        float moveAmount = tiltInput * gyroSpeed * Time.fixedDeltaTime;
        float newX = rb.position.x + moveAmount;

        newX = Mathf.Clamp(newX, minXBound, maxXBound);

        Vector3 targetPos = new Vector3(newX, transform.position.y, transform.position.z);
        rb.MovePosition(targetPos);
    }

    // --- RESTO DE MÉTODOS (Táctil y cálculos) ---
    void StartDrag(Vector2 screenPosition, int fingerId)
    {
        float worldX = GetWorldXFromScreen(screenPosition);
        if (float.IsNaN(worldX)) return;

        isDragging = true;
        touchFingerId = fingerId;
        dragStartX = worldX;
        paddleStartX = rb.position.x;
    }

    void UpdateDrag(Vector2 screenPosition)
    {
        float currentWorldX = GetWorldXFromScreen(screenPosition);
        if (float.IsNaN(currentWorldX)) return;

        float offset = currentWorldX - dragStartX;
        float targetX = paddleStartX + offset;
        float clampedX = Mathf.Clamp(targetX, minXBound, maxXBound);

        Vector3 newPos = new Vector3(clampedX, transform.position.y, transform.position.z);
        rb.MovePosition(newPos);
    }

    void EndDrag()
    {
        isDragging = false;
        touchFingerId = -1;
        rb.linearVelocity = Vector3.zero;
    }

    void CalculateBounds()
    {
        if (leftWall != null && rightWall != null)
        {
            float leftWallInnerEdge = leftWall.position.x + (leftWall.localScale.x / 2);
            float rightWallInnerEdge = rightWall.position.x - (rightWall.localScale.x / 2);

            minXBound = leftWallInnerEdge + paddleHalfWidth + padding;
            maxXBound = rightWallInnerEdge - paddleHalfWidth - padding;
        }
    }

    float GetWorldXFromScreen(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        float enter;
        if (movementPlane.Raycast(ray, out enter))
        {
            return ray.GetPoint(enter).x;
        }
        return float.NaN;
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began && !isDragging)
                {
                    StartDrag(touch.position, touch.fingerId);
                }

                if (isDragging && touch.fingerId == touchFingerId)
                {
                    UpdateDrag(touch.position);

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        EndDrag();
                    }
                }
            }
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) StartDrag(Input.mousePosition, -1);
        if (isDragging && Input.GetMouseButton(0)) UpdateDrag(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) EndDrag();
    }
}