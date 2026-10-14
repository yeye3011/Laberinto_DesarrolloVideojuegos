using System.Collections.Generic;
using UnityEngine;

public class CheeseSpawner : MonoBehaviour
{
    [Header("Área válida para quesos")]
    [SerializeField] private GameObject cheeseSpawnArea;

    [Header("Paredes")]
    [SerializeField] private LayerMask wallLayer;

    [Header("Prefabs de queso")]
    [SerializeField] private GameObject normalCheesePrefab;
    [SerializeField] private GameObject iceCheesePrefab;
    [SerializeField] private GameObject speedCheesePrefab;

    [Header("Distancia mínima entre quesos")]
    [SerializeField] private float minimumDistance = 80f;

    [Header("Altura del queso")]
    [SerializeField] private float spawnY = 12f;

    [Header("Margen adicional contra paredes")]
    [SerializeField] private float wallClearance = 2.5f;

    [Header("Jugadores")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float playerClearRadius = 50f;

    [Header("Rotación del queso")]
    [SerializeField] private float minimumRotationY = -180f;
    [SerializeField] private float maximumRotationY = 180f;

    [Header("Intentos")]
    [SerializeField] private int maxAttemptsPerCheese = 500;

    private readonly List<GameObject> spawnedCheeses = new List<GameObject>();

    private readonly List<Vector3> cheesePositions = new List<Vector3>();

    private readonly List<NetworkCheeseData> spawnedNetworkCheeses = new List<NetworkCheeseData>();

    private MeshFilter[] areaMeshFilters;

    // =========================================================
    // PREPARAR ÁREA
    // =========================================================

    private void PrepareSpawnArea()
    {

        areaMeshFilters = cheeseSpawnArea.GetComponentsInChildren<MeshFilter>();

    }


    // =========================================================
    // HOST: GENERAR QUESOS
    // =========================================================

    public List<NetworkCheeseData> SpawnHostCheeses()
    {
        PrepareSpawnArea();

        ClearCheeses();

        spawnedNetworkCheeses.Clear();

        // =====================================================
        // CREAR LOS 50 QUESOS CON IDs FIJOS
        // =====================================================

        for (int cheeseId = 0; cheeseId < 50; cheeseId++)
        {
            GameObject prefab;
            string cheeseType;

            // 0 - 39 = NORMAL
            if (cheeseId >= 0 && cheeseId <= 39)
            {
                prefab = normalCheesePrefab;
                cheeseType = "Normal";
            }

            // 40 - 44 = ICE
            else if (cheeseId >= 40 && cheeseId <= 44)
            {
                prefab = iceCheesePrefab;
                cheeseType = "Ice";
            }

            // 45 - 49 = SPEED
            else
            {
                prefab = speedCheesePrefab;
                cheeseType = "Speed";
            }

            Vector3 spawnPosition;
            Quaternion spawnRotation;

            bool found =
                TryFindValidPosition(
                    prefab,
                    out spawnPosition,
                    out spawnRotation
                );

            if (!found)
            {
                Debug.LogWarning(
                    "CheeseSpawner: no se encontró una posición válida para el queso ID "
                    + cheeseId
                );

                continue;
            }

            SpawnCheese(
                prefab,
                spawnPosition,
                spawnRotation,
                cheeseId,
                cheeseType
            );
        }

        Debug.Log(
            "CheeseSpawner HOST: quesos creados = "
            + spawnedCheeses.Count
        );

        Debug.Log(
            "CheeseSpawner HOST: datos de red = "
            + spawnedNetworkCheeses.Count
        );

        return new List<NetworkCheeseData>(
            spawnedNetworkCheeses
        );
    }

    // =========================================================
    // COMPATIBILIDAD CON EL MÉTODO ANTERIOR
    // =========================================================

    public void SpawnInitialCheeses()
    {
        SpawnHostCheeses();
    }

    public int GetSpawnedCheeseCount()
    {
        return spawnedCheeses.Count;
    }

    public List<NetworkCheeseData> GetSpawnedNetworkCheeses()
    {
        return new List<NetworkCheeseData>(
            spawnedNetworkCheeses
        );
    }


    // =========================================================
    // CLIENTES: CREAR QUESOS RECIBIDOS
    // =========================================================

    public void ApplyNetworkCheeses(
        List<NetworkCheeseData> cheeseData)
    {
        PrepareSpawnArea();

        ClearCheeses();

        if (cheeseData == null ||
            cheeseData.Count == 0)
        {
            Debug.LogWarning(
                "CheeseSpawner: no se recibieron datos de quesos."
            );

            return;
        }

        foreach (NetworkCheeseData data in cheeseData)
        {
            GameObject prefab =
                GetPrefabForType(
                    data.cheeseType
                );

            Vector3 position =
                new Vector3(
                    data.posX,
                    data.posY,
                    data.posZ
                );

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    data.rotY,
                    0f
                );

            SpawnCheese(
                prefab,
                position,
                rotation,
                data.cheeseId,
                data.cheeseType
            );
        }

        Debug.Log(
            "CheeseSpawner CLIENTE: quesos creados = "
            + spawnedCheeses.Count
        );
    }


    // =========================================================
    // OBTENER PREFAB SEGÚN TIPO
    // =========================================================

    private GameObject GetPrefabForType(
        string cheeseType)
    {
        switch (cheeseType)
        {
            case "Normal":
                return normalCheesePrefab;

            case "Ice":
                return iceCheesePrefab;

            case "Speed":
                return speedCheesePrefab;

            default:
                return null;
        }
    }

    // =========================================================
    // BUSCAR POSICIÓN
    // =========================================================

    private bool TryFindValidPosition(
        GameObject prefab,
        out Vector3 validPosition,
        out Quaternion validRotation)
    {
        validPosition = Vector3.zero;
        validRotation = Quaternion.identity;

        for (
            int attempt = 0;
            attempt < maxAttemptsPerCheese;
            attempt++)
        {
            Vector3 candidate;

            if (!TryGetRandomPointOnSpawnArea(
                out candidate))
            {
                continue;
            }

            candidate.y = spawnY;

            if (IsTooCloseToAnotherCheese(
                candidate))
            {
                continue;
            }

            if (IsTooCloseToPlayer(
                candidate))
            {
                continue;
            }

            float randomY =
                Random.Range(
                    minimumRotationY,
                    maximumRotationY
                );

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    randomY,
                    0f
                );

            if (IsOverlappingWall(
                prefab,
                candidate,
                rotation))
            {
                continue;
            }

            validPosition = candidate;
            validRotation = rotation;

            return true;
        }

        return false;
    }

    // =========================================================
    // PUNTO ALEATORIO EN EL ÁREA
    // =========================================================

    private bool TryGetRandomPointOnSpawnArea(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        if (areaMeshFilters == null ||
            areaMeshFilters.Length == 0)
        {
            return false;
        }

        List<MeshFilter> validMeshes = new List<MeshFilter>();

        foreach (
            MeshFilter meshFilter
            in areaMeshFilters)
        {
            if (meshFilter == null)
                continue;

            if (meshFilter.sharedMesh == null)
                continue;

            validMeshes.Add(meshFilter);
        }

        if (validMeshes.Count == 0)
            return false;

        MeshFilter selectedMeshFilter =
            validMeshes[
                Random.Range(
                    0,
                    validMeshes.Count
                )
            ];

        Mesh mesh = selectedMeshFilter.sharedMesh;

        Vector3[] vertices = mesh.vertices;

        int[] triangles = mesh.triangles;

        if (triangles == null ||
            triangles.Length < 3)
        {
            return false;
        }

        for (
            int attempt = 0;
            attempt < 50;
            attempt++)
        {
            int triangleIndex =
                Random.Range(
                    0,
                    triangles.Length / 3
                );

            int indexA =
                triangles[
                    triangleIndex * 3
                ];

            int indexB =
                triangles[
                    triangleIndex * 3 + 1
                ];

            int indexC =
                triangles[
                    triangleIndex * 3 + 2
                ];

            Vector3 localA =
                vertices[indexA];

            Vector3 localB =
                vertices[indexB];

            Vector3 localC =
                vertices[indexC];

            Vector3 localNormal =
                Vector3.Cross(
                    localB - localA,
                    localC - localA
                ).normalized;

            if (localNormal.y < 0.5f)
                continue;

            float r1 =
                Random.Range(0f, 1f);

            float r2 =
                Random.Range(0f, 1f);

            if (r1 + r2 > 1f)
            {
                r1 = 1f - r1;
                r2 = 1f - r2;
            }

            Vector3 localPoint =
                localA
                + (localB - localA) * r1
                + (localC - localA) * r2;

            worldPoint =
                selectedMeshFilter.transform
                .TransformPoint(localPoint);

            return true;
        }

        return false;
    }

    // =========================================================
    // COLLIDER
    // =========================================================

    private BoxCollider GetCheeseCollider(
        GameObject prefab)
    {
        if (prefab == null)
            return null;

        return prefab.GetComponentInChildren<BoxCollider>();
    }

    // =========================================================
    // PAREDES
    // =========================================================

    private bool IsOverlappingWall(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation)
    {
        BoxCollider collider =
            GetCheeseCollider(prefab);

        Vector3 worldScale =
            Vector3.Scale(
                collider.transform.lossyScale,
                collider.size
            );

        Vector3 halfExtents =
            worldScale * 0.5f;

        halfExtents +=
            Vector3.one * wallClearance;

        Vector3 colliderCenter =
            position
            + Vector3.Scale(
                collider.center,
                collider.transform.lossyScale
            );

        Collider[] walls =
            Physics.OverlapBox(
                colliderCenter,
                halfExtents,
                rotation,
                wallLayer,
                QueryTriggerInteraction.Ignore
            );

        return walls.Length > 0;
    }

    // =========================================================
    // DISTANCIA ENTRE QUESOS
    // =========================================================

    private bool IsTooCloseToAnotherCheese(
        Vector3 position)
    {
        foreach (
            Vector3 cheesePosition
            in cheesePositions)
        {
            float distance =
                Vector2.Distance(
                    new Vector2(
                        position.x,
                        position.z
                    ),
                    new Vector2(
                        cheesePosition.x,
                        cheesePosition.z
                    )
                );

            if (distance < minimumDistance)
                return true;
        }

        return false;
    }

    // =========================================================
    // DISTANCIA A JUGADORES
    // =========================================================

    private bool IsTooCloseToPlayer(Vector3 position)
    {
        Collider[] playerColliders =
            Physics.OverlapSphere(
                position,
                playerClearRadius,
                playerLayer,
                QueryTriggerInteraction.Ignore
            );

        return playerColliders.Length > 0;
    }

    // =========================================================
    // CREAR QUESO
    // =========================================================

    private void SpawnCheese(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        int cheeseId,
        string cheeseType)
    {
        GameObject cheese =
            Instantiate(
                prefab,
                position,
                rotation
            );

        // =====================================================
        // IDENTIDAD DE RED
        // =====================================================

        NetworkCheeseIdentity identity = cheese.GetComponent<NetworkCheeseIdentity>();

        if (identity == null)
        {
            identity = cheese.AddComponent<NetworkCheeseIdentity>();
        }

        identity.SetIdentity(
            cheeseId,
            cheeseType
        );

        CheesePickup pickup = cheese.GetComponent<CheesePickup>();

        if (pickup == null)
        {
            cheese.AddComponent<CheesePickup>();
        }

        // =====================================================
        // GUARDAR QUESO
        // =====================================================

        spawnedCheeses.Add(cheese);

        cheesePositions.Add(position);

        NetworkCheeseData data = new NetworkCheeseData
            {
                cheeseId = cheeseId,
                cheeseType = cheeseType,

                posX = position.x,
                posY = position.y,
                posZ = position.z,

                rotY = rotation.eulerAngles.y
            };

        spawnedNetworkCheeses.Add(data);
    }

    // =========================================================
    // ELIMINAR QUESO POR ID
    // =========================================================

    public bool RemoveCheeseById(int cheeseId)
    {
        for (int i = 0; i < spawnedCheeses.Count; i++)
        {
            GameObject cheese =
                spawnedCheeses[i];

            if (cheese == null)
                continue;

            NetworkCheeseIdentity identity =
                cheese.GetComponent<NetworkCheeseIdentity>();

            if (identity == null)
                continue;

            if (identity.CheeseId != cheeseId)
                continue;

            // Eliminar objeto de Unity
            Destroy(cheese);

            // Eliminar de las listas
            spawnedCheeses.RemoveAt(i);

            if (i < cheesePositions.Count)
            {
                cheesePositions.RemoveAt(i);
            }

            for (int j = 0; j < spawnedNetworkCheeses.Count; j++)
            {
                if (spawnedNetworkCheeses[j].cheeseId == cheeseId)
                {
                    spawnedNetworkCheeses.RemoveAt(j);
                    break;
                }
            }

            Debug.Log(
                "CheeseSpawner: queso eliminado. ID = "
                + cheeseId
            );

            return true;
        }

        Debug.LogWarning(
            "CheeseSpawner: no se encontró el queso ID "
            + cheeseId
        );

        return false;
    }

    // =========================================================
    // CREAR REEMPLAZO
    // =========================================================

    public NetworkCheeseData SpawnReplacementCheese(
        int cheeseId,
        string cheeseType)
    {
        GameObject prefab =
            GetPrefabForType(
                cheeseType
            );

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        bool found =
            TryFindValidPosition(
                prefab,
                out spawnPosition,
                out spawnRotation
            );

        if (!found)
        {
            Debug.LogError(
                "CheeseSpawner: no se encontró posición para reemplazo ID "
                + cheeseId
            );

            return null;
        }

        SpawnCheese(
            prefab,
            spawnPosition,
            spawnRotation,
            cheeseId,
            cheeseType
        );

        return spawnedNetworkCheeses[
            spawnedNetworkCheeses.Count - 1
        ];
    }

    // =========================================================
    // CLIENTE: CREAR UN SOLO QUESO DE RED
    // =========================================================

    public void ApplySingleNetworkCheese(
        NetworkCheeseData data)
    {
        if (data == null)
            return;

        GameObject prefab =
            GetPrefabForType(
                data.cheeseType
            );

        // Por seguridad, eliminar primero
        // cualquier objeto que todavía tenga este ID.
        RemoveCheeseById(
            data.cheeseId
        );

        Vector3 position =
            new Vector3(
                data.posX,
                data.posY,
                data.posZ
            );

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                data.rotY,
                0f
            );

        SpawnCheese(
            prefab,
            position,
            rotation,
            data.cheeseId,
            data.cheeseType
        );

        Debug.Log(
            "CheeseSpawner: reemplazo creado. ID = "
            + data.cheeseId
            + " | Tipo = "
            + data.cheeseType
        );
    }


    // =========================================================
    // LIMPIAR
    // =========================================================

    public void ClearCheeses()
    {
        foreach (
            GameObject cheese
            in spawnedCheeses)
        {
            if (cheese != null)
            {
                Destroy(cheese);
            }
        }

        spawnedCheeses.Clear();
        cheesePositions.Clear();
        spawnedNetworkCheeses.Clear();
    }
}