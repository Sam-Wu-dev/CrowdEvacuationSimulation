using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DirectionLabel { None, Forward, Back, Left, Right, Target }

public class DirectionBoard : MonoBehaviour
{
    public DirectionLabel directionLabel;
    public GameObject closeRangeObject;
    public GameObject closeRangeObject2;
    public int stepRewardNum = 30;
    public List<GameObject> checkPoints;
    public Color color = Color.white;

    private Vector3 direction;
    private List<Bounds> bound;
    private bool initialize = false;
    private List<Vector3> checkPositions;

    private List<string> names = new List<string> { "direction_back (1)", "direction_back (2)", "direction_back (3)", "direction_back (4)" };
    private List<Color> colors = new List<Color> { new Color(1f, 0.2f, 0.2f), new Color(0.5f, 0f, 0f), new Color(0.7f, 0.1f, 0.1f), new Color(1f, 0.4f, 0.4f) };


    private LineRenderer lineRenderer;
    private LineRenderer arrowHeadLeftRenderer;
    private LineRenderer arrowHeadRightRenderer;

    private float arrowLength = 1f;
    private float lineThickness = 0.2f;

    void Start()
    {
        //Debug.Log("DirectionBoard Start");

        switch (directionLabel)
        {
            case DirectionLabel.Left:
                direction = Quaternion.Euler(0, 90, 0) * transform.forward;
                break;
            case DirectionLabel.Right:
                direction = Quaternion.Euler(0, 270, 0) * transform.forward;
                break;
            case DirectionLabel.Forward:
                direction = -transform.forward;
                break;
            case DirectionLabel.Back:
                direction = transform.forward;
                break;
            case DirectionLabel.Target:
                direction = transform.forward;
                break;
            default:
                break;
        }
        direction = direction.normalized;
        ////clchen
        EpisodeRunner r = FindObjectOfType<EpisodeRunner>();

        // Get the name of the current GameObject
        string objectName = gameObject.name;

        // Find the matching index
        int index = names.IndexOf(objectName);
        if (index != -1)
        {
            color = colors[index];
        }
        else
        {
            color = r.GetDirectionColor(direction);
        }

        //lineRenderer = GetComponent<LineRenderer>();
        //wang
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        GameObject arrowHeadLeft = new GameObject("ArrowHeadLeft");
        arrowHeadLeft.transform.SetParent(transform);
        arrowHeadLeftRenderer = arrowHeadLeft.AddComponent<LineRenderer>();
        arrowHeadLeftRenderer.startWidth = lineThickness;
        arrowHeadLeftRenderer.endWidth = lineThickness;
        arrowHeadLeftRenderer.positionCount = 2;
        arrowHeadLeftRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arrowHeadLeftRenderer.startColor = color;
        arrowHeadLeftRenderer.endColor = color;

        GameObject arrowHeadRight = new GameObject("ArrowHeadRight");
        arrowHeadRight.transform.SetParent(transform);
        arrowHeadRightRenderer = arrowHeadRight.AddComponent<LineRenderer>();
        arrowHeadRightRenderer.startWidth = lineThickness;
        arrowHeadRightRenderer.endWidth = lineThickness;
        arrowHeadRightRenderer.positionCount = 2;
        arrowHeadRightRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arrowHeadRightRenderer.startColor = color;
        arrowHeadRightRenderer.endColor = color;

        //DrawArrowShape();
    }

    public void EpisodeInit()
    {
        //Debug.Log("DirectionBoard EpisodeInit");

        if (initialize) return;
        initialize = true;
        bound = new List<Bounds>();

        BoxCollider c = closeRangeObject.GetComponent<BoxCollider>();
        bound.Add(c.bounds);
        c.enabled = false;

        if (closeRangeObject2 != null)
        {
            c = closeRangeObject2.GetComponent<BoxCollider>();
            bound.Add(c.bounds);
            c.enabled = false;
        }

        checkPositions = new List<Vector3>();
        for (int i = 0; i < checkPoints.Count; ++i)
        {
            checkPositions.Add(checkPoints[i].transform.position);
        }
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public DirectionLabel GetLabel()
    {
        return directionLabel;
    }

    public Vector3 GetDirectionBoardPosition()
    {
        return gameObject.transform.position;
    }

    public int GetStepRewardNum()
    {
        return stepRewardNum;
    }

    // should not rotate the range object and only support f, b, l, r directions
    public bool IsClose(Vector3 pos)
    {
        for (int i = 0; i < bound.Count; ++i)
        {
            if (bound[i].Contains(pos))
            {
                return true;
            }
        }

        return false;
    }

    public List<Vector3> GetCheckPointPositions()
    {
        return checkPositions;
    }

    private void DrawArrowShape()
    {
        Vector3 startPoint = transform.position - direction * arrowLength;
        Vector3 endPoint = transform.position + direction * arrowLength;

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        Vector3 normDirection = (endPoint - startPoint).normalized;
        Vector3 perpendicular = Vector3.Cross(normDirection, Vector3.up) * lineThickness * 2;

        arrowHeadLeftRenderer.SetPosition(0, endPoint);
        arrowHeadLeftRenderer.SetPosition(1, endPoint - normDirection * lineThickness * 3 + perpendicular);

        arrowHeadRightRenderer.SetPosition(0, endPoint);
        arrowHeadRightRenderer.SetPosition(1, endPoint - normDirection * lineThickness * 3 - perpendicular);
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = color;

    //    Vector3 p = transform.position - direction;
    //    Vector3 v = transform.position + direction;
    //    Vector3 v1 = v - Quaternion.Euler(0, 45, 0) * direction;
    //    Vector3 v2 = v - Quaternion.Euler(0, -45, 0) * direction;

    //    DrawThickLine(p, v, 5);
    //    DrawThickLine(v, v1, 5);
    //    DrawThickLine(v, v2, 5);
    //}

    //private void DrawThickLine(Vector3 start, Vector3 end, int thickens)
    //{
    //    Vector2 line = new Vector2(end.x - start.x, end.z - start.z);
    //    Vector2 line_n = Vector2.Perpendicular(line);
    //    float unit = 0.025f;

    //    for (int i = 0; i < thickens; ++i)
    //    {
    //        Vector3 offset = new Vector3(line_n.x * unit * (i - thickens / 2), 0, line_n.y * unit * (i - thickens / 2));
    //        Gizmos.DrawLine(start + offset, end + offset);
    //    }
    //}
}
