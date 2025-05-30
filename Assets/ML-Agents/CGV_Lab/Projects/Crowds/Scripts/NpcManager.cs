using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class NpcManager : MonoBehaviour
{
    private int currentFailAgentNum;// 目前訓練agent的編號
    private int trainingStartStep; // 設定fail agent的初始化step
    public GameObject agentNpc;
    List<AgentReplayer> allAgentReplayers = new List<AgentReplayer>();
    private FailAgent failAgent;

    // Start is called before the first frame update
    void Start()
    {
        failAgent = FindObjectOfType<FailAgent>();
        currentFailAgentNum = failAgent.currentFailAgentNum;
        trainingStartStep = failAgent.trainingStartStep;

        AgentReplayer firstAgentReplayer = agentNpc.GetComponent<AgentReplayer>();
        allAgentReplayers.Add(firstAgentReplayer);

        for (int i = 1; i < failAgent.totalAgentNum; i++)
        {
            GameObject agentnpc = Instantiate(agentNpc);
            agentnpc.transform.parent = transform;
            agentnpc.name = $"Agent ({i})";
            AgentReplayer agentReplayer = agentnpc.GetComponent<AgentReplayer>();
            if (agentReplayer != null)
            {
                allAgentReplayers.Add(agentReplayer);
            }
        }
    }

    void FixedUpdate()
    {
    }

    public int GetCurrentAgent()
    {
        return currentFailAgentNum;
    }

    public int GetTrainingStartStep()
    {
        return trainingStartStep;
    }

    public void UpdateCurrentFailAgentNum(int currentfailagentnum)
    {
        currentFailAgentNum = currentfailagentnum;
    }

    public void UpdateTrainingStartStep(int num)
    {
        trainingStartStep = num;
    }

    public void ResetAllNPC()
    {
        Debug.Log("Resetting all Replayers in the scene...");

        allAgentReplayers.RemoveAll(r => r == null); // 清理 null 引用
        Debug.Log("allAgentReplayers.Count : " + allAgentReplayers.Count);
        foreach (var npc in allAgentReplayers)
        {
            npc.gameObject.SetActive(true);
            npc.ResetReplayer();
        }
    }

    public void InitAllAgentReplayers()
    {
        foreach (var npc in allAgentReplayers)
        {
            npc.EpisodeInit(failAgent.GetEpisode());
        }
    }

    public void EndAllNPC()
    {
        foreach (var npc in allAgentReplayers)
        {
            npc.gameObject.SetActive(false);
        }
    }
}
