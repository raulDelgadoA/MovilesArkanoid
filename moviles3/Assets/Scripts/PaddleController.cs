using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    [Header("Settings")]
    public float padding = 0f;

    [Header("Gyroscope Settings (Vertical)")]
    public bool useGyroscope = true;
    // En vertical la pantalla es más estrecha, quizás necesites menos velocidad o más precisión
    public float gyroSpeed = 35f;
    public float deadZone = 0.02f;     // Zona muerta pequeña para detectar movimientos sutiles

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
    }

    void FixedUpdate()
    {
        // Si NO tocas la pantalla, usamos el giroscopio
        if (!isDragging && useGyroscope && Application.isMobilePlatform)
        {
            HandlePortraitGyro();
        }
    }

    // --- MOVIMIENTO GIROSCOPIO (VERTICAL) ---
    void HandlePortraitGyro()
    {
        // En MODO VERTICAL (Portrait):
        // Input.acceleration.x detecta la inclinación Izquierda/Derecha.
        // Izquierda = Negativo, Derecha = Positivo.
        float tiltInput = Input.acceleration.x;

        // Aplicamos la zona muerta (Deadzone)
        if (Mathf.Abs(tiltInput) < deadZone)
        {
            tiltInput = 0;
            // Frenamos en seco para dar sensación de control preciso
            rb.linearVelocity = Vector3.zero;
        }

        // Calculamos el movimiento
        float moveAmount = tiltInput * gyroSpeed * Time.fixedDeltaTime;
        float newX = rb.position.x + moveAmount;

        // Respetamos los límites de las paredes
        newX = Mathf.Clamp(newX, minXBound, maxXBound);

        // Movemos el Rigidbody
        Vector3 targetPos = new Vector3(newX, transform.position.y, transform.position.z);
        rb.MovePosition(targetPos);
    }

    // --- EL RESTO DEL CÓDIGO (TÁCTIL Y CÁLCULOS) SIGUE IGUAL ---

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