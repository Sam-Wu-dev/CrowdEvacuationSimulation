using System.Collections;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.MLAgents;
using UnityEditor;
using System;
using System.Threading.Tasks;


public class Replayer : MonoBehaviour
{
    public bool save;
    public bool replay;
    public int replayStartSecond = 0;

    private int agentNum;
    private string path;
    private string saveData;
    private PassenagerAgent agent;
    private List<Quaternion> rotationData;
    private List<Vector3> positionData;
    private List<Vector3> directionData;
    private int currentReplayNum;
    private Color color = Color.white;

    private FailAgent failAgent;
    private int trainingStartStep;
    private bool isSaving = false;


    void Start()
    {
        Time.timeScale = 1f;
        //Time.timeScale = 3f;
        //Debug.Log("Application.dataPath : " + Application.dataPath);

        agent = GetComponent<PassenagerAgent>();
        failAgent = FindObjectOfType<FailAgent>();

#if UNITY_EDITOR
        if (failAgent == null || !failAgent.enabled)
        {
            EditorApplication.playModeStateChanged += SaveData;
        }
#endif

        agentNum = ParseName();   //replay
        if (failAgent != null && failAgent.enabled && failAgent.load)
        {
            agentNum = failAgent.currentFailAgentNum; // training
        }
        //Debug.Log($"{agentNum}");

        EpisodeInit();
        //when training
        trainingStartStep = failAgent.trainingStartStep;
        currentReplayNum = trainingStartStep;
        if (!save) LoadData();

        //if (replay) LoadData();
        //if (replay) currentReplayNum = replayStartSecond * 40;
        //Debug.Log("Replayer Start.");
    }

    // 只用來對不是TRAINING AGENT的有用 FAIL AGENT改在PASSENGER AGENT中
    void FixedUpdate()
    {
        if (save && !isSaving)
        {
            SaveState();
        }
        //agent.SetInference(!(replay));

        // 如果該agent不為failAgent且replay是true那就讀檔
        if (replay)
        {
            //Debug.Log("agent id : " +  agentNum.ToString());

            agent.SetInference(!(replay));

            if (replay && currentReplayNum >= positionData.Count)
            {
                replay = false;
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

            //transform.rotation = rotationData[currentReplayNum];
            //transform.rotation = Quaternion.Lerp(transform.rotation, rotationData[currentReplayNum], 0.05f);
            agent.direction_tmp = directionData[currentReplayNum];

            if (currentReplayNum % 20 == 0)
            {
                agent.ReplayUpdate();
            }

            color = agent.GetDirectionColor();
            //if (r != null) color = r.GetDirectionColor(agent.direction_tmp);

            currentReplayNum++;
        }
    }

#if UNITY_EDITOR
    private void SaveData(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && save)
        {
            StreamWriter writer = new StreamWriter(path, false);
            writer.Write(saveData);
            writer.Close();
        }
    }
    public async Task SaveDataAsync()
    {
        if (isSaving) return; // Prevent multiple saves

        isSaving = true;

        using (StreamWriter writer = new StreamWriter(path, false))
        {
            await writer.WriteAsync(saveData);
        }

        isSaving = false;
    }
#endif

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
    private void SaveState()
    {
        saveData += transform.position.x.ToString() + ", " + transform.position.y.ToString() + ", " + transform.position.z.ToString() + '\n';
        saveData += agent.direction_tmp.x.ToString() + ", " + agent.direction_tmp.y.ToString() + ", " + agent.direction_tmp.z.ToString() + '\n';
        saveData += transform.rotation.x.ToString() + ", " + transform.rotation.y.ToString() + ", " + transform.rotation.z.ToString() + ", " + transform.rotation.w.ToString() + '\n';
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

        //agent.SetInference(true);
    }

    public void UpdateLoader(int trainingAgentNum)
    {
        path = Application.dataPath + failAgent.failAgentFolderPath + "episode" + failAgent.GetEpisode() + "/Replay/" + "agent_" + trainingAgentNum.ToString() + ".txt";
        LoadData();
        //Debug.Log($"load data isactive: {isActiveAndEnabled}");
    }

    public void UpdateTrainingStartStep(int num)
    {
        trainingStartStep = num;
    }

    public Quaternion GetFailAgentStartRotation()
    {
        return rotationData[trainingStartStep];
    }

    public Vector3 GetFailAgentStartPosition()
    {
        Debug.Log("trainingStartStep : " + trainingStartStep);
        Debug.Log("positionData.Count : " + positionData.Count);
        return positionData[trainingStartStep];
    }

    public Vector3 GetFailAgentStartDirection()
    {
        Debug.Log("directionData.Count : " + directionData.Count);
        return directionData[trainingStartStep];
    }

    public void EpisodeInit()
    {
        path = Application.dataPath + failAgent.failAgentFolderPath + "episode" + failAgent.GetEpisode() + "/Replay/" + "agent_" + agentNum.ToString() + ".txt";
        saveData = "";
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