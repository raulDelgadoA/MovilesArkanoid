using UnityEngine;

public class CameraGyroEffect : MonoBehaviour
{
    [Header("Settings")]
    public bool enableEffect = true;

    [Header("Horizontal Tilt (Roll) - Más notable")]
    public float maxTiltX = 6f;     // Girar muñecas (Lado a lado)
    public bool inverseX = true;

    [Header("Vertical Tilt (Pitch) - Muy sutil")]
    public float maxTiltY = 2f;     // Inclinar adelante/atrás (Puesto a 2 para que sea suave)
    public bool inverseY = true;

    [Header("Smoothness")]
    public float rotationSpeed = 5f;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
    }

    void Update()
    {
        if (!enableEffect) return;

        float inputX = 0f;
        float inputY = 0f;

#if UNITY_EDITOR
        // EN PC: Ratón
        inputX = (Input.mousePosition.x / Screen.width) * 2 - 1;
        inputY = (Input.mousePosition.y / Screen.height) * 2 - 1;

        inputX = Mathf.Clamp(inputX, -1f, 1f);
        inputY = Mathf.Clamp(inputY, -1f, 1f);
#else
        // EN MÓVIL: Acelerómetro
        inputX = Input.acceleration.x;
        // Ajustamos el offset vertical (0.6f suele ser el ángulo cómodo de sostener el móvil)
        inputY = Input.acceleration.y + 0.6f; 
#endif

        // Calculamos los ángulos
        // Fíjate que inputY se multiplica por maxTiltY, que ahora es pequeño (2.0)
        float rotX = inputX * maxTiltX;
        float rotY = inputY * maxTiltY;

        if (inverseX) rotX = -rotX;
        if (inverseY) rotY = -rotY;

        // Aplicamos la rotación
        // Z = Roll (Lado a lado)
        // X = Pitch (Arriba/Abajo)
        Quaternion targetRotation = initialRotation * Quaternion.Euler(rotY, 0, rotX);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}