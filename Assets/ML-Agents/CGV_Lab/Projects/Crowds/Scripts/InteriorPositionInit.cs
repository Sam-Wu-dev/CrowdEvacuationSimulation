using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteriorPositionInit : MonoBehaviour
{
    public GameObject initialPositionList;

    public void EpisodeInit()
    {
        PassenagerAgent[] agents = FindObjectsOfType<PassenagerAgent>();
        Vector3[] rndPosition = new Vector3[initialPositionList.transform.childCount];

        for (int i = 0; i < rndPosition.Length; ++i)
        {
            rndPosition[i] = initialPositionList.transform.GetChild(i).position;
        }

        for (int i = 0; i < rndPosition.Length; ++i)
        {
            int rnd = Random.Range(0, rndPosition.Length);
            Vector3 tmp = rndPosition[rnd];
            rndPosition[rnd] = rndPosition[i];
            rndPosition[i] = tmp;
        }

        int idx = 0;
        for (int i = 0; i < agents.Length; ++i)
        {
            agents[i].transform.position = rndPosition[idx];
            agents[i].transform.rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);

            idx++;
            idx %= rndPosition.Length;
        }
    }
}