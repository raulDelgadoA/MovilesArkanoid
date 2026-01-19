using UnityEngine;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Settings")]
    public GameObject brickPrefab;
    public GameObject bossPrefab;

    [Header("Layout")]
    public int columns = 7; // Subimos a 7 para que los patrones tengan centro
    public float paddingX = 0.2f;
    public float paddingZ = 0.2f;
    public float startZ = 4f;

    [Header("Power Ups")]
    [Range(0, 1)] public float chanceForPowerUp = 0.15f;

    private enum LevelPattern
    {
        StandardBlock,  // Bloque solido
        Checkerboard,   // Ajedrez (Denso)
        ZigZag,         // ZigZag vertical
        BrickWall,      // Como una pared de ladrillos real (Desplazado)
        Crosses,        // Cruces
        Maze            // Laberinto simple
    }

    [Header("DEBUG")]
    public bool useDebugPattern = false;
    public int debugPatternIndex = 0;

    public void GenerateLevel(int currentLevel)
    {
        foreach (Transform child in transform) DestroyImmediate(child.gameObject);

        if (brickPrefab == null) return;

        // Calculamos filas. Mínimo 4, máximo 14.
        int rows = Mathf.Clamp(4 + (currentLevel / 2), 4, 14);

        LevelPattern selectedPattern = ChoosePattern(currentLevel);

        // Setup de medidas
        Renderer brickRenderer = brickPrefab.GetComponent<Renderer>();
        float brickWidth = brickRenderer.bounds.size.x;
        float brickDepth = brickRenderer.bounds.size.z;
        float totalWidth = columns * (brickWidth + paddingX);
        float startX = -(totalWidth / 2) + (brickWidth / 2) + (paddingX / 2);

        for (int row = 0; row < rows; row++)
        {
            // Para el patrón de "Muro de Ladrillo", desplazamos las filas impares
            float rowOffsetX = 0;
            if (selectedPattern == LevelPattern.BrickWall && row % 2 != 0)
            {
                rowOffsetX = (brickWidth + paddingX) / 2f;
            }

            for (int col = 0; col < columns; col++)
            {
                if (ShouldSpawnBrick(selectedPattern, col, row))
                {
                    // Si es "Muro", saltamos el último de la fila desplazada para que no se salga
                    if (selectedPattern == LevelPattern.BrickWall && row % 2 != 0 && col == columns - 1) continue;

                    float posX = startX + (col * (brickWidth + paddingX)) + rowOffsetX;
                    float posZ = startZ - (row * (brickDepth + paddingZ));

                    CreateBrick(new Vector3(posX, 0.5f, posZ), row, rows);
                }
            }
        }
    }

    LevelPattern ChoosePattern(int level)
    {
        if (useDebugPattern) return (LevelPattern)debugPatternIndex;

        System.Array values = System.Enum.GetValues(typeof(LevelPattern));
        return (LevelPattern)values.GetValue(Random.Range(0, values.Length));
    }

    bool ShouldSpawnBrick(LevelPattern pattern, int col, int row)
    {
        switch (pattern)
        {
            case LevelPattern.StandardBlock:
                return true;

            case LevelPattern.Checkerboard:
                // Denso: Ladrillo sí, Ladrillo no
                return (col + row) % 2 == 0;

            case LevelPattern.ZigZag:
                // Crea líneas en ZigZag
                return (col % 4 == 0) || ((col + row) % 2 == 0);

            case LevelPattern.BrickWall:
                return true; // Se gestiona con el offset en el bucle principal

            case LevelPattern.Crosses:
                // Patrón de cruces +
                return (col % 2 == 0) || (row % 2 == 0);

            case LevelPattern.Maze:
                // Un patrón pseudo-aleatorio pero conectado
                // Deja huecos raros pero mantiene estructura
                return (col * row + col) % 3 != 0;

            default:
                return true;
        }
    }

    void CreateBrick(Vector3 pos, int currentRow, int totalRows)
    {
        GameObject newBrick = Instantiate(brickPrefab, pos, Quaternion.identity);
        newBrick.transform.SetParent(transform);

        BrickController brickScript = newBrick.GetComponent<BrickController>();
        Renderer rend = newBrick.GetComponent<Renderer>();

        if (brickScript != null)
        {
            // Lógica de PowerUps y Colores (Igual que antes)
            if (Random.value < chanceForPowerUp)
            {
                brickScript.SetupPowerUp((PowerUpType)Random.Range(1, 5));
            }
            else if (rend != null)
            {
                float hue = (float)currentRow / totalRows;
                Color finalColor = Color.HSVToRGB(hue, 0.75f, 1f);
                rend.material.color = finalColor;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", finalColor * 2.5f);
            }
        }
    }

    public void SpawnBossLevel()
    {
        foreach (Transform child in transform) DestroyImmediate(child.gameObject);

        if (bossPrefab == null) return;

        // --- CAMBIO ---
        // Usamos Y = 1.0f para asegurarnos que flote por encima del suelo.
        // Si tu boss mide 2 metros de alto, esto lo dejará posado en el suelo (si su pivote es el centro).
        // Ajusta este 1.0f si sigue quedando bajo o muy alto.
        Vector3 bossPos = new Vector3(0, 2.0f, 5f);

        GameObject boss = Instantiate(bossPrefab, bossPos, Quaternion.Euler(0, 0, 0));

        boss.transform.SetParent(transform);

        BossController bossScript = boss.GetComponent<BossController>();
        if (bossScript != null)
        {
            int currentLevel = GameManager.Instance.currentLevel;
            bossScript.maxHealth = 10 * (currentLevel / 5);
        }
    }
}