using UnityEngine;

public class CameraGyroEffect : MonoBehaviour
{
    [Header("Settings")]
    public bool enableEffect = true;

    [Header("Horizontal Tilt (Roll)")]
    public float maxTiltX = 6f;
    public bool inverseX = true;

    // [Header("Vertical Tilt (Pitch)")] // Ocultamos esto en el Inspector porque ya no se usa
    // public float maxTiltY = 2f;      
    // public bool inverseY = true;

    [Header("Smoothness")]
    public float rotationSpeed = 5f;

    [Header("UI Parallax Effect")]
    public Transform uiContainer;

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
        // float inputY = 0f; // ELIMINADO: No necesitamos input vertical

#if UNITY_EDITOR
        if (Screen.width <= 0 || Screen.height <= 0) return;

        inputX = (Input.mousePosition.x / Screen.width) * 2 - 1;
        // inputY = (Input.mousePosition.y / Screen.height) * 2 - 1; // ELIMINADO

        if (float.IsNaN(inputX)) inputX = 0;

        inputX = Mathf.Clamp(inputX, -1f, 1f);
#else
        inputX = Input.acceleration.x;
        // inputY = Input.acceleration.y + 0.6f; // ELIMINADO
#endif

        float rotX = inputX * maxTiltX;
        // float rotY = inputY * maxTiltY; // ELIMINADO

        if (inverseX) rotX = -rotX;

        // --- CAMBIO PRINCIPAL ---
        // En Quaternion.Euler(x, y, z):
        // x = Pitch (Vertical) -> Lo forzamos a 0
        // y = Yaw (Giro) -> 0
        // z = Roll (Horizontal) -> rotX

        // 1. CÁMARA
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, rotX);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 2. UI (INTERFAZ)
        if (uiContainer != null)
        {
            float uiRotX = rotX * uiTiltMultiplier;
            // float uiRotY = rotY * uiTiltMultiplier; // ELIMINADO

            // Forzamos 0 en el primer parámetro para que la UI tampoco suba/baje
            Quaternion uiTarget = Quaternion.Euler(0, 0, -uiRotX);

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