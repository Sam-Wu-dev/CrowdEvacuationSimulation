using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextController : MonoBehaviour
{
    public Text actionText;
    public float reward = 0;

    float x = 0;
    float y = 0;

    // Start is called before the first frame update
    void Start()
    {
        actionText.text = "Action x: " + x.ToString("F2") + ", y:" + y.ToString("F2");
    }

    // Update is called once per frame
    void Update()
    {
        DisplayReward();
        //DisplayAction();
    }

    void DisplayReward()
    {
        actionText.text = "Reward: " + reward.ToString("F2");
    }

    void DisplayAction()
    {
        Vector3 moveVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        moveVector.x = Mathf.Clamp(moveVector.x, -1, 1);
        moveVector.z = Mathf.Clamp(moveVector.z, 0, 1);
        
        actionText.text = "Action x: " + moveVector.x.ToString("F2") + ", y:" + moveVector.z.ToString("F2");
    }
}