using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EpisodeRunner : MonoBehaviour
{
    public int randNPC = 4;
    private Locations crowdsInitLocation;
    private GameObject[] crowds;

    public void EpisodeInit(Vector3 agentPosition)
    {
        InitCrowds(agentPosition);
    }

    public Color GetDirectionColor(Vector3 direction)
    {
        if (direction == Vector3.zero) return Color.white;

        if (Vector3.Angle(direction, new Vector3(1, 0, 0)) < 1)
        {
            return Color.red;
        }
        else if (Vector3.Angle(direction, new Vector3(-1, 0, 0)) < 1)
        {
            return Color.blue;
        }
        else if (Vector3.Angle(direction, new Vector3(0, 0, 1)) < 1)
        {
            return Color.green;
        }
        else if (Vector3.Angle(direction, new Vector3(0, 0, -1)) < 1)
        {
            //return new Color(1f, 0.6f, 1f, 1f); // pink
            return Color.yellow;
        }
        else if (Vector3.Angle(direction, new Vector3(-0.7f, 0, 0.7f)) < 1)
        {
            return Color.cyan;
            //return Color.yellow;
        }
        else if (Vector3.Angle(direction, new Vector3(0.7f, 0, 0.7f)) < 1)
        {
            return Color.blue;
            //return new Color(1f, 0.647f, 0f, 1f); // orange
        }
        else if (Vector3.Angle(direction, new Vector3(0.7f, 0, -0.7f)) < 1)
        {
            return new Color(0.541f, 0.169f, 0.89f, 1f); // purple
        }
        else if (Vector3.Angle(direction, new Vector3(-0.7f, 0, -0.7f)) < 1)
        {
            return new Color(1f, 0.6f, 1f, 1f); // pink
        }

        return Color.white;
    }

    private void InitCrowds(Vector3 agentPosition)
    {
        if (crowds == null)
        {
            RecordInitCrowdsLocation();
        }

        int rnd = 0;
        for (int i = 0; i < crowds.Length; ++i)
        {
            GameObject crowd = crowds[i];
            rnd = Random.Range(i, crowds.Length);
            crowds[i] = crowds[rnd];
            crowds[rnd] = crowd;
        }

        rnd = Random.Range(0, randNPC);   // 0, 1, 2, 3
        int enableCrowdsNum = rnd * 5;

        for (int i = 0; i < crowds.Length; ++i)
        {
            if (i >= enableCrowdsNum)
            {
                crowds[i].SetActive(false);
                continue;
            }

            crowds[i].SetActive(true);
            NPCControl NPCController = (NPCControl)crowds[i].GetComponent(typeof(NPCControl));
            NPCController.SetInitLocation(RandomInitNPCPosition(agentPosition), Quaternion.Euler(0, Random.Range(-180, 180), 0));
            NPCController.EpisodeInit();
        }
    }

    private void RecordInitCrowdsLocation()
    {
        crowds = GameObject.FindGameObjectsWithTag("CGV_Crowd");
        crowdsInitLocation = new Locations();

        foreach (GameObject crowd in crowds)
        {
            crowdsInitLocation.position.Add(crowd.transform.position);
            crowdsInitLocation.rotation.Add(crowd.transform.rotation);
        }
    }

    private Vector3 RandomInitNPCPosition(Vector3 agentPosition)
    {
        Vector3 position = agentPosition;
        Vector3 randPos = new Vector3(0, 0, 0);
        float randSize = 4f;
        float collisionRadius = 0.5f;
        int sampleMax = 100;
        int sampleCount = 0;
        bool check = false;

        do
        {
            sampleCount++;
            if (sampleCount > sampleMax)
            {
                randPos = new Vector3(1, 0, 1);
                break;
            }

            check = false;

            randPos = new Vector3(Random.Range(-randSize, randSize), 0, Random.Range(-randSize, randSize));

            Collider[] hitColliders = Physics.OverlapSphere(randPos, collisionRadius);
            //for (int i = 0; i < hitColliders.Length; ++i)
            //{
            //    if (hitColliders[i].tag != "CGV_Crowd" && hitColliders[i].tag != "CGV_Ground" && hitColliders[i].tag != "CGV_Expert")
            //    {
            //        check = true;
            //        break;
            //    }
            //} 
        } while (check);

        return position + randPos;
    }
}