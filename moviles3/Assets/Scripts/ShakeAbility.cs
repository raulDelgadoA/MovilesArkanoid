using UnityEngine;
using System.Collections;
using TMPro;

namespace Cyan
{
    public class ShakeAbility : MonoBehaviour
    {
        [Header("Configuración de Habilidad")]
        public int maxUsesPerLevel = 2;       // Límite de 2 usos
        public float cooldownDuration = 5f;  // Cooldown de 5 segundos
        public float upwardForce = 18f;      // Fuerza del impulso hacia adelante/arriba

        [Header("Detección de Agitación")]
        [Tooltip("Sensibilidad: 2.0 es muy sensible, 3.5 requiere un sacudón fuerte.")]
        public float shakeThreshold = 2.8f;

        [Header("UI References")]
        public TextMeshProUGUI shakeCounterText; // Arrastra el objeto ShakeText aquí

        private int currentUses;
        private bool isCooldown = false;

        void Start()
        {
            // Inicializamos los usos y la interfaz al arrancar el nivel
            currentUses = maxUsesPerLevel;
            UpdateUI();

            // Verificamos si el acelerómetro está disponible
            if (SystemInfo.supportsAccelerometer)
            {
                Debug.Log("<color=green>ShakeAbility:</color> Acelerómetro detectado y listo.");
            }
        }

        void Update()
        {
            // Solo procesamos la detección si el juego está activo y hay usos disponibles
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver && currentUses > 0 && !isCooldown)
            {
                // Detectamos la magnitud del vector de aceleración (fuerza G)
                // Al usar sqrMagnitude ahorramos recursos de procesamiento en el móvil
                if (Input.acceleration.sqrMagnitude >= Mathf.Pow(shakeThreshold, 2))
                {
                    ExecuteShakeImpulse();
                }
            }
        }

        void ExecuteShakeImpulse()
        {
            isCooldown = true;
            currentUses--;
            UpdateUI();

            // Buscamos todas las bolas activas con el Tag "Ball"
            GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");

            if (balls.Length > 0)
            {
                foreach (GameObject ballObj in balls)
                {
                    Rigidbody rb = ballObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Reseteamos la velocidad vertical previa para que el impulso sea consistente
                        // Aplicamos la fuerza en el eje Z (profundidad del escenario)
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, upwardForce);
                    }
                }

                // Feedback: Sacudida de cámara (ComboEffectManager) y vibración nativa
                if (ComboEffectManager.Instance != null)
                    ComboEffectManager.Instance.RegisterHit(Vector3.zero, 0);

                Vibration.Vibrate(80); // Vibración de 80ms para confirmar la acción

                StartCoroutine(CooldownRoutine());
            }
            else
            {
                // Si por error se activa sin bolas en juego, devolvemos el uso
                currentUses++;
                isCooldown = false;
                UpdateUI();
            }
        }

        IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(cooldownDuration);
            isCooldown = false;
        }

        void UpdateUI()
        {
            if (shakeCounterText != null)
                shakeCounterText.text = "SHAKES: " + currentUses;
        }
    }
}