using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGizmos : MonoBehaviour
{
    public DepthCamera viewCamera;

    private LineRenderer lineRenderer;
    private AgentController agentController;
    private Color directionBoardColor = Color.clear;

    private float arrowLength = 1f;
    private float lineThickness = 0.2f;
    private float agentHeight = 2f;
    
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = directionBoardColor;
        lineRenderer.endColor = directionBoardColor;
        agentController = GetComponentInParent<AgentController>();
    }

    void Update()
    {
        UpdateCircle();
    }


    // Update is called once per frame
    public void UpdateCircle()
    {
        //if (agentController.CheckFrontAgent())
        //{
        //    DrawCircleShape(Color.gray);
        //}
        //else
        //{
        //    directionBoardColor = viewCamera.GetDirectionColor();
        //    DrawCircleShape(directionBoardColor);
        //};
        directionBoardColor = viewCamera.GetDirectionColor();
        DrawCircleShape(directionBoardColor);
    }

    private void DrawCircleShape(Color color)
    {
        int segments = 30;  
        float radius = arrowLength * 0.1f;  
        Vector3 center = transform.position + new Vector3(0, agentHeight, 0);

        lineRenderer.positionCount = segments + 1; 

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 point = new Vector3(center.x + x, center.y, center.z + z);
            lineRenderer.SetPosition(i, point);
        }

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
