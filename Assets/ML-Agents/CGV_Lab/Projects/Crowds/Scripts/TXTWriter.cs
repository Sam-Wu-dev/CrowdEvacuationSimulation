using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TXTWriter : MonoBehaviour
/*  <summary>
 * This class is used to record the type of reward the agent receives at each step during training.   
 */
{
    public string fileName;

    private string txtFilePath;

    void Start()
    {
        string uniqueID = System.Guid.NewGuid().ToString();
        txtFilePath = System.IO.Path.Combine(Application.dataPath, $"AgentData_{fileName}_{uniqueID}.txt");
        //Debug.Log(txtFilePath);

        if (!System.IO.File.Exists(txtFilePath))
        {
            System.IO.File.WriteAllText(txtFilePath, "Eposide  SuccessRate\n");
        }

    }

    public void WriteData(int eposide, float successRate)
    {
        string data = $"{eposide} {successRate}\n";
        System.IO.File.AppendAllText(txtFilePath, data);
    }
}
