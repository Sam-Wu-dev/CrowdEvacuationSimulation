using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AgentController : MonoBehaviour
{
    private Rigidbody rigid;
    private Animator animator;

    private string[] prefabPaths;
    private string prefabRootPath = "Prefabs";

    private float rotationSpeed = 2f;
    private float linearSpeed = 0;
    private Vector3 targetDirection = Vector3.zero;
    private float timeAnimUpdate = 0.1f;
    private float timeAmount = 0;
    private Vector3 lastPosition = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero;
    private bool forceAdd = false;
    private TargetArea[] targetAreas;
    private float shininess;

    //private DebugGizmos debugGizmos;

    private float detectionDistance = 0.45f; // Adjust based on your agent's size
    private float detectionRadius = 0.01f;  // Width of detection

    void Start()
    {
        shininess = SceneManager.GetActiveScene().name == "Shop" ? 0.5f : 1.8f;

        rigid = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        rigid.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

        string prefabDir = Path.Combine(Application.dataPath, "Resources", prefabRootPath).Replace("\\", "/");
        if (Directory.Exists(prefabDir))
        {
            string[] paths = Directory.GetFiles(prefabDir, "*.prefab", SearchOption.TopDirectoryOnly);
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

        Transform skinnedMeshTransform = transform.Find(
            Path.Combine("m01_schoolwear_000_l", "m01", "m01_schoolwear_000_l").Replace("\\", "/"));
        if (skinnedMeshTransform != null)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshTransform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                int seed = GetInstanceID() + System.DateTime.Now.Millisecond;
                System.Random sysRandom = new System.Random(seed);
                string randomPrefabPath = prefabPaths[sysRandom.Next(prefabPaths.Length)];

                GameObject loadedModel = Resources.Load<GameObject>(Path.Combine(prefabRootPath, randomPrefabPath).Replace("\\", "/"));
                Transform loadedModelTransform = loadedModel.transform.Find(Path.Combine(randomPrefabPath, "m01", randomPrefabPath).Replace("\\", "/"));

                if (loadedModelTransform != null)
                {
                    SkinnedMeshRenderer loadedMeshRenderer = loadedModelTransform.GetComponent<SkinnedMeshRenderer>();
                    if (loadedMeshRenderer != null)
                    {
                        skinnedMeshRenderer.sharedMesh = loadedMeshRenderer.sharedMesh;

                        Material[] newMaterials = new Material[loadedMeshRenderer.sharedMaterials.Length];
                        for (int i = 0; i < newMaterials.Length; i++)
                        {
                            Material orgMaterial = loadedMeshRenderer.sharedMaterials[i];
                            Material newMaterial = new Material(orgMaterial);

                            if (newMaterial.HasProperty("_Shininess"))
                            {
                                newMaterial.SetFloat("_Shininess", shininess);
                            }
                            else
                            {
                                Debug.LogError("Property _Shininess not found.");
                            }

                            newMaterials[i] = newMaterial;
                        }

                        skinnedMeshRenderer.materials = newMaterials;
                    }
                    else
                    {
                        Debug.LogError("SkinnedMeshRenderer component of loaded model not found on the target object.");
                    }
                }
                else
                {
                    Debug.LogError("Transform component of loaded model not found on the target object.");
                }
            }
            else
            {
                Debug.LogError("SkinnedMeshRenderer component of original model not found on the target object.");
            }
        }
        else
        {
            Debug.LogError("Target GameObject not found in the hierarchy.");
        }
    }

    public void EpisodeInit()
    {
        targetAreas = FindObjectsOfType<TargetArea>();

        //* wang
        //debugGizmos.EpisodeInit();
        targetDirection = Vector3.zero;
        linearSpeed = 0;
        timeAmount = 0;
        lastPosition = transform.position;
        currentVelocity = Vector3.zero;
        forceAdd = false;
    }

    public void Move(Vector3 moveVector, float moveSpeed)
    {
        targetDirection = Quaternion.Euler(0, moveVector.x * 90, 0) * transform.rotation * Vector3.forward;

        linearSpeed = Mathf.Abs(moveVector.z) * moveSpeed;
    }


    public void FixedUpdate()
    {
        if (targetAreas != null) CloseTarget();

        //if (CheckFrontAgent())
        //{
        //    rigid.velocity = Vector3.zero;
        //}
        //else
        //{
        //    rigid.velocity = targetDirection * linearSpeed;
        //}

        rigid.velocity = targetDirection * linearSpeed;
        currentVelocity = targetDirection * linearSpeed;

        UpdateAnim();

        if (targetDirection.magnitude == 0) return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        //Quaternion rotation = Quaternion.RotateTowards(rigid.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime * 20);
        Quaternion rotation = Quaternion.Lerp(rigid.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        rigid.MoveRotation(rotation);
        forceAdd = false;

        //* wang
        //debugGizmos.UpdateCircle();
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "CGV_Ground" || collision.gameObject == gameObject || forceAdd) return;

        float forceMax = 1f;
        Vector3 cur = currentVelocity;
        Vector3 n = collision.contacts[0].normal;

        cur.y = 0; n.y = 0;
        cur = cur.normalized; n = n.normalized;

        if (Vector3.Angle(-cur.normalized, n.normalized) > 80) return;
        else if (Vector3.Angle(-cur.normalized, n.normalized) > 70) forceMax = 0.6f;
        if (collision.gameObject.tag == "CGV_Expert")
        {
            forceMax = 0.25f;
        }

        float weight = 10f;
        Vector3 reflectVelocity = Vector3.Reflect(cur, n).normalized;
        Vector3 force = (cur + reflectVelocity) / 2 * weight;
        force = force.normalized * Mathf.Min(force.magnitude, forceMax);

        rigid.MovePosition(transform.position + force * Time.fixedDeltaTime);

        forceAdd = true;
    }

    private void CloseTarget()
    {
        foreach (TargetArea targetArea in targetAreas)
        {
            float distance = Vector3.Distance(transform.position, targetArea.transform.position);

            if (distance < 3f)
            {
                Vector3 d = (targetArea.transform.position) - transform.position;
                d.y = 0;
                targetDirection = Vector3.Lerp(targetDirection, d.normalized, 1 / (distance + 1e-4f) * 0.5f);
            }
        }
    }

    private void UpdateAnim()
    {
        timeAmount += Time.fixedDeltaTime;

        if (timeAmount >= timeAnimUpdate)
        {
            timeAmount = 0;

            float moveDistance = Vector3.Distance(lastPosition, transform.position);

            if (moveDistance < 0.01f)
            {
                animator.SetBool("Walking", false);
            }
            else
            {
                animator.SetBool("Walking", true);
                animator.SetFloat("Speed", moveDistance * 10f);
            }

            lastPosition = transform.position;
        }
    }

    public bool CheckFrontAgent()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        float halfAngle = 7.5f;
        int numRays = 5;


        for (int i = 0; i < numRays; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / (numRays - 1));
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            if (Physics.SphereCast(origin, detectionRadius, direction, out hit, detectionDistance))
            {
                if (hit.collider.CompareTag("CGV_Expert"))
                {

                    return true;
                }
            }
        }

        return false;
    }
}
