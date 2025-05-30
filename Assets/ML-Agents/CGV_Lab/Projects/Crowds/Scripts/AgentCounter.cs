using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AgentCounter : MonoBehaviour
{
    public string path;
    public int recordPeriod;
    public int maxStep = 12000;

    private string data;
    private int step;
    
    void Start()
    {
        data = "";
        step = 0;

        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged += SaveData;
        #endif
    }

    void FixedUpdate()
    {
        if (step % recordPeriod == 0) RecoardAgentNum();
        step++;
    }

    public void RecoardAgentNum()
    {
        PassenagerAgent[] agents = FindObjectsOfType<PassenagerAgent>();
        
        data += agents.GetLength(0) + "\n";

        #if UNITY_EDITOR
        if (agents.GetLength(0) == 0 || step >= maxStep) EditorApplication.isPlaying = false;
        #endif
    }

    #if UNITY_EDITOR
    void SaveData(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            StreamWriter writer = new StreamWriter(path, false);
            writer.Write(data);
            writer.Close();
        }
    }
    #endif

}