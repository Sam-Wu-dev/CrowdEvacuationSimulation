using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

public class NPCController : MonoBehaviour
{
    private const float detectionRadius = 6f;
    private const float goalDetectionRadius = 7f;
    private const float moveDistance = 3f;
    private const float moveStraightDistance = 7f;
    private const float overlapDistance = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private GameObject[] directionMarkers;
    private GameObject[] goalMarkers;

    private Vector3 previousPosition;
    private GameObject previousMarker;
    private GameObject currentGoalMarker;

    private LineRenderer lineRenderer;
    private LineRenderer arrowHeadLeftRenderer;
    private LineRenderer arrowHeadRightRenderer;
    private Color directionBoardColor = Color.clear;

    private Vector3 targetPosition = Vector3.zero;
    private const float walkSpeed = 0.5f;
    private const float rotationSpeed = 1.5f;

    private const float arrowLength = 0.25f;
    private const float lineThickness = 0.1f;
    private float agentHeight = 1.0f;

    private const float speedThreshold = 0.5f;
    private const float positionThreshold = 1.0f;
    private const float checkInterval = 3.0f;
    private const int collisionThreshold = 3;

    private float timeSinceLastCheck = 0.0f;
    private int collisionCount = 0;

    private bool isActive = true;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = directionBoardColor;
        lineRenderer.endColor = directionBoardColor;

        GameObject arrowHeadLeft = new GameObject("ArrowHeadLeft");
        arrowHeadLeft.transform.SetParent(transform);
        arrowHeadLeftRenderer = arrowHeadLeft.AddComponent<LineRenderer>();
        arrowHeadLeftRenderer.startWidth = lineThickness;
        arrowHeadLeftRenderer.endWidth = lineThickness;
        arrowHeadLeftRenderer.positionCount = 2;
        arrowHeadLeftRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arrowHeadLeftRenderer.startColor = directionBoardColor;
        arrowHeadLeftRenderer.endColor = directionBoardColor;

        GameObject arrowHeadRight = new GameObject("ArrowHeadRight");
        arrowHeadRight.transform.SetParent(transform);
        arrowHeadRightRenderer = arrowHeadRight.AddComponent<LineRenderer>();
        arrowHeadRightRenderer.startWidth = lineThickness;
        arrowHeadRightRenderer.endWidth = lineThickness;
        arrowHeadRightRenderer.positionCount = 2;
        arrowHeadRightRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arrowHeadRightRenderer.startColor = directionBoardColor;
        arrowHeadRightRenderer.endColor = directionBoardColor;

        animator = GetComponent<Animator>();
        animator.applyRootMotion = true;
        animator.SetFloat("Speed", walkSpeed);

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updatePosition = true;
        agent.avoidancePriority = Random.Range(0, 100);
        agent.stoppingDistance = 0.5f;
        agent.speed = walkSpeed;
        agentHeight = agent.height;

        directionMarkers = GameObject.FindGameObjectsWithTag("CGV_Direction")
            .Where(obj => obj.name.StartsWith("direction"))
            .ToArray();
        goalMarkers = GameObject.FindGameObjectsWithTag("CGV_Goal");

        previousPosition = transform.position;
    }

    private void Update()
    {
        if (IsGameOver())
        {
            return;
        }

        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            CheckIfStuck();
            timeSinceLastCheck = 0.0f;
        }

        GameObject nearestMarker = FindNearestMarker();
        // Check if the agent is near a goal marker.
        if (currentGoalMarker == null)
        {
            foreach (GameObject marker in goalMarkers)
            {
                if (IsInRange(marker, transform.position, goalDetectionRadius))
                {
                    currentGoalMarker = marker;
                    ResetPath();
                    GoTo(marker.transform.position);

                    if (nearestMarker != null)
                    {
                        DirectionBoard nearestMarkerDirectionBoard = nearestMarker.GetComponent<DirectionBoard>();
                        directionBoardColor = nearestMarkerDirectionBoard.color;
                    }
                    DrawArrowShape(transform.forward, directionBoardColor);

                    return;
                }
            }
        }
        else
        {
            GoTo(currentGoalMarker.transform.position);
            DrawArrowShape(transform.forward, directionBoardColor);

            return;
        }

        if (nearestMarker != null)
        {
            MoveThroughNearMarker(nearestMarker);
            DirectionBoard nearestMarkerDirectionBoard = nearestMarker.GetComponent<DirectionBoard>();
            directionBoardColor = nearestMarkerDirectionBoard.color;
        }
        else
        {
            if (targetPosition != Vector3.zero)
            {
                GoTo(targetPosition);
            }
            else
            {
                RandomMove();
            }
            directionBoardColor = Color.clear;
        }

        DrawArrowShape(transform.forward, directionBoardColor);
    }
    private GameObject FindNearestMarker()
    {
        Vector3 currentPosition = transform.position;

        GameObject nearestMarker = null;
        float minDistance = float.MaxValue;
        foreach (GameObject directionMarker in directionMarkers)
        {
            if (IsInRange(directionMarker, currentPosition, detectionRadius)) // Near a marker.
            {
                float distance = Vector3.Distance(transform.position, directionMarker.transform.position);
                if (distance < minDistance) // Find nearest marker.
                {
                    minDistance = distance;
                    nearestMarker = directionMarker;
                }
            }
        }
        
        return nearestMarker;
    }

    private void MoveThroughNearMarker(GameObject nearestMarker)
    {
        if (nearestMarker == null)
        {
            return;
        }

        if (previousMarker != nearestMarker) // Nearest marker is changed.
        {
            ResetPath();
        }
        previousMarker = nearestMarker;

        DirectionBoard directionBoard = nearestMarker.GetComponent<DirectionBoard>();
        DirectionLabel directionLabel = directionBoard.directionLabel;

        Vector3 direction = transform.forward;
        switch (directionLabel)
        {
            case DirectionLabel.Left:
                direction = Vector3.right;
                break;
            case DirectionLabel.Right:
                direction = Vector3.left;
                break;
            case DirectionLabel.Forward:
                direction = Vector3.back;
                break;
            case DirectionLabel.Back:
                direction = Vector3.forward;
                break;
        }

        direction = direction.normalized * moveStraightDistance;
        GoTo(nearestMarker.transform.position + direction);
    }

    private void CheckIfStuck()
    {
        bool isStuckByPosition = Vector3.Distance(previousPosition, transform.position) < positionThreshold;
        bool isStuckBySpeed = agent.velocity.magnitude < speedThreshold && agent.remainingDistance > agent.stoppingDistance;
        bool isStuckByCollision = collisionCount > collisionThreshold;

        if (isStuckByPosition || isStuckBySpeed || isStuckByCollision)
        {
            previousMarker = null;
            currentGoalMarker = null;
            ResetPath();
        }

        previousPosition = transform.position;
        collisionCount = 0;
    }

    private void RandomMove()
    {
        Vector3 randomDirection = Random.insideUnitSphere * moveDistance;
        randomDirection.y = 0f;

        if (NavMesh.SamplePosition(transform.position + randomDirection, out NavMeshHit hit, moveDistance, NavMesh.AllAreas))
        {
            if (!NavMesh.Raycast(transform.position, hit.position, out NavMeshHit hitRay, NavMesh.AllAreas))
            {
                GoTo(hit.position);
            }
        }
    }

    private void DrawArrowShape(Vector3 direction, Color color)
    {
        Vector3 startPoint = transform.position - direction * arrowLength;
        Vector3 endPoint = transform.position + direction * arrowLength;

        startPoint.y = agentHeight;
        endPoint.y = agentHeight;

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        Vector3 normDirection = (endPoint - startPoint).normalized;
        Vector3 perpendicular = Vector3.Cross(normDirection, Vector3.up) * lineThickness * 2;

        arrowHeadLeftRenderer.SetPosition(0, endPoint);
        arrowHeadLeftRenderer.SetPosition(1, endPoint - normDirection * lineThickness * 3 + perpendicular);

        arrowHeadRightRenderer.SetPosition(0, endPoint);
        arrowHeadRightRenderer.SetPosition(1, endPoint - normDirection * lineThickness * 3 - perpendicular);

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        arrowHeadLeftRenderer.startColor = color;
        arrowHeadLeftRenderer.endColor = color;

        arrowHeadRightRenderer.startColor = color;
        arrowHeadRightRenderer.endColor = color;
    }

    private void ResetPath()
    {
        targetPosition = Vector3.zero;
        directionBoardColor = Color.clear;
        DrawArrowShape(transform.forward, directionBoardColor);
        agent.ResetPath();
    }

    private void GoTo(Vector3 destination)
    {
        destination.y = 0f;

        if (Vector3.Distance(transform.position, targetPosition) <= agent.stoppingDistance)
        {
            ResetPath();
            return;
        }

        if (targetPosition == Vector3.zero)
        {
            targetPosition = destination;
            agent.SetDestination(destination);
        }

        if (targetPosition == destination)
        {
            Move();
        }

        //if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        //{
        //    SetDestination(destination);
        //}
    }

    private void Move()
    {
        Vector3 velocity = agent.velocity;
        float speed = velocity.magnitude;

        animator.SetFloat("Speed", speed);
        agent.speed = walkSpeed;

        if (agent.hasPath)
        {
            Vector3 nextPosition = agent.steeringTarget;
            Vector3 directionToTarget = nextPosition - transform.position;
            directionToTarget.y = 0;

            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, targetRotation) <= 0.00f)
                {
                    agent.Move(directionToTarget.normalized * agent.speed * Time.deltaTime);
                }
            }
        }

        //Vector3 velocity = agent.velocity;
        //float speed = velocity.magnitude;

        //animator.SetFloat("Speed", speed);

        //if (velocity != Vector3.zero)
        //{
        //    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), rotationSpeed * Time.deltaTime);
        //}

        //if (speed > walkSpeed)
        //{
        //    agent.speed = walkSpeed;
        //}

        //agent.SetDestination(destination);
    }

    private bool IsInRange(GameObject obj, Vector3 currentPosition, float distance)
    {
        Vector3 markerPosition = obj.transform.position;
        Vector3 direction = currentPosition - markerPosition;
        float objDistance = direction.magnitude;

        if (objDistance < overlapDistance) // Overlap with the marker.
        {
            return true;
        }
        else if (objDistance <= distance)
        {
            Ray ray = new Ray(markerPosition, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, objDistance);
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.transform.gameObject;
                if (!hitObject.CompareTag("CGV_Crowd"))
                {
                    return false;
                }

                if (hitObject == gameObject)
                {
                    return true;
                }
            }

            //RaycastHit hit;
            //if (Physics.Raycast(markerPosition, direction, out hit, objDistance))
            //{
            //    if (hit.transform.gameObject.CompareTag("CGV_Crowd")) // FIXME.
            //    {
            //        return true;
            //    }
            //}
        }

        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        gameObject.SetActive(false);
        isActive = false;
    }

    private bool IsGameOver()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (Collider col in colliders)
        {
            foreach (GameObject marker in goalMarkers)
            {
                if (col.gameObject == marker)
                {
                    gameObject.SetActive(false);
                    isActive = false;

                    return true;
                }
            }
        }

        return false;
    }

    private void OnCollisionStay(Collision collision)
    {
        collisionCount++;
    }

    private void OnCollisionExit(Collision collision)
    {
        collisionCount = 0;
    }

    public bool GetActive()
    {
        return isActive;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }
}