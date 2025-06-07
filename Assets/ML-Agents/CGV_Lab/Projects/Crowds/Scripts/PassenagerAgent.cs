using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class PassenagerAgent : Agent
{
    public DepthCamera viewCamera;
    public bool mainAgent;
    public bool isInitialPosition;
    public GameObject initialPositionList;
    public InteriorPositionInit interiorPositionInit;
    public bool isInitialCondition;
    public int episodeLength;

    [SerializeField]
    private AgentController character;
    private EpisodeRunner epiRunner;
    private TargetArea[] targetAreas;
    //private float lineThickness = 0.1f;
    private float moveSpeed = 1.8f;

    // wang
    public RayPerceptionSensorComponent3D rayPerceptionSensorComponent3D;

    private bool inference = true; // replay
    private int step = 0;
    private int episode = 0;
    private int totalEpisode = 0;
    private int initialMaxStep;
    //private int minStep = 3000;
    private int successCount = 0;
    private int countBase = 10;

    // memory
    private List<Vector2> actionMemory = new List<Vector2>();
    private List<Vector2> translateMemory = new List<Vector2>();
    private List<Vector3> positionMemory = new List<Vector3>();
    private int memorySize = 10;
    private int memoryCounter = 0;
    private int memoryForgetNum = 30;
    private float memoryDiscount = 0.95f;
    private Matrix4x4 lastLocalMatrix;

    // reward
    private float collisionDistance = 999f;
    private float collisionReward = 0;              // WHY this reward is not used?
    private Vector3 lastPosition_Reward;

    // initial condition
    private bool restart = false;
    private bool checkMove = true;
    private Vector3 checkPosition;
    private Vector3 lastInitPosition = Vector3.zero;
    private Quaternion lastInitRotation = Quaternion.Euler(0, 0, 0);

    // exploration
    private bool[,] viewedTable;
    private int gridBlockSize = 5;
    private Vector2 sceneMin;
    private Vector2 sceneSize;

    public Vector3 action_tmp = Vector3.zero;
    public Vector3 direction_tmp = Vector3.zero;
    public Vector3 forward_tmp = Vector3.zero;

    //private Vector3 trejectoryLastPosition = Vector3.zero;

    private bool isTargetLabel = false;

    public bool isDrawCircle = false;

    private LineRenderer lineRenderer;
    private Color directionBoardColor = Color.clear;

    private float arrowLength = 1f;
    private float lineThickness = 0.2f;
    private float agentHeight = 2f;

    private Replayer replayer;
    private FailAgent failAgent;

    public bool addNPCInfo = false;
    private CheckRay checkRay;
    private int stepCount = 0;
    public bool isRotate = false;

    public bool distance = false;
    public float distanceTreshold = 1f;
    public int lastStepThreshold = 5;
    public float penalty = -0.1f;
    private int lastStep = 0;
    private bool isMove = false;

    public bool mixInitPos = false;
    public int mixRatio = 0;
    private bool isFailAgentInitPos = true;

    public bool useRealData = false;

    private Vector3 lastPos;
    private float lastTime;
    private static HashSet<int> usedInitIndices = new HashSet<int>(); // 避免重複使用的位置 index

    public bool isFall = false;
    public float fallPenalty = -1f;    // 懲罰值
    public float fallRayThreshold = 2f; // 當 agent 距離地面超過這個值，視為跳樓


    void Start()
    {
        //Debug.Log("PassenagerAgent Start");
        if (mainAgent)
        {
            Random.InitState(0);
        }

        if (isDrawCircle)
        {
            // Check if a LineRenderer component already exists
            lineRenderer = GetComponent<LineRenderer>();

            // If not, add one dynamically
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
        }

        if (mixInitPos && mixRatio == 0) mixRatio = 1;

        initialMaxStep = MaxStep;

        lastPos = transform.position;
        lastTime = Time.fixedTime;

        //Debug.Log("=== Transform Info ===");
        //Debug.Log($"Position: {transform.position}");
        //Debug.Log($"Rotation (Euler): {transform.eulerAngles}");
        //Debug.Log($"Forward: {transform.forward}");
        //LogLocalToWorldMatrix();
    }
    void Update()
    {
        // color
        if (isDrawCircle)
        {
            directionBoardColor = viewCamera.GetDirectionColor();
            DrawCircleShape(transform.forward, directionBoardColor);
        }
    }

    void FixedUpdate()
    {
        if (failAgent != null && failAgent.enabled && failAgent.load)
        {
            if (stepCount >= MaxStep - 100)  // reach maxStep but not reach targetArea
            {
                //Debug.Log("end");
                checkFunction(false);
                //episode++;
                //checkFunction(true);
                EndEpisode();
            }
        }

        if (failAgent != null && failAgent.enabled && failAgent.checkRay)
        {
            if (stepCount >= MaxStep - 100)  // reach maxStep but not reach targetArea
            {
                //Debug.Log("end");
                checkRay.EpisodeInit();
                EndEpisode();
            }
        }
        stepCount++;
        //Debug.Log($"stepCount: {stepCount}");

        //float dt = Time.fixedTime - lastTime;
        //float dist = Vector3.Distance(transform.position, lastPos);
        //float speed = dist / dt;

        //Debug.Log($"[FixedUpdate 測速] dist: {dist:F3}, dt: {dt:F3}, speed: {speed:F3} m/s");

        //lastPos = transform.position;
        //lastTime = Time.fixedTime;
        //SnapToGround();
    }

    private void DrawCircleShape(Vector3 direction, Color color)
    {
        int segments = 30;  // 圓形的細緻程度 (點數)
        float radius = arrowLength * 0.1f;  // 圓的半徑
        Vector3 center = transform.position + new Vector3(0, agentHeight, 0);

        lineRenderer.positionCount = segments + 1;  // 多一個點以閉合圓形

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 point = new Vector3(center.x + x, center.y, center.z + z);
            lineRenderer.SetPosition(i, point);
        }

        // 設定顏色
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    public override void Initialize()
    {
        //Debug.Log("PassenagerAgent Initialize");

        character = GetComponent<AgentController>();
        replayer = GetComponent<Replayer>();
        epiRunner = FindObjectOfType<EpisodeRunner>();
        targetAreas = FindObjectsOfType<TargetArea>();
        failAgent = FindObjectOfType<FailAgent>();
        if (addNPCInfo || failAgent != null && failAgent.enabled && (failAgent.save || failAgent.checkRay))
        {
            checkRay = GetComponent<CheckRay>();
        }
        //txtWriter = GetComponent<TXTWriter>();

        lastPosition_Reward = character.transform.position;
        checkPosition = character.transform.position;

        sceneMin = new Vector2(-44, -2.5f);  // -45, -3
        sceneSize = new Vector2(46, 46);  // 48, 48

        GenerateExploGrid();
        ResetTable();
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Debug.Log($"OnActionReceived: {stepCount}");

        //Debug.Log("inference : " + inference);
        if (!inference) return;

        TakeActions(actionBuffers);
    }

    // Collect the observation information in the step
    public override void CollectObservations(VectorSensor sensor)
    {
        //Debug.Log($"CollectObservations: {stepCount}");

        UpdateReward();

        viewCamera.UpdateDirection();

        if (viewCamera.IsUpdateBoard())
        {
            viewCamera.SwitchUpdateBoardFlag();
            memoryCounter = 0;
            memoryForgetNum = viewCamera.GetStepRewardNum();
        }

        // clchen
        if (addNPCInfo)
        {
            Vector3[] npcMovements = checkRay.GetNPCMovementVectors();
            //if (npcMovements.Count() != 15)  Debug.Log($"size: {npcMovements.Count()}");
            foreach (Vector3 movement in npcMovements)
            {
                sensor.AddObservation(new Vector2(movement.x, movement.z)); // 15*2
            }
        }

        //// Add the direction of the direction board in the observation
        Vector3 direction = viewCamera.TargetDirectionBoard();
        if (!useRealData)
        {
            sensor.AddObservation(direction);                  //1*3 
            sensor.AddObservation(transform.forward);   //1*3
        }
        //Debug.Log($"sensor step: {step}, direction : {direction}");
        //Debug.Log($"sensor step: {step}, forward : {transform.forward}");

        // action memory
        for (int i = 0; i < memorySize; ++i)
        {
            //Debug.Log(i);
            sensor.AddObservation(actionMemory[i]);     // 10*2
            if (i < memorySize - 1) actionMemory[i] = actionMemory[i + 1];
        }

        // translate memory
        Vector3 translate = Vector3.zero;
        for (int i = 0; i < memorySize - 1; ++i)
        {
            translate = transform.localToWorldMatrix.inverse * (positionMemory[i + 1] - positionMemory[i]);
            sensor.AddObservation(new Vector2(translate.x, translate.z)); //9*2
            //Debug.Log($"step: {step}, translate before: {positionMemory[i + 1] - positionMemory[i]}, after : {translate}");
        }
        translate = transform.localToWorldMatrix.inverse * (transform.position - positionMemory[memorySize - 1]);
        sensor.AddObservation(new Vector2(translate.x, translate.z)); //1*2

        // distance memory
        for (int i = 0; i < memorySize - 1; ++i)
        {
            sensor.AddObservation(Vector3.Distance(positionMemory[i + 1], positionMemory[i])); //9
            positionMemory[i] = positionMemory[i + 1];
        }
        sensor.AddObservation(Vector2.Distance(transform.position, positionMemory[memorySize - 1])); //1
        positionMemory[memorySize - 1] = transform.position;

        if (useRealData)
        {
            sensor.AddObservation(transform.forward);   //1*3
            sensor.AddObservation(direction);           //1*3 
        }
        sensor.AddObservation((float)memoryCounter / episodeLength); //1

        memoryCounter++;
        memoryCounter = Mathf.Min(memoryCounter, episodeLength);
        if (memoryCounter > 10)
        {
            //if (isDrawCircle)
            //{
            //    directionBoardColor = Color.white;
            //}
            viewCamera.ResetViewedBoard();
        }

        direction_tmp = direction;
        forward_tmp = transform.forward;
    }

    private void LogLocalToWorldMatrix()
    {
        Matrix4x4 m = transform.localToWorldMatrix;
        Debug.Log("transform.localToWorldMatrix:");
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"{m[i, 0]:F4}\t{m[i, 1]:F4}\t{m[i, 2]:F4}\t{m[i, 3]:F4}");
        }
    }


    public override void OnEpisodeBegin()
    {
        //Debug.Log("PassenagerAgent OnEpisodeBegin");
        if (mainAgent) InitBoards();        // intialization order should not change
        if (mainAgent && interiorPositionInit) interiorPositionInit.EpisodeInit();

        if (isInitialPosition) InitPosition();

        if (mainAgent) epiRunner.EpisodeInit(transform.position);
        viewCamera.EpisodeInit();
        character.EpisodeInit();

        if (mainAgent) usedInitIndices.Clear();

        memoryCounter = 0;
        collisionReward = 0;
        collisionDistance = 999;
        step = 0;
        stepCount = 0;

        isTargetLabel = false;

        lastLocalMatrix = transform.localToWorldMatrix;
        actionMemory.Clear();
        translateMemory.Clear();
        positionMemory.Clear();
        for (int i = 0; i < memorySize; ++i)
        {
            actionMemory.Add(Vector2.zero);
            translateMemory.Add(Vector2.zero);
            positionMemory.Add(transform.position);
        }

        if (distance)
        {
            lastStep = 0;
            isMove = true;
        }

        //trejectoryLastPosition = transform.position;

        ResetTable();
    }

    private void InitBoards()
    {
        DirectionBoard[] boards = FindObjectsOfType<DirectionBoard>();
        foreach (DirectionBoard board in boards)
        {
            board.EpisodeInit();
        }
    }

    public void ReplayUpdate()
    {
        viewCamera.UpdateDirection();

        if (viewCamera.IsUpdateBoard())
        {
            viewCamera.SwitchUpdateBoardFlag();
            memoryCounter = 0;
            memoryForgetNum = viewCamera.GetStepRewardNum();
        }

        memoryCounter++;
        if (memoryCounter > 10) viewCamera.ResetViewedBoard();
    }

    public Color GetDirectionColor()
    {
        return viewCamera.GetDirectionColor();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        Vector3 moveVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        moveVector.x = Mathf.Clamp(moveVector.x, -1, 1);
        moveVector.z = Mathf.Clamp(moveVector.z, 0, 1);

        continuousActionsOut[0] = moveVector.x;
        continuousActionsOut[1] = moveVector.z;
    }

    public void TakeActions(ActionBuffers actionBuffers)
    {
        var actionX = actionBuffers.ContinuousActions[0];
        var actionZ = actionBuffers.ContinuousActions[1];
        var moveVector = new Vector3(actionX, 0, actionZ);

        actionMemory[actionMemory.Count - 1] = new Vector2(actionX, actionZ);

        moveVector.x = Mathf.Clamp(moveVector.x, -1f, 1f);
        moveVector.z = Mathf.Clamp(moveVector.z, 0, 1);
        action_tmp = moveVector;
        //Debug.Log($"step: {step}, x: {moveVector.x}, z: {moveVector.z}");
        character.Move(moveVector, moveSpeed);

        if (step % 5 == 0) CheckMoveDistance();

        step++;
    }

    public void SetInference(bool isInference)
    {
        inference = isInference;
    }

    /*<summary> 
     * call this function every 5 steps
     * if the Decision Requester set too low, that mean the low academy step the agent will rqust a action.
     * so it may let this fumction call too fast, and the agent will be determined not move.
     */
    private void CheckMoveDistance()
    {
        //Debug.Log("Step" + step);
        if (step == 0)
        {
            //Debug.Log("step is 0");
            checkPosition = transform.position;
        }
        else
        {
            if (Vector3.Distance(checkPosition, transform.position) < 1)
            {
                //Debug.Log($"Agent is not moving, step: {step}");
                //Debug.Log("checkPosition: " + checkPosition);
                //Debug.Log("transform.position: " + transform.position);
                checkMove = false;
            }

            if (distance)
            {
                float moveDistance = Vector3.Distance(checkPosition, transform.position);
                if (moveDistance < distanceTreshold)
                {
                    //Debug.Log($"moveDistance: {moveDistance}");
                    if (lastStep >= lastStepThreshold)
                    {
                        isMove = false;
                        Debug.Log($"notMove, step: {step}");
                    }
                    lastStep++;
                    //Debug.Log($"lastStep: {lastStep}");
                }
                else
                {
                    lastStep = 0;
                }
            }

            checkPosition = transform.position;
        }
    }

    /*<summary>
     * set the agent initial position and rotation
     * if lastInitPosition is zero, set the initial position and rotation to the current position and rotation (first epoisde)
     * if the agent checkMove is false, set the initial position and rotation to the lastInitPosition and lastInitRotation(restart epoisde)
     * else, set the initial position and rotation to the random position and rotation(other episodes)
     */
    private void InitPosition()
    {
        if (lastInitPosition == Vector3.zero)
        {
            lastInitPosition = gameObject.transform.position;
            lastInitRotation = gameObject.transform.rotation;
        }

        // 用來load fail agent的初始位置
        if (failAgent != null && failAgent.enabled && failAgent.load && isFailAgentInitPos)
        {
            //Debug.Log($"failagnet init, totalEpisode: {totalEpisode}");
            InitializeFailAgent();
        }
        else
        {
            //Debug.Log($"success agent init, totalEpisode: {totalEpisode}");
            if (isInitialCondition && (restart || !checkMove))
            {
                Debug.Log("restart last eposide");
                //int num = replayer.ParseName();
                //Debug.Log(num);
                gameObject.transform.position = lastInitPosition;
                gameObject.transform.rotation = lastInitRotation * Quaternion.Euler(0, Random.Range(-0.05f, 0.05f), 0);
            }
            else
            {
                RandomScenePosition();

                lastInitPosition = gameObject.transform.position;
                lastInitRotation = gameObject.transform.rotation;
            }
        }

        if (replayer == null)
        {
            Debug.Log("replayer is null.");
        }

        lastPosition_Reward = character.transform.position;
        restart = true;
        checkMove = true;

        //SnapToGround();
    }

    private void SnapToGround()
    {
        // 從角色上方打一條往下的 Ray，量到最近地面
        if (Physics.Raycast(transform.position + Vector3.up * 1f,
                            Vector3.down,
                            out RaycastHit hit,
                            5f,
                            LayerMask.GetMask("Default", "Ground")))   // 視專案調整
        {
            // 將角色 y 設到碰撞點 (保留 xz)
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
        }
    }

    private void RandomScenePosition()
    {
        int idx = GetUniqueRandomIndex(initialPositionList.transform.childCount);
        Vector3 randPos = initialPositionList.transform.GetChild(idx).position;

        gameObject.transform.position = randPos;
        //gameObject.transform.position = new Vector3(99.5599976f, 1.63f, -61.1199989f);  // 一樓斜
        //gameObject.transform.position = new Vector3(92.2f, 5.58f, -51.6f);  // 二樓綠色右上
        //gameObject.transform.position = new Vector3(81.5999985f, 9.57999992f, -44.2599983f); // 可
        //gameObject.transform.position = new Vector3(86.4400024f, 11.3500004f, -40.2799988f);
        //gameObject.transform.position = new Vector3(37.8f, 1.63f, -65f);  // 一樓斜
        gameObject.transform.rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);
    }

    private int GetUniqueRandomIndex(int maxCount)
    {
        int tries = 0;
        int idx;
        do
        {
            idx = Random.Range(0, maxCount);
            tries++;
        } while (usedInitIndices.Contains(idx) && tries < 100);
        //Debug.Log($"tries: {tries}");

        usedInitIndices.Add(idx);
        return idx;
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (TargetArea targetArea in targetAreas)
        {
            if (collision.gameObject == targetArea.gameObject)
            {
                SetReward(50);

                restart = false;
                successCount++;
                episode++;
                totalEpisode++;

                checkFunction(true);
                if (failAgent != null && failAgent.enabled && failAgent.save)
                {
                    failAgent.NotifyAgentReachedTarget(this);
                }

                if (failAgent != null && failAgent.enabled && failAgent.checkRay)
                {
                    checkRay.EpisodeInit();
                }

                if (addNPCInfo && checkRay != null && checkRay.enabled)
                {
                    checkRay.EpisodeInit();
                }
                //EndEpisode();                     // training
                gameObject.SetActive(false);    // testing
            }
        }
    }

    private void checkFunction(bool isTargetArea)
    {
        if (failAgent != null && failAgent.enabled && failAgent.load)
        {
            if (mixInitPos)
            {
                if (isFailAgentInitPos)
                {
                    if (episode == failAgent.trainingIterationsPerAgent)
                    {
                        failAgent.ResetAllNpc(true, isTargetArea); // next training agent
                        episode = 0;
                        isFailAgentInitPos = false;
                    }
                    else failAgent.ResetAllNpc(false, isTargetArea);
                }
                else
                {
                    if (episode == mixRatio)
                    {
                        episode = 0;
                        isFailAgentInitPos = true;
                    }
                }
            }
            else
            {
                if (episode == failAgent.trainingIterationsPerAgent)
                {
                    Debug.Log($"checkFunction episode: {episode}");
                    failAgent.ResetAllNpc(true, isTargetArea); // next training agent
                    episode = 0;
                }
                else failAgent.ResetAllNpc(false, isTargetArea);
            }
        }
    }

    // used to handle the reward during the collision(each frame)
    void OnTriggerStay(Collider collider)
    {
        Vector3 agentHeight = new Vector3(0, 0.8f, 0);
        float hummanWeight = 0.5f, obstacleWeight = 2f;

        if (collider.gameObject.tag != "CGV_Ground" && collider.gameObject.tag != "CGV_Goal")
        {
            float distance = Vector3.Distance(collider.ClosestPoint(transform.position + agentHeight), transform.position + agentHeight);

            if (distance < collisionDistance && distance != 0)
            {
                if (collider.gameObject.tag == "CGV_Crowd")
                {
                    collisionReward = -1 / distance * hummanWeight;
                }
                else
                {
                    collisionReward = -1 / distance * obstacleWeight;
                }

                collisionDistance = distance;
            }
        }
    }
    private void UpdateReward()
    {
        Vector3 currentPosition = character.transform.position;
        Vector3 moveVector = currentPosition - lastPosition_Reward;
        Vector3 direction = viewCamera.TargetDirectionBoard();
        DirectionLabel directionLabel = viewCamera.GetDirectionLabel();
        if (directionLabel == DirectionLabel.Target) isTargetLabel = true;
        //Debug.Log($"123 UpdateReward step: {step}, directionLabel: {directionLabel}, direction: {direction}, moveVector: {moveVector}");

        //// clchen
        //To encouage the agent to move in the direction of the direction board
        if (direction != Vector3.zero && moveVector != Vector3.zero && moveVector.magnitude > 0.01f)
        {
            if (directionLabel != DirectionLabel.Target && memoryCounter <= memoryForgetNum || isTargetLabel)
            {
                float directionAngle = Vector3.Angle(direction, moveVector);
                float reward = Mathf.Pow(memoryDiscount, memoryCounter - 1) * (-directionAngle / 90.0f + 1);
                AddReward(reward);
                //Debug.Log($"step: {step}, isTargetLabel: {isTargetLabel}, memoryCounter: {memoryCounter}");
            }
        }

        float explorationReward = GetExplorationReward();
        //Debug.Log("GetExplorationReward : " + explorationReward);

        AddReward(explorationReward);

        //if (step > 100)
        //{
        //    AddReward(-1);
        //}
        //else
        //{
        //    AddReward(-0.01f);
        //}
        //AddReward(-0.01f);

        if (distance && !isMove)
        {
            AddReward(penalty);
            isMove = true;
        }

        // --- 跳樓偵測 (Raycast 版本) ---
        if (isFall)
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;  // 稍微從上方開始射線，避免碰到自身 collider
            float rayMaxDistance = 10f;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayMaxDistance))
            {
                float groundDistance = hit.distance;
                if (groundDistance > fallRayThreshold)
                {
                    AddReward(fallPenalty);
                    //Debug.Log($" Jumped off detected by Raycast! Distance = {groundDistance}");
                }
            }
        }

        collisionReward = 0;
        collisionDistance = 999;
        lastPosition_Reward = currentPosition;
    }

    private Vector3 RandomInitPosition(Vector2 posMin, Vector2 xRange, Vector2 zRange)
    {
        Vector3 position = new Vector3(posMin.x, 0.05f, posMin.y);
        Vector3 randPos = new Vector3(0, 0, 0);

        float collisionRadius = 0.5f;
        int sampleMax = 100;
        int sampleCount = 0;
        bool check = false;

        // check whether the initial position is legal.
        do
        {
            sampleCount++;
            if (sampleCount > sampleMax)
            {
                randPos = Vector3.zero;
                break;
            }

            check = false;

            randPos = new Vector3(Random.Range(xRange.x, xRange.y), 0, Random.Range(zRange.x, zRange.y));

            Collider[] hitColliders = Physics.OverlapSphere(position + randPos, collisionRadius);
            for (int i = 0; i < hitColliders.Length; ++i)
            {
                if (hitColliders[i].tag != "CGV_Crowd" && hitColliders[i].tag != "CGV_Ground" && hitColliders[i].tag != "CGV_Expert"
                    && hitColliders[i].tag != "CGV_Range"
                    && hitColliders[i].tag != "CGV_tmp_l" && hitColliders[i].tag != "CGV_tmp_r"
                    && hitColliders[i].tag != "CGV_tmp_u" && hitColliders[i].tag != "CGV_tmp_d")
                {
                    check = true;
                    break;
                }
            }
        } while (check);

        return position + randPos;
    }

    private void ResetTable()
    {
        //Debug.Log($"get viewed table length: {viewedTable.GetLength(0)} x {viewedTable.GetLength(1)}");
        for (int i = 0; i < viewedTable.GetLength(0); ++i)
        {
            for (int j = 0; j < viewedTable.GetLength(1); ++j)
            {
                //Debug.Log($"reset viewed table: {i}, {j}");
                viewedTable[i, j] = false;
            }
        }
    }

    private float GetExplorationReward()
    {
        //Debug.Log("GetExplorationReward");
        int idx_i = (int)(Mathf.Abs((transform.position.x - sceneMin.x) / gridBlockSize));
        int idx_j = (int)(Mathf.Abs((transform.position.z - sceneMin.y) / gridBlockSize));

        //Debug.Log($"Calculated indices: idx_i = {idx_i}, idx_j = {idx_j}");
        //Debug.Log($"viewedTable dimensions: {viewedTable.GetLength(0)} x {viewedTable.GetLength(1)}");

        if (idx_i >= viewedTable.GetLength(0) || idx_j >= viewedTable.GetLength(1))
        {
            //Debug.Log($"step: {step}, Indices out of range");
            return 0f;
        }
        //Debug.Log($"viewedTable[{idx_i}, {idx_j}] before check: {viewedTable[idx_i, idx_j]}");

        if (!viewedTable[idx_i, idx_j])
        {
            viewedTable[idx_i, idx_j] = true;
            //Debug.Log($"viewedTable[{idx_i}, {idx_j}] set to true");
            //DrawCrosses(viewedTable.GetLength(0), viewedTable.GetLength(1)); // Draw crosses after update
            //Debug.Log($"step: {step}, Returning 0.15f");
            return 0.15f;
        }

        return 0f;
    }

    public bool GetInference()
    {
        return inference;
    }

    private void InitializeFailAgent()
    {
        if (replayer != null && replayer.enabled)
        {
            //Debug.Log("Position : " + replayer.GetFailAgentStartPosition());
            //Debug.Log("Rotation : " + replayer.GetFailAgentStartRotation());
            //Debug.Log("Direction : " + replayer.GetFailAgentStartDirection());
            gameObject.transform.position = replayer.GetFailAgentStartPosition();

            gameObject.transform.rotation = replayer.GetFailAgentStartRotation();
            if (restart && isRotate) gameObject.transform.rotation *= Quaternion.Euler(0, Random.Range(-0.05f, 0.05f), 0);

            direction_tmp = replayer.GetFailAgentStartDirection();
        }
    }

    private float CalculateEpisodeMaxStep(float decayRate)
    {
        return Mathf.Exp(-decayRate * episode);
    }


    private void DrawDebugLine(Vector3 start, Vector3 end)
    {
        float duration = 600.0f;
        Color color = GetDirectionColor();
        if (direction_tmp == Vector3.zero)
        {
            Debug.DrawLine(start, end, color, duration);
        }
        else
        {
            Debug.DrawLine(start, end, color, duration);
        }
    }

    private void GenerateExploGrid()
    {
        //* Set the table size 
        int blockNum_x = (int)(Mathf.Abs(sceneSize.x / gridBlockSize + 1)), blockNum_y = (int)(Mathf.Abs(sceneSize.y / gridBlockSize + 1));
        viewedTable = new bool[blockNum_x, blockNum_y];

        //DrawExplorationGrid(blockNum_x, blockNum_y);
    }

    private float GetSuccessRate()
    {
        return (float)successCount / countBase;
    }

    private void DrawExplorationGrid(int blockNum_x, int blockNum_y)
    {
        Vector3 p = new Vector3(sceneMin.x, 0.5f, sceneMin.y);
        for (int i = 0; i < blockNum_x; ++i)
        {
            for (int j = 0; j < blockNum_y; ++j)
            {
                Vector3 start = p + new Vector3(i * gridBlockSize, 0, j * gridBlockSize);

                for (int time = 0; time < 5; time++)    // draw the color deeper
                {
                    Debug.DrawLine(start, start + new Vector3(gridBlockSize, 0, 0), Color.red, 300f);
                    Debug.DrawLine(start, start + new Vector3(0, 0, gridBlockSize), Color.red, 300f);
                }
            }
        }

        for (int time = 0; time < 5; time++)
        {
            Debug.DrawLine(p + new Vector3(blockNum_x, 0, 0) * gridBlockSize, p + new Vector3(blockNum_x, 0, blockNum_y) * gridBlockSize, Color.red, 300f);
            Debug.DrawLine(p + new Vector3(0, 0, blockNum_y) * gridBlockSize, p + new Vector3(blockNum_x, 0, blockNum_y) * gridBlockSize, Color.red, 300f);
        }
    }

    private void DrawCrosses(int blockNum_x, int blockNum_y)
    {
        Vector3 p = new Vector3(sceneMin.x, 0.5f, sceneMin.y);
        float duration = 60f;

        for (int i = 0; i < blockNum_x; ++i)
        {
            for (int j = 0; j < blockNum_y; ++j)
            {
                if (viewedTable[i, j])
                {
                    Vector3 start = p + new Vector3(i * gridBlockSize, 0, j * gridBlockSize);
                    Vector3 bottomLeft = start;
                    Vector3 topRight = start + new Vector3(gridBlockSize, 0, gridBlockSize);
                    Vector3 topLeft = start + new Vector3(0, 0, gridBlockSize);
                    Vector3 bottomRight = start + new Vector3(gridBlockSize, 0, 0);

                    Debug.DrawLine(bottomLeft, topRight, Color.blue, duration);
                    Debug.DrawLine(topLeft, bottomRight, Color.blue, duration);
                }
            }
        }
    }
    public void ResetAgent()
    {
        gameObject.SetActive(true); // 確保物件啟用
        if (failAgent != null && failAgent.enabled && failAgent.save)
        {
            if (replayer != null && replayer.enabled)
            {
                replayer.EpisodeInit();
            }
            if (checkRay != null && checkRay.enabled)
            {
                checkRay.EpisodeInit();
            }
        }
    }
    public void AddPenalty(float penalty)
    {
        AddReward(penalty);
    }
}