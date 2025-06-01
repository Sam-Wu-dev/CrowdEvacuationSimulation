using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class CheckRay : MonoBehaviour
{
    private FailAgent failAgent;
    private PassenagerAgent passenagerAgent;
    private int step = 0;
    public float checkFailRayHitDis = 1f;
    private int agentNum = 0;
    private int startStep = 0;
    private int lastStep = 0;
    public int lastStepThreshold = 250;
    public float penalty = -0.1f;

    private const int MaxNPCCount = 15;
    public Dictionary<GameObject, Vector3> npcLastPositions = new Dictionary<GameObject, Vector3>();
    private Vector3 lastPosition;
    public float distanceTreshold = 1f;
    private List<Vector3> trajectory = new List<Vector3>();
    private int trajectorySize = 50;
    public int lastAction = 5;

    // Start is called before the first frame update
    void Start()
    {
        EpisodeInit();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (failAgent != null && failAgent.enabled && failAgent.save)
        {
            if (CheckRayCast())
            {
                bool exists = failAgent.agentInfos.Any(tuple => tuple.Item1 == agentNum);
                if (lastStep > lastStepThreshold && !exists)
                {
                    int endStep = startStep + lastStep;
                    failAgent.agentInfos.Add(new Tuple<int, int, int>(agentNum, startStep, endStep));

                    //Debug.Log("stuck step : " + step);
                    //Debug.Log("stuck transform.position : " + transform.position);
                }

                if (lastStep == 0) startStep = step; // agent start to stop
                lastStep++;
            }
            else
            {
                lastStep = 0;
            }
        }
        //if (failAgent != null && failAgent.enabled && failAgent.save)
        //{
        //    bool exists = failAgent.agentInfos.Any(tuple => tuple.Item1 == agentNum);
        //    if (lastStep > lastStepThreshold && !exists)
        //    {
        //        int endStep = startStep + lastStep;
        //        failAgent.agentInfos.Add(new Tuple<int, int, int>(agentNum, startStep, endStep));

        //        //Debug.Log("stuck step : " + step);
        //        //Debug.Log("stuck transform.position : " + transform.position);
        //    }

        //    float moveDistance = Vector3.Distance(trajectory[trajectorySize - 1], trajectory[trajectorySize - lastAction - 1]);
        //    //Debug.Log($"moveDistance: {moveDistance}");
        //    if (moveDistance < distanceTreshold && CheckRayCast())
        //    {
        //        if (lastStep == 0) startStep = step; // agent start to stop
        //        lastStep++;
        //    }
        //    else
        //    {
        //        lastStep = 0;
        //    }

        //    if (step % 20 == 0)
        //    {
        //        for (int i = 0; i < trajectorySize - 1; ++i)
        //        {
        //            trajectory[i] = trajectory[i + 1];
        //        }
        //        trajectory[trajectorySize - 1] = transform.position;
        //    }
        //}

        if (failAgent != null && failAgent.enabled && failAgent.checkRay)
        {
            if (CheckRayCast())
            {
                if (lastStep > lastStepThreshold)
                {
                    passenagerAgent.AddPenalty(penalty);
                }

                if (lastStep == 0) startStep = step; // agent start to stop
                lastStep++;
            }
            else
            {
                lastStep = 0;
            }
            //Debug.Log("lastStep : " + lastStep);
        }
        //Debug.Log("step : " + step);
        //Debug.Log("transform.position : " + transform.position);

        step++;
    }

    // Name patten "Agent (1)"
    private int ParseName()
    {
        string name = gameObject.name;
        string[] nameSplit = name.Split(' ');
        int res = 0;

        if (nameSplit.Length == 2)
        {
            string[] split1 = nameSplit[1].Split('(');
            string[] split2 = split1[1].Split(')');
            res = int.Parse(split2[0]);
        }
        return res;
    }

    public bool CheckRayCast()
    {
        RayPerceptionSensorComponent3D raySensor = GetComponent<RayPerceptionSensorComponent3D>();

        RayPerceptionOutput.RayOutput[] rayOutputs = RayPerceptionSensor.Perceive(raySensor.GetRayPerceptionInput()).RayOutputs;
        int lengthOfRayOutputs = rayOutputs.Length;
        //Debug.Log($"{lengthOfRayOutputs}");

        // Alternating Ray Order: it gives an order of
        // (0, -delta, delta, -2delta, 2delta, ..., -ndelta, ndelta)
        // index 0 indicates the center of raycasts
        float minRayHitDistance = 999f;
        string hitTag = "";
        for (int i = 0; i < lengthOfRayOutputs; i++)
        {
            GameObject goHit = rayOutputs[i].HitGameObject;
            if (goHit != null)
            {
                Vector3 rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                float scaledRayLength = rayDirection.magnitude;
                float rayHitDistance = rayOutputs[i].HitFraction * scaledRayLength;

                if (rayHitDistance < minRayHitDistance)
                {
                    minRayHitDistance = rayHitDistance;
                    hitTag = goHit.tag;
                }
                //Debug.Log($"name: {goHit.name}");
            }
        }

        if ((minRayHitDistance < checkFailRayHitDis) && (hitTag == "CGV_Building" || hitTag == "Untagged"))
        {
            //Debug.Log($"step: {step}, minRayHitDistance");
            return true;
        }
        return false;
    }

    public Vector3[] GetNPCMovementVectors()
    {
        List<Vector3> movementList = new List<Vector3>();
        HashSet<GameObject> addedNPCs = new HashSet<GameObject>();

        RayPerceptionSensorComponent3D raySensor = GetComponent<RayPerceptionSensorComponent3D>();
        RayPerceptionOutput.RayOutput[] rayOutputs = RayPerceptionSensor.Perceive(raySensor.GetRayPerceptionInput()).RayOutputs;

        foreach (var ray in rayOutputs)
        {
            GameObject goHit = ray.HitGameObject;
            if (goHit != null && goHit.CompareTag("CGV_Crowd") && !addedNPCs.Contains(goHit))
            {
                Vector3 movement = Vector3.zero;
                Vector3 currentPos = goHit.transform.position;

                if (npcLastPositions.TryGetValue(goHit, out Vector3 lastPos))
                {
                    movement = currentPos - lastPos;
                }

                npcLastPositions[goHit] = currentPos;

                if (movementList.Count < MaxNPCCount)
                {
                    movementList.Add(movement);
                }
                addedNPCs.Add(goHit);

                //if (movement.magnitude > 2) Debug.Log($"NPC Movement Magnitude: {movement.magnitude}");
            }
        }

        // Pad with zero vectors if needed
        while (movementList.Count < MaxNPCCount)
        {
            movementList.Add(Vector3.zero);
        }

        return movementList.ToArray();
    }

    public void EpisodeInit()
    {
        failAgent = GetComponentInParent<FailAgent>();
        passenagerAgent = GetComponent<PassenagerAgent>();
        step = 0;
        startStep = 0;
        lastStep = 0;
        agentNum = ParseName();
        npcLastPositions.Clear();
        lastPosition = transform.position;
        trajectory.Clear();
        for (int i = 0; i < trajectorySize; ++i)
        {
            trajectory.Add(transform.position);
        }
    }
}
