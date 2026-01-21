using UnityEngine;

public class CameraGyroEffect : MonoBehaviour
{
    [Header("Settings")]
    public bool enableEffect = true;

    [Header("Horizontal Tilt (Roll)")]
    public float maxTiltX = 6f;
    public bool inverseX = true;

    [Header("Smoothness")]
    public float rotationSpeed = 5f;

    [Header("UI Parallax Effect")]
    public Transform uiContainer;

    [Range(-2f, 2f)]
    public float uiTiltMultiplier = 0.5f;

    [HideInInspector] public Vector3 shakeOffset;
    private Quaternion initialRotation;
    private Quaternion initialUiRotation;

    void Start()
    {
        initialRotation = transform.rotation;
        if (uiContainer != null) initialUiRotation = uiContainer.localRotation;
    }

    void Update()
    {
        if (!enableEffect) return;

        // 1. CHEQUEO DE OPCIONES
        bool isGyroEnabled = true;
        if (OptionsManager.Instance != null) isGyroEnabled = OptionsManager.Instance.GyroscopeEnabled;

        // Si está DESACTIVADO en opciones, forzamos el retorno al centro y salimos
        if (!isGyroEnabled)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, rotationSpeed * Time.deltaTime);
            if (uiContainer != null)
            {
                uiContainer.localRotation = Quaternion.Slerp(uiContainer.localRotation, initialUiRotation, rotationSpeed * Time.deltaTime);
            }
            return;
        }

        // --- CÓDIGO NORMAL DE GIROSCOPIO (Solo se ejecuta si isGyroEnabled es true) ---

        float inputX = 0f;

#if UNITY_EDITOR
        if (Screen.width <= 0 || Screen.height <= 0) return;
        inputX = (Input.mousePosition.x / Screen.width) * 2 - 1;
        if (float.IsNaN(inputX)) inputX = 0;
        inputX = Mathf.Clamp(inputX, -1f, 1f);
#else
        inputX = Input.acceleration.x;
#endif

        float rotX = inputX * maxTiltX;
        if (inverseX) rotX = -rotX;

        // 1. CÁMARA
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, rotX);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 2. UI (INTERFAZ)
        if (uiContainer != null)
        {
            float uiRotX = rotX * uiTiltMultiplier;
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