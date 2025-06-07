using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCControl : MonoBehaviour
{
    public float speed = 2f;
    public float aroundAgentRange = 5f;
    public bool correctAgent = false;
    public bool drawDebugRange = false;

    private TargetArea[] targetAreas;
    private GameObject[] waypoints;
    private UnityEngine.AI.NavMeshAgent navAgent;
    private PassenagerAgent mainAgent;
    private float time;
    private float forwardUnit = 2f;
    private float offsetSize = 3f;

    private CheckRay checkRay;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    public void EpisodeInit()
    {
        waypoints = GameObject.FindGameObjectsWithTag("CGV_NPC_WayPoint");
        targetAreas = FindObjectsOfType<TargetArea>();
        mainAgent = FindObjectOfType<PassenagerAgent>();
        time = 0;
        navAgent.speed = speed;

        if (mainAgent.addNPCInfo)
        {
            checkRay = FindObjectOfType<CheckRay>();
        }

        UpdateTarget();
    }

    void FixedUpdate()
    {
        if (navAgent.pathPending) return;
        if (targetAreas == null) return;

        time += Time.fixedDeltaTime;

        foreach (var waypoint in waypoints)
        {
            if (Vector3.Distance(transform.position, waypoint.transform.position) < 2f)
            {
                Debug.Log("NPC " + gameObject.name + " reached target area: " + waypoint.name);
                gameObject.SetActive(false);
                return;
            }
        }

        if (navAgent.remainingDistance < 1f || time >= 1f)
        {
            UpdateTarget();
            time = 0;
        }

        if (drawDebugRange) DrawDebugNPCRange();
    }

    public void SetInitLocation(Vector3 position, Quaternion rotation)
    {
        navAgent.Warp(position);
        transform.rotation = rotation;
    }

    void UpdateTarget()
    {
        Vector3 targetPosition = mainAgent.transform.position + mainAgent.transform.forward * forwardUnit;

        if (Mathf.Abs(targetPosition.x - transform.position.x) > aroundAgentRange ||
            Mathf.Abs(targetPosition.z - transform.position.z) > aroundAgentRange)
        {
            Vector3 pos = targetPosition;

            float rnd_x = Random.Range(-aroundAgentRange + offsetSize, aroundAgentRange - offsetSize), rnd_z = Random.Range(-aroundAgentRange + offsetSize, aroundAgentRange - offsetSize);

            pos.x += rnd_x + Mathf.Sign(rnd_x) * offsetSize;
            pos.z += rnd_z + Mathf.Sign(rnd_z) * offsetSize;

            SetInitLocation(pos, Quaternion.Euler(0, Random.Range(-180, 180), 0));

            if (checkRay != null && checkRay.enabled && checkRay.npcLastPositions.ContainsKey(gameObject))
            {
                checkRay.npcLastPositions.Remove(gameObject);
            }
        }

        if (correctAgent)
        {
            CalTargetPoint();
        }
        else
        {
            WalkAroundAgent();
        }
    }

    void WalkAroundAgent()
    {
        Vector3 targetPosition = mainAgent.transform.position + mainAgent.transform.forward * forwardUnit;

        targetPosition.x += Random.Range(-aroundAgentRange, aroundAgentRange);
        targetPosition.z += Random.Range(-aroundAgentRange, aroundAgentRange);

        navAgent.SetDestination(targetPosition);
    }

    void CalTargetPoint()
    {
        GameObject target = waypoints[Random.Range(0, waypoints.Length)];

        float distanceMin = 1000f;
        for (int i = 0; i < waypoints.Length; ++i)
        {
            float d = Vector3.Distance(waypoints[i].transform.position, transform.position);
            if (d < distanceMin)
            {
                distanceMin = d;
                target = waypoints[i];
            }
        }

        navAgent.SetDestination(target.transform.position);
    }

    void DrawDebugNPCRange()
    {
        Vector3 center = mainAgent.transform.position + mainAgent.transform.forward * forwardUnit;
        Vector3 v0 = center + new Vector3(aroundAgentRange, 0, aroundAgentRange);
        Vector3 v1 = center + new Vector3(aroundAgentRange, 0, -aroundAgentRange);
        Vector3 v2 = center + new Vector3(-aroundAgentRange, 0, -aroundAgentRange);
        Vector3 v3 = center + new Vector3(-aroundAgentRange, 0, aroundAgentRange);

        float duration = 0.2f;

        Debug.DrawLine(v0, v1, Color.blue, duration);
        Debug.DrawLine(v1, v2, Color.blue, duration);
        Debug.DrawLine(v2, v3, Color.blue, duration);
        Debug.DrawLine(v3, v0, Color.blue, duration);

        v0 = center + new Vector3(offsetSize, 0, offsetSize);
        v1 = center + new Vector3(offsetSize, 0, -offsetSize);
        v2 = center + new Vector3(-offsetSize, 0, -offsetSize);
        v3 = center + new Vector3(-offsetSize, 0, offsetSize);

        Debug.DrawLine(v0, v1, Color.red, duration);
        Debug.DrawLine(v1, v2, Color.red, duration);
        Debug.DrawLine(v2, v3, Color.red, duration);
        Debug.DrawLine(v3, v0, Color.red, duration);
    }
}