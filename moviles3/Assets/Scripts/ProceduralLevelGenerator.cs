using UnityEngine;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Settings")]
    public GameObject brickPrefab;

    [Header("Layout")]
    public int columns = 6;
    public float paddingX = 0.2f;
    public float paddingZ = 0.2f;
    public float startZ = 8f;

    [Header("Power Ups")]
    [Range(0, 1)] public float chanceForPowerUp = 0.2f;

    [Header("DEBUG TESTING (Marca uno para forzar)")]
    public bool forceExtraBall = false;
    public bool forceSpeedUp = false;
    public bool forceSlowDown = false;
    public bool forceSafetyNet = false;

    public void GenerateLevel(int currentLevel)
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        if (brickPrefab == null) return;

        int rows = 2 + currentLevel;
        if (rows > 15) rows = 15;

        Renderer brickRenderer = brickPrefab.GetComponent<Renderer>();
        float brickWidth = brickRenderer.bounds.size.x;
        float brickDepth = brickRenderer.bounds.size.z;

        float totalWidth = columns * (brickWidth + paddingX);
        float startX = -(totalWidth / 2) + (brickWidth / 2) + (paddingX / 2);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float posX = startX + (col * (brickWidth + paddingX));
                float posZ = startZ - (row * (brickDepth + paddingZ));
                float posY = 0.5f;

                Vector3 spawnPos = new Vector3(posX, posY, posZ);

                GameObject newBrick = Instantiate(brickPrefab, spawnPos, Quaternion.identity);
                newBrick.transform.SetParent(transform);

                // --- AQUÍ ESTABA EL PROBLEMA ---
                BrickController brickScript = newBrick.GetComponent<BrickController>();
                if (brickScript != null)
                {
                    PowerUpType typeToAssign = PowerUpType.None;

                    // 1. PRIMERO MIRAMOS SI HAY ALGUNA CASILLA DE DEBUG MARCADA
                    if (forceSafetyNet) typeToAssign = PowerUpType.SafetyNet;
                    else if (forceExtraBall) typeToAssign = PowerUpType.ExtraBall;
                    else if (forceSpeedUp) typeToAssign = PowerUpType.SpeedUp;
                    else if (forceSlowDown) typeToAssign = PowerUpType.SlowDown;

                    // 2. SI NO HAY NINGUNA MARCADA, ENTONCES USAMOS EL AZAR
                    else if (Random.value < chanceForPowerUp)
                    {
                        typeToAssign = (PowerUpType)Random.Range(1, 5);
                    }

                    // 3. APLICAMOS EL RESULTADO
                    if (typeToAssign != PowerUpType.None)
                    {
                        brickScript.SetupPowerUp(typeToAssign);
                    }
                    else
                    {
                        Renderer rend = newBrick.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            // Generamos el color arcoíris
                            Color rainbowColor = Color.HSVToRGB((float)row / rows, 0.8f, 0.9f);

                            // 1. Asignamos color base
                            rend.material.color = rainbowColor;

                            // 2. Asignamos EMISIÓN NEÓN (Intensidad x3)
                            rend.material.EnableKeyword("_EMISSION");
                            rend.material.SetColor("_EmissionColor", rainbowColor * 3f);
                        }
                    }
                }
            }
        }
    }
}