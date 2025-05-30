using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class TrajectoryRecorder : MonoBehaviour
{
    public string folderPath;
    public bool recordInitialState = false;
    
    private bool start = false;
    private int count = 0;
    private int savePeriod = 20;
    private int trajectory_num = 0;
    private string saveData;
    private string initialData;
    private List<Vector3> initPositionData;
    private List<Quaternion> initRotationData;

    void Start()
    {
        start = false;

        initPositionData = new List<Vector3>();
        initRotationData = new List<Quaternion>();

        string readPath = "Assets/ML-Agents/CGV_Lab/Projects/Crowds/Trajectory/GAIL_0321/initial_states.txt";
        StreamReader reader = new StreamReader(readPath);

        while (reader.Peek() >= 0)
        {
            string positionData = reader.ReadLine();
            string[] positionSplit = positionData.Split(',');

            initPositionData.Add(new Vector3(float.Parse(positionSplit[0]), 0.05f, float.Parse(positionSplit[1])));

            string rotationData = reader.ReadLine();
            string[] rotationSplit = rotationData.Split(',');

            initRotationData.Add(new Quaternion(float.Parse(rotationSplit[0]), float.Parse(rotationSplit[1]), float.Parse(rotationSplit[2]), float.Parse(rotationSplit[3])));
        }
    }

    public void StartEpisode()
    {
        start = true;
        count = 0;

        if (recordInitialState)
        {
            initialData += transform.position.x.ToString() + ", " + transform.position.z.ToString() + '\n'; 
            initialData += transform.rotation.x.ToString() + ", " + transform.rotation.y.ToString() + ", " + transform.rotation.z.ToString() + ", " + transform.rotation.w.ToString() + '\n'; 
        }
        
        if (trajectory_num < initPositionData.Count)
        {
            transform.position = initPositionData[trajectory_num];
            transform.rotation = initRotationData[trajectory_num];
        }
        
        Debug.Log(trajectory_num);
    }

    void FixedUpdate()
    {
        if (!start) return;

        if (count % savePeriod == 0)
        {
            saveData += transform.position.x.ToString() + ", " + transform.position.z.ToString() + '\n';
        }
        
        count++;
    }

    // save data
    public void EndEpisode()
    {        
        string path = folderPath + "tra_" + (trajectory_num) + ".txt";

        StreamWriter writer = new StreamWriter(path, false);
        writer.Write(saveData);
        writer.Close();

        saveData = "";
        trajectory_num++;

        writer = new StreamWriter(folderPath + "initial_states.txt");
        writer.Write(initialData);
        writer.Close();
    }
}