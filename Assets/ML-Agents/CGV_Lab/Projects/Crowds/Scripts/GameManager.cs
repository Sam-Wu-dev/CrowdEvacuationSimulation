using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public GameObject initialPositionList;

    public int fps = 16;
    public int maxStopMinute = 2;
    public int maxStopSecond = 0;

    private GameObject[] npcs;

    private const string prefabRootPath = "Prefabs";
    private const string propertyShininess = "_Shininess";

    private string[] prefabPaths;
    private string sceneName;
    private float shininess;

    private int currentFrame = 1;
    private int positionInObstacleCount = 0; // Count positions in obstacles after max attempts.

    private void Start()
    {
        //Time.timeScale = 0.5f;

        npcs = GameObject.FindGameObjectsWithTag("CGV_Crowd");
        //foreach (GameObject npc in npcs)
        //{
        //    if (npc.name != "NPC")
        //    {
        //        npc.SetActive(false);
        //        npc.GetComponent<NPCController>().SetActive(false);
        //    }
        //}
        NavMesh.pathfindingIterationsPerFrame = 1000;

        // Avoid tunnel effect.
        Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Only goal markers are triggers.
        Collider[] colliders = FindObjectsOfType<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.gameObject.CompareTag("CGV_Goal"))
            {
                col.isTrigger = true;
            }
            else
            {
                col.isTrigger = false;
            }
        }

        // Disable collision detection of markers.
        GameObject[] directionMarkers = GameObject.FindGameObjectsWithTag("CGV_Direction");
        foreach (GameObject obj in directionMarkers)
        {
            DisableCollider(obj);
            DisableChildColliders(obj.transform);
        }

        sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Mall"))
        {
            shininess = 0.5f;
        }
        else
        {
            shininess = 1.8f;
        }

        // Get all prefab paths.
        string prefabDir = Path.Combine(Application.dataPath, "Resources", prefabRootPath);
        if (Directory.Exists(prefabDir))
        {
            string[] paths = Directory.GetFiles(prefabDir, "*.prefab", SearchOption.TopDirectoryOnly)
                             .Where(file => !Path.GetFileName(file).StartsWith(".")) // Exclude hidden files.
                             .ToArray();
            string[] excludeKeywords = {
                "apron", "bathtowel", "blazer_100", "naked", "schoolwear_100",
                "schoolwear_200", "schoolwear_300", "schoolwear_420", "swimwear" };
            paths = paths
                .Where(file =>
                    !excludeKeywords.Any(keyword => Path.GetFileName(file).ToLowerInvariant().Contains(keyword))
                )
                .ToArray();

            prefabPaths = new string[paths.Length];
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                prefabPaths[i] = Path.GetFileNameWithoutExtension(paths[i]);
            }
        }

        SetPositions();

        if (positionInObstacleCount != 0)
        {
            Debug.Log($"[WARN]: Found {positionInObstacleCount} NPC(s) in obstacle...");
        }
    }

    private void SetPositions()
    {
        int countX = (int) Math.Ceiling(Math.Sqrt(npcs.Length));
        int countZ = countX;

        Vector3[] positions = new Vector3[0];

        if (sceneName.StartsWith("Hall"))
        {
            positions = SampleRandomPositions(Vector2.zero, new Vector2(-43, 0), new Vector2(0, 43), countX, countZ);
        }
        else if (sceneName.StartsWith("Mall"))
        {
            positions = SampleRandomPositions(new Vector2(-44, -14), new Vector2(0, 86), new Vector2(0, 26), countX, countZ);
        }
        else if (sceneName.StartsWith("Campus"))
        {
            int countX1 = (int)Math.Ceiling(Math.Sqrt(npcs.Length / 2));
            int countZ1 = countX1;

            Vector3[] positions1 = SampleRandomPositions(new Vector2(-32, -19), new Vector2(0, 64), new Vector2(0, 33), countX1, countZ1);
            Vector3[] positions2 = SampleRandomPositions(new Vector2(-46, -64), new Vector2(0, 79), new Vector2(0, 41), countX1, countZ1);

            positions = positions1.Concat(positions2).ToArray();
        }
        else if (sceneName.StartsWith("Classroom"))
        {
            positions = new Vector3[npcs.Length];
            for (int i = 0; i < npcs.Length; i++)
            {
                bool check = false;
                int sampleCount = 0, sampleMax = 100, idx = 0;
                float collisionRadius = 0.35f;

                Vector3 position = initialPositionList.transform.GetChild(idx).position;
                do
                {
                    sampleCount++;
                    if (sampleCount > sampleMax)
                    {
                        break;
                    }

                    idx = UnityEngine.Random.Range(0, initialPositionList.transform.childCount);
                    position = initialPositionList.transform.GetChild(idx).position;

                    Collider[] hitColliders = Physics.OverlapSphere(position, collisionRadius);
                    for (int j = 0; j < hitColliders.Length; ++j)
                    {
                        if (hitColliders[j].tag != "CGV_Crowd" && hitColliders[j].tag != "CGV_Ground" &&
                            hitColliders[j].tag != "CGV_Expert" && hitColliders[j].tag != "CGV_Range")
                        {
                            check = true;
                            break;
                        }
                    }
                } while (check);

                positions[i] = position;
            }
        }

        for (int i = 0; i < npcs.Length; i++)
        {
            NavMeshAgent agent = npcs[i].GetComponent<NavMeshAgent>();
            SetAgentOutfit(agent);
            agent.Warp(positions[i]);
        }
    }

    //private Vector3[] SampleUniformPositions(
    //    Vector2 center, Vector2 xRange, Vector2 zRange, int countX, int countZ)
    //{
    //    Vector3[] positions = new Vector3[countX * countZ];

    //    float stepX = (xRange.y - xRange.x) / (countX - 1);
    //    float stepZ = (zRange.y - zRange.x) / (countZ - 1);

    //    int index = 0;
    //    for (int i = 0; i < countX; i++)
    //    {
    //        for (int j = 0; j < countZ; j++)
    //        {
    //            float x = center.x + xRange.x + i * stepX;
    //            float z = center.y + zRange.x + j * stepZ;

    //            Vector3 position = new Vector3(x, 0.05f, z);

    //            if (IsPositionInObstacle(position))
    //            {
    //                position = FindNearbyPosition(position, center, xRange, zRange);
    //            }

    //            positions[index++] = position;
    //        }
    //    }

    //    return positions;
    //}

    private Vector3[] SampleRandomPositions(
        Vector2 center, Vector2 xRange, Vector2 zRange, int countX, int countZ)
    {
        Vector3[] positions = new Vector3[countX * countZ];
        System.Random random = new System.Random();

        int index = 0;
        for (int i = 0; i < countX; i++)
        {
            for (int j = 0; j < countZ; j++)
            {
                float x = center.x + xRange.x + (float)random.NextDouble() * (xRange.y - xRange.x);
                float z = center.y + zRange.x + (float)random.NextDouble() * (zRange.y - zRange.x);

                Vector3 position = new Vector3(x, 0.05f, z);

                if (IsPositionInObstacle(position))
                {
                    position = FindNearbyPosition(position, center, xRange, zRange);
                }

                positions[index++] = position;
            }
        }

        return positions;
    }

    private Vector3 FindNearbyPosition(
        Vector3 originalPosition, Vector2 center, Vector2 xRange, Vector2 zRange)
    {
        float searchRadius = 10f;
        int maxAttempts = 50;

        float minX = center.x + xRange.x;
        float maxX = center.x + xRange.y;
        float minZ = center.y + zRange.x;
        float maxZ = center.y + zRange.y;

        Vector3 newPosition = originalPosition;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float offsetX = UnityEngine.Random.Range(-searchRadius, searchRadius);
            float offsetZ = UnityEngine.Random.Range(-searchRadius, searchRadius);
            newPosition = new Vector3(originalPosition.x + offsetX, originalPosition.y, originalPosition.z + offsetZ);

            if (newPosition.x >= minX && newPosition.x <= maxX && // Make sure new position is in range and not in obstacles.
                newPosition.z >= minZ && newPosition.z <= maxZ &&
                !IsPositionInObstacle(newPosition))
            {
                return newPosition;
            }
        }

        positionInObstacleCount++;
        return originalPosition;
    }

    private bool IsPositionInObstacle(Vector3 position)
    {
        float collisionRadius = 0.35f;

        Collider[] hitColliders = Physics.OverlapSphere(position, collisionRadius);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].tag != "CGV_Crowd" && hitColliders[i].tag != "CGV_Ground" &&
                hitColliders[i].tag != "CGV_Expert" && hitColliders[i].tag != "CGV_Range" &&
                hitColliders[i].tag != "CGV_tmp_l" && hitColliders[i].tag != "CGV_tmp_r" &&
                hitColliders[i].tag != "CGV_tmp_u" && hitColliders[i].tag != "CGV_tmp_d")
            {
                return true;
            }
        }

        return false;
    }

    private void SetAgentOutfit(NavMeshAgent agent)
    {
        Transform skinnedMeshTransform = agent.transform.Find(
            Path.Combine("m01_schoolwear_000_l/m01/m01_schoolwear_000_l"));
        SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshTransform.GetComponent<SkinnedMeshRenderer>();

        string randomPrefabPath = prefabPaths[UnityEngine.Random.Range(0, prefabPaths.Length)];
        GameObject loadedModel = Resources.Load<GameObject>($"{prefabRootPath}/{randomPrefabPath}");
        Transform loadedModelTransform = loadedModel.transform.Find($"{randomPrefabPath}/m01/{randomPrefabPath}");

        SkinnedMeshRenderer loadedMeshRenderer = loadedModelTransform.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = loadedMeshRenderer.sharedMesh;

        Material[] newMaterials = new Material[loadedMeshRenderer.sharedMaterials.Length];
        for (int i = 0; i < newMaterials.Length; i++)
        {
            Material orgMaterial = loadedMeshRenderer.sharedMaterials[i];
            Material newMaterial = new Material(orgMaterial);

            if (newMaterial.HasProperty(propertyShininess))
            {
                newMaterial.SetFloat(propertyShininess, shininess);
            }
            else
            {
                Debug.LogError($"Property {propertyShininess} not found.");
            }

            newMaterials[i] = newMaterial;
        }

        skinnedMeshRenderer.materials = newMaterials;
    }

    private void Update()
    {
        currentFrame += 1;

        if (AreAllNPCsInactive() || (currentFrame > fps * (maxStopMinute * 60 + maxStopSecond)))
        {
            QuitApplication();
        }
    }

    private void DisableCollider(GameObject obj)
    {
        Collider[] colliders = obj.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void DisableChildColliders(Transform parent)
    {
        foreach (Transform child in parent)
        {
            DisableCollider(child.gameObject);
            DisableChildColliders(child);
        }
    }

    private bool AreAllNPCsInactive()
    {
        foreach (GameObject npc in npcs)
        {
            if (npc.GetComponent<NPCController>().GetActive())
            {
                return false;
            }
        }

        return true;
    }

    private void QuitApplication()
    {
    #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}