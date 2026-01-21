using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    [Header("Settings")]
    public float padding = 0f;

    [Header("Gyroscope Settings (Vertical)")]
    public bool useGyroscope = true; // Esta variable se sobrescribirá en el Start
    public float gyroSpeed = 35f;
    public float deadZone = 0.02f;

    [Header("Shake Ability (Habilidad Agitar)")]
    public int maxShakes = 2;
    public float shakeCooldown = 5f;
    public float shakeThreshold = 2.8f;
    public float upwardForce = 18f;

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
        // --- CAMBIO PRINCIPAL: CARGAR PREFERENCIA DE USUARIO ---
        // Leemos si el giroscopio está activado o no en las opciones (1 = True, 0 = False)
        // Esto sobrescribe la checkbox del Inspector.
        useGyroscope = PlayerPrefs.GetInt("Gyroscope", 1) == 1;
        // -------------------------------------------------------

        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        paddleHalfWidth = transform.localScale.x / 2;

        movementPlane = new Plane(Vector3.back, transform.position);

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

        // INPUT TÁCTIL (Funciona siempre, tengas o no giroscopio)
        if (Application.isMobilePlatform)
            HandleTouchInput();
        else
            HandleMouseInput();

        // DETECCIÓN DE AGITACIÓN
        // Nota: Si quieres que la habilidad de agitar TAMBIÉN se desactive
        // si el giroscopio está apagado, añade "&& useGyroscope" al if.
        // Por ahora lo dejo activo porque el acelerómetro suele funcionar aunque no uses gyro para moverte.
        if (Application.isMobilePlatform)
        {
            HandleShakeDetection();
        }
    }

    void FixedUpdate()
    {
        // LOGICA DE MOVIMIENTO
        // Aquí es donde ocurre la magia:
        // 1. Si estás arrastrando con el dedo (!isDragging), el dedo manda.
        // 2. Si NO arrastras Y el giroscopio está activado (useGyroscope), usamos el tilt.
        // 3. Si useGyroscope es false (por el menú), esta línea no se cumple y la pala se queda quieta esperando el dedo.
        if (!isDragging && useGyroscope && Application.isMobilePlatform)
        {
            HandlePortraitGyro();
        }
    }

    // --- DETECCIÓN DE AGITACIÓN BRUSCA ---
    void HandleShakeDetection()
    {
        if (currentShakes > 0 && Time.time >= lastShakeTime + shakeCooldown)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
            {
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

        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ballObj in balls)
        {
            Rigidbody ballRb = ballObj.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.linearVelocity = new Vector3(ballRb.linearVelocity.x, 0, upwardForce);
            }
        }

        Debug.Log("¡Agitación detectada! Usos restantes: " + currentShakes);
    }

    void UpdateShakeUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.shakeCounterText != null)
        {
            GameManager.Instance.shakeCounterText.text = "SHAKES: " + currentShakes;
        }
    }

    // --- MOVIMIENTO GIROSCOPIO (Inclinación suave) ---
    void HandlePortraitGyro()
    {
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