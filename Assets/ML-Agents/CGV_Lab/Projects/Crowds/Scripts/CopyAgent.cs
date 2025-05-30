using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* <summary>
*  Parameters:
*  agentNum: int = number of agent you want to copy
*  copyAgent: GameObject = object that you want to copy
*/
public class CopyAgent : MonoBehaviour
{
    public int agentNum;

    public GameObject copyAgent;
    void Start()
    {
        for (int i = 1; i < agentNum; i++)
        {
            GameObject newAgent = Instantiate(copyAgent);
            newAgent.transform.parent = transform;
            newAgent.name = $"Agent ({i})";
        }
    }
}
