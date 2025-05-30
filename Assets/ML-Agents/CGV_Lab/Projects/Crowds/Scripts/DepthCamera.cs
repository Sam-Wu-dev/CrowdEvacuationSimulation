using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
    public Camera depthCamera;
    public bool drawDebugRange;

    private DirectionBoard[] directionBoards;
    private DirectionBoard viewedBoard;
    private bool[] isViewdBoardTable;
    private bool isUpdateBoard;
    private Color lastDirectionColor = Color.white;

    void Start()
    {
        viewedBoard = null;
        isUpdateBoard = false;
    }

    public void EpisodeInit()
    {
        directionBoards = FindObjectsOfType<DirectionBoard>();
        isViewdBoardTable = new bool[directionBoards.Length];

        for (int i = 0; i < directionBoards.Length; ++i)
        {
            isViewdBoardTable[i] = false;
        }

        viewedBoard = null;
        isUpdateBoard = false;
    }

    /* <summary>
     * find the closest direction board and update the viewed board
     * if the board is in the view range and not viewed yet
     * then update the viewed board
     */
    public void UpdateDirection()
    {
        //Debug.Log("UpdateDirection()");
        float distance = 999f;
        DirectionBoard closingBoard = null;
        int n = -1;

        if (drawDebugRange) DrawDebugViewRange();
        if (directionBoards == null)
        {
            Debug.Log("direction board empty.");
        }
        for (int i = 0; i < directionBoards.Length; ++i)
        {
            if (directionBoards[i].IsClose(transform.position) && CheckBoardInView(directionBoards[i]) &&
                Vector3.Distance(directionBoards[i].transform.position, transform.position) < distance)
            {
                distance = Vector3.Distance(directionBoards[i].transform.position, transform.position);
                closingBoard = directionBoards[i];
                n = i;
            }
        }

        if (closingBoard && n != -1 && viewedBoard == null)
        {
            UpdateViewedBoard(closingBoard); // viewdBoard = closingBoard;
            isViewdBoardTable[n] = true;
            isUpdateBoard = true;
        }
        else if (closingBoard && n != -1 && !isViewdBoardTable[n])
        {
            UpdateViewedBoard(closingBoard);
            isViewdBoardTable[n] = true;
            isUpdateBoard = true;
        }
    }

    public void ResetViewedBoard()
    {
        for (int i = 0; i < isViewdBoardTable.Length; ++i)
        {
            if (viewedBoard && directionBoards[i] != viewedBoard)
            {
                isViewdBoardTable[i] = false;
            }
        }
    }

    public bool IsUpdateBoard()
    {
        return isUpdateBoard;
    }

    public void SwitchUpdateBoardFlag()
    {
        isUpdateBoard = !isUpdateBoard;
    }

    public Vector3 GetDirection()
    {
        return viewedBoard ? viewedBoard.GetDirection() : Vector3.zero;
    }

    //* <summary>
    //* if lastDirctionColor is open, then will return the color of the viewed board in last episode
    //* </summary>  
    public Color GetDirectionColor()
    {
        return viewedBoard ? viewedBoard.color : Color.white;
    }

    public DirectionLabel GetDirectionLabel()
    {
        return viewedBoard ? viewedBoard.directionLabel : DirectionLabel.None;
    }

    public Vector3 TargetDirectionBoard()
    {
        if (viewedBoard == null) return GetDirection();

        //Debug.Log("viewed label Board : " + viewedBoard.GetLabel());

        if (viewedBoard.GetLabel() == DirectionLabel.Target)
        {
            Vector3 agentTargetDirection = GetTargetDirection();
            agentTargetDirection = agentTargetDirection.normalized;
            return agentTargetDirection;
        }
        else
        {
            return GetDirection();
        }
    }

    public Vector3 GetTargetDirection()
    {
        Vector3 targetPosition = viewedBoard.GetDirectionBoardPosition();

        Vector3 agentPosition = transform.position;

        Vector3 agentTargrtDirection = targetPosition - agentPosition;
        agentTargrtDirection.y = 0;

        return agentTargrtDirection;
    }

    public Vector3 GetDirectionBoardPosition()
    {
        Vector3 directionboardPosision = viewedBoard.GetDirectionBoardPosition();
        directionboardPosision.y = 0;
        return directionboardPosision;
    }

    public int GetStepRewardNum()
    {
        return viewedBoard ? viewedBoard.GetStepRewardNum() : 0;
    }

    private bool CheckBoardInView(DirectionBoard board)
    {
        List<Vector3> checkPositions = board.GetCheckPointPositions();

        for (int i = 0; i < checkPositions.Count; ++i)
        {
            Vector3 screenPoint = depthCamera.WorldToViewportPoint(checkPositions[i]);
            if (screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.z > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateViewedBoard(DirectionBoard closingBoard)
    {
        viewedBoard = closingBoard;
    }

    private void DrawDebugViewRange()
    {
        Vector3 p = depthCamera.ViewportToWorldPoint(new Vector3(0.35f, 0.5f, depthCamera.nearClipPlane));
        Vector3 v1 = (p - transform.position).normalized * 7 * (2 / Mathf.Sqrt(3));

        p = depthCamera.ViewportToWorldPoint(new Vector3(0.65f, 0.5f, depthCamera.nearClipPlane));
        Vector3 v2 = (p - transform.position).normalized * 7 * (2 / Mathf.Sqrt(3));

        Debug.DrawLine(transform.position, transform.position + v1, Color.red, 0.05f);
        Debug.DrawLine(transform.position, transform.position + v2, Color.red, 0.05f);
        Debug.DrawLine(transform.position + v1, transform.position + v2, Color.red, 0.05f);
    }
}
