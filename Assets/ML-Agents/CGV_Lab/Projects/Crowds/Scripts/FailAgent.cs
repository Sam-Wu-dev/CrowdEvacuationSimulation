using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using UnityStandardAssets.Water;

public class FailAgent : MonoBehaviour
{
    public GameObject agent;

    public bool save;
    public bool load;
    public bool check;
    public bool checkRay;
    public bool crowd;  // use crowd to finetune if load, save only failagent.txt if save
    public string failAgentFolderPath;
    public int totalAgentNum = 1000;
    public int currentFailAgentNum = -1;// 目前訓練agent的編號

    public List<Tuple<int, int, int>> agentInfos = new List<Tuple<int, int, int>>(); // agentNum, startStep, endStep
    public Dictionary<int, Tuple<int, int>> agentStepDict = new Dictionary<int, Tuple<int, int>>(); // agentNum -> (startStep, endStep)

    private string failAgentPath;
    private string folder;

    private Queue<int> failAgentQueue = new Queue<int>();
    private NpcManager npcManager;
    public int trainingStartStep; // 設定fail agent的初始化step
    public int trainingIterationsPerAgent = 10; // 每個agent訓練次數
    public int backStep; // 設定回推步數
    List<Replayer> allReplayers = new List<Replayer>();

    private int episode = 0;
    private int totalEpisode = 0;
    public int targetFailAgentNum = 1000;
    private int curFailAgentNum = 0;

    private List<PassenagerAgent> allAgents = new List<PassenagerAgent>();
    private HashSet<PassenagerAgent> finishedAgents = new HashSet<PassenagerAgent>();
    private bool timerStarted = false;
    private bool episodeFinalized = false;
    private int lastFinishedAgentsCount = 0;

    private int noProgressCounter = 0;
    public int maxNoProgressSteps = 100;  //how many steps to wait with no new finished agents
    private int step = 0;

    public bool allAgentFinetune = false;  // 是否所有agent都要進行finetune
    public bool mixAgentFinetune = false;  // 是否用failagent和成功agent進行finetune
    public bool failAgentFinetune = false;  // 是否只用failagent進行finetune

    public int mixRatio = 1;  // successAgent/failAgent比例

    void Start()
    {
        //#if UNITY_EDITOR
        //        EditorApplication.playModeStateChanged += SaveAgentInfos;
        //        EditorApplication.playModeStateChanged += SaveAgentBackStep;
        //#endif
        folder = Application.dataPath;
        failAgentPath = folder + failAgentFolderPath + "episode" + episode + "/" + "failagent.txt";

        if (allAgentFinetune)
        {
            mixRatio = totalAgentNum;
        }
        else if (failAgentFinetune)
        {
            mixRatio = 0;
        }
        else
        {
            if (mixRatio == 0)
            {
                mixRatio = 1;
            }
        }

        if (save)
        {
            CreateDirectory();
            PassenagerAgent firstAgent = FindObjectOfType<PassenagerAgent>();
            allAgents.Add(firstAgent);

            for (int i = 1; i < totalAgentNum; i++)
            {
                GameObject newAgent = Instantiate(agent);
                newAgent.transform.parent = transform;
                newAgent.name = $"Agent ({i})";
                PassenagerAgent passenagerAgent = newAgent.GetComponent<PassenagerAgent>();
                if (newAgent)
                {
                    allAgents.Add(passenagerAgent);
                }
            }
        }
        else if (load)
        {
            if (!crowd)
            {
                npcManager = FindObjectOfType<NpcManager>();
            }
            EpisodeInit();
            //Debug.Log("Fail Agent Start.");
        }
        else if (check)
        {
            failAgentPath = folder + failAgentFolderPath + "episode" + episode + "/" + "failagent.txt";
            // Read and parse failAgent.txt
            ReadFailAgentFile(failAgentPath);

            agent.name = $"Agent ({agentStepDict.Keys.ElementAt(0)})";

            for (int i = 1; i < agentStepDict.Count; i++)
            {
                GameObject newAgent = Instantiate(agent);
                newAgent.transform.parent = transform;
                newAgent.name = $"Agent ({agentStepDict.Keys.ElementAt(i)})";
            }
        }
    }

    void FixedUpdate()
    {
        if (save && !episodeFinalized)
        {
            if (timerStarted)
            {
                // Check if finishedAgents.Count has changed
                if (finishedAgents.Count > lastFinishedAgentsCount)
                {
                    lastFinishedAgentsCount = finishedAgents.Count;
                    noProgressCounter = 0; // reset
                }
                else
                {
                    noProgressCounter++;
                }

                // Finalize if no progress for a while
                if (noProgressCounter >= maxNoProgressSteps)
                {
                    episodeFinalized = true;
                    Debug.Log($"step: {step}, noProgressCounter: {noProgressCounter}");
                    Debug.Log($"[No progress detected] Finalizing. Finished count stuck at: {finishedAgents.Count}");

                    FinalizeEpisode();
                }
            }

            if (curFailAgentNum >= targetFailAgentNum)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }
        step++;
    }


#if UNITY_EDITOR
    /// <summary>
    /// Save data when exiting Play mode (recording mode).
    /// </summary>
    /// <param name="state"></param>
    //void SaveAgentInfos(PlayModeStateChange state)
    //{
    //    if (state == PlayModeStateChange.ExitingPlayMode && save)
    //    {
    //        StreamWriter writer = new StreamWriter(failAgentPath, false);
    //        foreach (var info in agentInfos)
    //        {
    //            writer.WriteLine($"{info.Item1} {info.Item2} {info.Item3}");
    //        }
    //        writer.Close();
    //        //Debug.Log($"Fail agent data saved to {failAgentFilePath}");
    //    }
    //}
    void SaveAgentInfos()
    {
        if (save)
        {
            StreamWriter writer = new StreamWriter(failAgentPath, false);
            foreach (var info in agentInfos)
            {
                writer.WriteLine($"{info.Item1} {info.Item2} {info.Item3}");
            }
            writer.Close();
            //Debug.Log($"Fail agent data saved to {failAgentFilePath}");
        }
    }
#endif

    void EpisodeInit()
    {
        ProcessFailAgents();
        initializeFailAgents();
    }

    /// <summary>
    /// Loads and processes failAgent.txt file when in loadFail mode.
    /// </summary>
    private void ProcessFailAgents()
    {
        while (true)
        {
            failAgentPath = folder + failAgentFolderPath + "episode" + episode + "/" + "failagent.txt";
            // Read and parse failAgent.txt
            int failagentCnt = ReadFailAgentFile(failAgentPath);
            if (failagentCnt > 0) break;
            episode++;
        }

        allReplayers.Clear();

        Replayer replayer = gameObject.GetComponentInChildren<Replayer>();
        if (replayer != null)
        {
            allReplayers.Add(replayer);
        }
        //Debug.Log($"Processed {agentStepDict.Count} agents from failAgent.txt.");
    }

    /// <summary>
    /// Reads the failAgent.txt and stores agentNum and skipStep.
    /// </summary>
    private int ReadFailAgentFile(string path)
    {
        if (!File.Exists(path))
        {
            //Debug.LogError($"Fail agent file not found at: {path}");
            return 0;
        }

        agentStepDict.Clear(); // Clear existing data

        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            string[] parts = line.Split(' ');

            if (parts.Length >= 3 &&
                int.TryParse(parts[0], out int agentNum) &&
                int.TryParse(parts[1], out int startStep) &&
                int.TryParse(parts[2], out int endStep))
            {
                agentStepDict[agentNum] = new Tuple<int, int>(startStep, endStep);
            }
            else
            {
                Debug.LogWarning($"Invalid line format: {line}");
            }
        }
        Debug.Log($"Parsed {agentStepDict.Count} agents from {path}.");
        return agentStepDict.Count;
    }

    private void initializeFailAgents()
    {
        failAgentQueue.Clear();
        int failagentCount = agentStepDict.Count;
        int successAgentCount = failagentCount * mixRatio;
        if (failagentCount + successAgentCount > totalAgentNum)
        {
            successAgentCount = totalAgentNum - failagentCount;
        }

        List<int> failAgentNums = new List<int>();
        foreach (var failagent in agentStepDict)
        {
            int failAgentNum = failagent.Key;
            failAgentNums.Add(failAgentNum);
        }
        failAgentNums.Sort();

        int num = 0;
        List<int> successAgentNums = new List<int>();
        for (int i = 0; i < successAgentCount; i++)
        {
            while (agentStepDict.ContainsKey(num))
            {
                num++;
            }
            successAgentNums.Add(num);
            num++;
        }
        Debug.Log($"failagentCount: {failAgentNums.Count}, successAgentCount: {successAgentNums.Count}");

        int allAgentCount = failagentCount + successAgentCount;
        int failAgentIdx = 0, successAgentIdx = 0;
        for (int i = 0; i < allAgentCount; i++)  // use interleaving to add agentNum to failAgentQueue
        {
            if (failAgentIdx < failAgentNums.Count)
            {
                failAgentQueue.Enqueue(failAgentNums[failAgentIdx]);
                failAgentIdx++;
            }

            for (int j = 0; j < mixRatio; j++)
            {
                if (successAgentIdx < successAgentNums.Count)
                {
                    failAgentQueue.Enqueue(successAgentNums[successAgentIdx]);
                    successAgentIdx++;
                }
            }
        }
        Debug.Log($"failAgentQueue: {failAgentQueue.Count}");

        currentFailAgentNum = GetNextTrainingAgent();
        if (agentStepDict.ContainsKey(currentFailAgentNum))
        {
            trainingStartStep = Math.Max(agentStepDict[currentFailAgentNum].Item1 - backStep, 0);
        }
        else
        {
            trainingStartStep = 0;
        }

        if (currentFailAgentNum != -1)
        {
            Debug.Log($"Starting training for agent {currentFailAgentNum}, trainingStartStep: {trainingStartStep}");
        }
    }

    private int GetNextTrainingAgent()
    {
        while (failAgentQueue.Count > 0)
        {
            int nextAgent = failAgentQueue.Dequeue();
            return nextAgent;
        }
        return -1; // 所有卡住代理都已訓練完成
    }

    public void ResetAllNpc(bool switchTrainingAgent = false, bool isTargetArea = false)
    {
        if (isTargetArea) totalEpisode++;

        Replayer newTrainingReplayer = allReplayers[0]; // training 時只有一個agent
        Debug.Log($"totalEpisode: {totalEpisode}");

        if (totalEpisode >= trainingIterationsPerAgent * targetFailAgentNum)
        {
            if (npcManager != null && npcManager.enabled)
            {
                npcManager.EndAllNPC();
            }
            if (newTrainingReplayer != null && newTrainingReplayer.gameObject != null)
            {
                newTrainingReplayer.gameObject.SetActive(false);
            }
            return;
        }

        if (npcManager != null && npcManager.enabled)
        {
            npcManager.ResetAllNPC();
        }

        if (switchTrainingAgent)
        {
            currentFailAgentNum = GetNextTrainingAgent();
            if (currentFailAgentNum == -1)
            {
                episode++;
                EpisodeInit();
                if (npcManager != null && npcManager.enabled)
                {
                    npcManager.InitAllAgentReplayers();
                }

            }

            if (agentStepDict.ContainsKey(currentFailAgentNum))
            {
                trainingStartStep = Math.Max(agentStepDict[currentFailAgentNum].Item1 - backStep, 0);
            }
            else
            {
                trainingStartStep = 0;
            }

            if (npcManager != null && npcManager.enabled)
            {
                npcManager.UpdateCurrentFailAgentNum(currentFailAgentNum);
                npcManager.UpdateTrainingStartStep(trainingStartStep);
            }

            if (newTrainingReplayer != null)
            {
                newTrainingReplayer.UpdateLoader(currentFailAgentNum);
                newTrainingReplayer.UpdateTrainingStartStep(trainingStartStep);
            }
        }
        Debug.Log($"Starting training for agent {currentFailAgentNum}, trainingStartStep: {trainingStartStep}");
    }

    private void CreateDirectory()
    {
        String fullFailAgentFolderPath = folder + failAgentFolderPath + "episode" + episode + "/";
        String replayFolderPath = fullFailAgentFolderPath + "/Replay/";

        Directory.CreateDirectory(fullFailAgentFolderPath);
        Directory.CreateDirectory(replayFolderPath);

        failAgentPath = fullFailAgentFolderPath + "failagent.txt";
    }

    public int GetEpisode()
    {
        return episode;
    }

    public void NotifyAgentReachedTarget(PassenagerAgent agent)
    {
        if (!finishedAgents.Contains(agent))
        {
            finishedAgents.Add(agent);
            //Debug.Log($"Agent reached target: {agent.name}. Total finished: {finishedAgents.Count}/{totalAgentNum}");

            // Start timer once first agent finishes
            if (!timerStarted)
            {
                timerStarted = true;
            }
        }
    }

    private async void FinalizeEpisode()
    {
        try
        {
            Debug.Log("Finalizing episode...");

#if UNITY_EDITOR
            SaveAgentInfos();
#endif

            curFailAgentNum += agentInfos.Count;

            Replayer[] replayers = GetComponentsInChildren<Replayer>(true);
            List<Task> saveTasks = new List<Task>();

#if UNITY_EDITOR
            if (!crowd)
            {
                foreach (Replayer replayer in replayers)
                {
                    if (replayer != null)
                    {
                        if (replayer.Equals(null)) continue; // handle destroyed reference

                        Task saveTask = replayer.SaveDataAsync();
                        saveTasks.Add(saveTask);
                    }
                }
            }
            else
            {
                foreach (var info in agentInfos)
                {
                    Replayer replayer = replayers[info.Item1];
                    if (replayer != null)
                    {
                        if (replayer.Equals(null)) continue; // handle destroyed reference

                        Task saveTask = replayer.SaveDataAsync();
                        saveTasks.Add(saveTask);
                    }
                }
            }
#endif
            await Task.WhenAll(saveTasks); // Wait for all SaveDataAsync to finish

            episode++;

            Debug.Log("All replayer data saved asynchronously. Proceeding...");

            agentInfos.Clear();
            finishedAgents.Clear();
            ResetAllAgent();
            CreateDirectory();

            Debug.Log("Finalization complete.");
        }
        catch (Exception e)
        {
            Debug.LogError("Exception in FinalizeEpisode: " + e);
        }
    }

    private void ResetAllAgent()
    {
        Debug.Log("Resetting all PassenagerAgent in the scene...");

        allAgents.RemoveAll(r => r == null); // 清理 null 引用
        Debug.Log("allPassenagerAgents.Count : " + allAgents.Count);
        foreach (var agent in allAgents)
        {
            agent.gameObject.SetActive(true);
            agent.ResetAgent();
            agent.EndEpisode();
            //Debug.Log($"agentNum: {agent.name}, is active: {agent.isActiveAndEnabled}");
        }
        step = 0;
        episodeFinalized = false;
        noProgressCounter = 0;
        timerStarted = false;
        lastFinishedAgentsCount = 0;
    }
}
