using System.Collections;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.MLAgents;
using UnityEditor;
using System;

public class AgentReplayer : MonoBehaviour
{
    private int agentNum;
    private string path;
    private List<Quaternion> rotationData;
    private List<Vector3> positionData;
    private List<Vector3> directionData;
    private int currentReplayNum = 0;
    private Color color = Color.white;

    private NpcManager npcManager;
    private int trainingStartStep;
    private FailAgent failAgent;

    void Start()
    {
        Time.timeScale = 1f;
        //Time.timeScale = 3f;
        //Debug.Log("Application.dataPath : " + Application.dataPath);

        agentNum = ParseName();
        //Debug.Log($"{agentNum}");

        failAgent = FindObjectOfType<FailAgent>();
        EpisodeInit(failAgent.GetEpisode());

        //when training
        npcManager = FindObjectOfType<NpcManager>();
        currentReplayNum = npcManager.GetTrainingStartStep();
    }

    // 只用來對不是TRAINING AGENT的有用 FAIL AGENT改在PASSENGER AGENT中
    void FixedUpdate()
    {
        if (agentNum == npcManager.GetCurrentAgent())
        {
            gameObject.SetActive(false);
        }
        // 如果該agent不為failAgent且replay是true那就讀檔
        if (agentNum != npcManager.GetCurrentAgent())
        {
            //Debug.Log("agent id : " +  agentNum.ToString());

            if (currentReplayNum >= positionData.Count)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.position = positionData[currentReplayNum];
            //Debug.Log($"{transform.position}");

            if (currentReplayNum == 0)
            {
                transform.rotation = rotationData[currentReplayNum];
            }
            else if (currentReplayNum + 1 < positionData.Count)
            {
                Vector3 avgNextPosition = Vector3.zero;
                int count = 1;
                while (currentReplayNum + count < positionData.Count && count <= 5)
                {
                    avgNextPosition += positionData[currentReplayNum + count];
                    count++;
                }
                avgNextPosition /= (count - 1);

                Vector3 targetDirection = (avgNextPosition - positionData[currentReplayNum]).normalized;
                if (targetDirection != Vector3.zero && Vector3.Distance(avgNextPosition, positionData[currentReplayNum]) > 0.015f)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetDirection, Vector3.up), 0.04f);
                    transform.rotation = Quaternion.Lerp(transform.rotation, rotationData[currentReplayNum], 0.05f);
                }
            }

            currentReplayNum++;
        }
    }

    // Name patten "Agent (1)"
    public int ParseName()
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

    public void EpisodeInit(int episode)
    {
        path = Application.dataPath + failAgent.failAgentFolderPath + "episode" + failAgent.GetEpisode() + "/Replay/" + "agent_" + agentNum.ToString() + ".txt";
        //Debug.Log("Path : " + path);
        LoadData();
    }

    private void LoadData()
    {
        //agent.SetInference(false);

        positionData = new List<Vector3>();
        directionData = new List<Vector3>();
        rotationData = new List<Quaternion>();

        StreamReader reader = new StreamReader(path);
        while (reader.Peek() >= 0)
        {
            string linePositionData = reader.ReadLine();
            string[] positionSplit = linePositionData.Split(',');
            if (positionSplit.Length == 3)
            {
                positionData.Add(new Vector3(float.Parse(positionSplit[0]), float.Parse(positionSplit[1]), float.Parse(positionSplit[2])));
                //Debug.Log($"{new Vector3(float.Parse(positionSplit[0]), float.Parse(positionSplit[1]), float.Parse(positionSplit[2]))}");
            }

            string lineDirectionData = reader.ReadLine();
            string[] directionSplit = lineDirectionData.Split(',');
            if (directionSplit.Length == 3)
            {
                directionData.Add(new Vector3(float.Parse(directionSplit[0]), float.Parse(directionSplit[1]), float.Parse(directionSplit[2])));
            }

            string lineRotationData = reader.ReadLine();
            string[] rotationSplit = lineRotationData.Split(',');
            if (rotationSplit.Length == 4)
            {
                rotationData.Add(new Quaternion(float.Parse(rotationSplit[0]), float.Parse(rotationSplit[1]), float.Parse(rotationSplit[2]), float.Parse(rotationSplit[3])));
            }
        }
        reader.Close();
    }

    public void ResetReplayer()
    {
        gameObject.SetActive(true); // 確保物件啟用
        currentReplayNum = npcManager.GetTrainingStartStep(); // 重置回放進度
        //Debug.Log("currentReplayNum : " + currentReplayNum);
    }

    /*
    private void OnDrawGizmos()
    {
        if (agent == null) return;
        //color = agent.GetDirectionColor();
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position + transform.up * 2, 0.1f);
    }
    */
}