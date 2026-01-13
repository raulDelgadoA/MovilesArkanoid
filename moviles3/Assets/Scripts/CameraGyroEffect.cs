using UnityEngine;

public class CameraGyroEffect : MonoBehaviour
{
    [Header("Settings")]
    public bool enableEffect = true;

    [Header("Horizontal Tilt (Roll)")]
    public float maxTiltX = 6f;
    public bool inverseX = true;

    [Header("Vertical Tilt (Pitch)")]
    public float maxTiltY = 2f;
    public bool inverseY = true;

    [Header("Smoothness")]
    public float rotationSpeed = 5f;

    [Header("UI Parallax Effect")]
    public Transform uiContainer;

    // CAMBIO: Ampliado el rango a negativo por si quieres experimentar (-2 a 2)
    [Range(-2f, 2f)]
    public float uiTiltMultiplier = 0.5f;

    [HideInInspector] public Vector3 shakeOffset;
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
        if (Screen.width <= 0 || Screen.height <= 0) return;

        inputX = (Input.mousePosition.x / Screen.width) * 2 - 1;
        inputY = (Input.mousePosition.y / Screen.height) * 2 - 1;

        if (float.IsNaN(inputX)) inputX = 0;
        if (float.IsNaN(inputY)) inputY = 0;

        inputX = Mathf.Clamp(inputX, -1f, 1f);
        inputY = Mathf.Clamp(inputY, -1f, 1f);
#else
        inputX = Input.acceleration.x;
        inputY = Input.acceleration.y + 0.6f; 
#endif

        float rotX = inputX * maxTiltX;
        float rotY = inputY * maxTiltY;

        if (inverseX) rotX = -rotX;
        if (inverseY) rotY = -rotY;

        // 1. CÁMARA
        Quaternion targetRotation = initialRotation * Quaternion.Euler(rotY, 0, rotX);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 2. UI (INTERFAZ)
        if (uiContainer != null)
        {
            float uiRotX = rotX * uiTiltMultiplier;
            float uiRotY = rotY * uiTiltMultiplier;

            // --- CAMBIO IMPORTANTE: AÑADIDOS SIGNOS MENOS (-) ---
            // Al poner -uiRotY y -uiRotX, invertimos el giro de la interfaz
            // para que vaya al lado contrario que la cámara.
            Quaternion uiTarget = Quaternion.Euler(-uiRotY, 0, -uiRotX);

            if (!IsNaN(uiTarget))
            {
                uiContainer.localRotation = Quaternion.Slerp(uiContainer.localRotation, uiTarget, rotationSpeed * Time.deltaTime);
            }
        }
    }

    bool IsNaN(Quaternion q)
    {
        return float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w);
    }
}