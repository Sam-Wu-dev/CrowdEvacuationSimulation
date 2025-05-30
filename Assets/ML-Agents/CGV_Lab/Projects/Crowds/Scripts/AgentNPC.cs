using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentNPC : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent navAgent;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    public void EpisodeInit()
    {
        TargetArea[] targetAreas = FindObjectsOfType<TargetArea>();
        Vector3 targetPosition = targetAreas[0].transform.position;
        float dis = 1000000;

        foreach (TargetArea targetArea in targetAreas)
        {
            if (Vector3.Distance(targetArea.transform.position, transform.position) < dis)
            {
                dis = Vector3.Distance(targetArea.transform.position, transform.position);
                targetPosition = targetArea.transform.position;
            }
        }

        navAgent.SetDestination(targetPosition);
    }
}