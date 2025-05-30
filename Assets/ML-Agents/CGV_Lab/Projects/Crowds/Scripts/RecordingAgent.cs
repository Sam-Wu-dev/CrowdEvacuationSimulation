using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class RecordingAgent : Agent
{
    public int recordPeriod;

    private int step;
    
    public override void Initialize()
    {
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (step == 0) 
        {
            int agentNum = GetAgentNum();
            SetReward(agentNum);
            if (agentNum == 0) EndEpisode();
        }

        step++;
        step %= recordPeriod;
    }

    public int GetAgentNum()
    {
        PassenagerAgent[] agents = FindObjectsOfType<PassenagerAgent>();
        
        return agents.GetLength(0);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
 
    }

    public override void OnEpisodeBegin()
    {

    }
}
