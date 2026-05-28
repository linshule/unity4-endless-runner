using UnityEngine;
using System.Collections.Generic;

public class AdaptiveDifficulty : MonoBehaviour
{
    public static AdaptiveDifficulty Instance;

    // === 表现追踪 ===
    public int recentRunsToTrack = 5;
    private List<float> recentSurvivalDistances = new List<float>();
    private float currentRunStartZ;

    // === 难度等级 ===
    public int difficultyLevel = 1;
    public int minDifficulty = 1;
    public int maxDifficulty = 10;

    // === 自适应阈值 ===
    public float highSkillThreshold = 2000f;
    public float lowSkillThreshold = 500f;

    // === 调节冷却 ===
    public float adjustmentCooldown = 30f;
    private float lastAdjustmentTime;

    // === 回溯保护 ===
    public float rewindProtectionDuration = 10f;
    private float rewindProtectionEnd;

    // === 列车安全区 ===
    public float trainDangerDistance = 30f;
    public float trainSafeDistance = 150f;

    // === 组件引用 ===
    private GameManager gameManager;
    private ObstacleSpawner obstacleSpawner;
    private TerrainCollapse terrainCollapse;
    private TrainController trainController;
    private PlayerController player;

    // === 难度参数基准 ===
    private float baseSpeed = 15f;
    private float baseMaxSpeed = 35f;
    private float baseTrainApproach = 2f;
    private float baseCollapseMaxInterval = 12f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        player = FindObjectOfType<PlayerController>();
        obstacleSpawner = FindObjectOfType<ObstacleSpawner>();
        terrainCollapse = FindObjectOfType<TerrainCollapse>();
        trainController = FindObjectOfType<TrainController>();

        // 记录基准参数
        if (player != null)
        {
            baseSpeed = player.baseSpeed;
            baseMaxSpeed = player.maxSpeed;
        }
        if (trainController != null)
        {
            baseTrainApproach = trainController.baseApproachRate;
        }
        if (terrainCollapse != null)
        {
            baseCollapseMaxInterval = terrainCollapse.maxCollapseInterval;
        }
        if (obstacleSpawner != null)
        {
            // 使用固定基准值，由 ApplyDifficulty() 中的 Lerp 控制
        }
    }

    void Update()
    {
        if (player == null || gameManager == null) return;

        // 检测新一局开始
        if (gameManager.state == GameState.Playing && !player.isDead)
        {
            // 新游戏开始时记录起点
            if (currentRunStartZ < 0.1f)
            {
                currentRunStartZ = player.transform.position.z;
            }
        }

        // 检测死亡（游戏结束时记录本次存活距离）
        if (gameManager.state == GameState.GameOver && currentRunStartZ > 0.1f)
        {
            float survivalDistance = player.transform.position.z - currentRunStartZ;
            RecordSurvival(survivalDistance);
            currentRunStartZ = 0f;

            // 立即评估
            EvaluateDifficulty();
        }

        // 检测回溯保护期
        if (rewindProtectionEnd > 0f && Time.time > rewindProtectionEnd)
        {
            rewindProtectionEnd = 0f;
        }

        // 游戏运行中，检查列车距离驱动的难度调整
        if (gameManager.state == GameState.Playing && trainController != null)
        {
            float distance = trainController.GetDistance();

            // 列车危险：暂停难度提升
            if (distance <= trainDangerDistance)
            {
                // 维持当前难度，不做提升
            }
            // 列车安全：加速难度追赶
            else if (distance >= trainSafeDistance)
            {
                if (Time.time - lastAdjustmentTime > 10f)
                {
                    IncreaseDifficulty();
                    lastAdjustmentTime = Time.time;
                }
            }
        }
    }

    void RecordSurvival(float distance)
    {
        recentSurvivalDistances.Add(distance);
        while (recentSurvivalDistances.Count > recentRunsToTrack)
        {
            recentSurvivalDistances.RemoveAt(0);
        }
    }

    void EvaluateDifficulty()
    {
        if (recentSurvivalDistances.Count < recentRunsToTrack) return;
        if (Time.time - lastAdjustmentTime < adjustmentCooldown) return;
        if (rewindProtectionEnd > Time.time) return;

        // 计算平均存活距离
        float total = 0f;
        for (int i = 0; i < recentSurvivalDistances.Count; i++)
        {
            total += recentSurvivalDistances[i];
        }
        float average = total / recentSurvivalDistances.Count;

        if (average > highSkillThreshold)
        {
            IncreaseDifficulty();
            lastAdjustmentTime = Time.time;
        }
        else if (average < lowSkillThreshold)
        {
            DecreaseDifficulty();
            lastAdjustmentTime = Time.time;
        }
    }

    void IncreaseDifficulty()
    {
        if (difficultyLevel >= maxDifficulty) return;
        difficultyLevel++;
        ApplyDifficulty();
    }

    void DecreaseDifficulty()
    {
        if (difficultyLevel <= minDifficulty) return;
        difficultyLevel--;
        ApplyDifficulty();
    }

    void ApplyDifficulty()
    {
        float t = (float)(difficultyLevel - 1) / (float)(maxDifficulty - 1); // 0~1

        // 1. 玩家速度
        if (player != null)
        {
            player.baseSpeed = Mathf.Lerp(baseSpeed, baseSpeed * 1.5f, t);
            player.maxSpeed = Mathf.Lerp(baseMaxSpeed, baseMaxSpeed * 1.5f, t);
            // 也调整当前速度，让变化即时生效
            player.currentSpeed = Mathf.Min(player.currentSpeed, player.maxSpeed);
        }

        // 2. 障碍物生成器
        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetDifficulty(difficultyLevel);

            // 高难度缩小生成间距
            obstacleSpawner.spawnDistanceMin = Mathf.Lerp(80f, 30f, t);
            obstacleSpawner.spawnDistanceMax = Mathf.Lerp(150f, 60f, t);
        }

        // 3. 地形塌陷频率
        if (terrainCollapse != null)
        {
            float interval = Mathf.Lerp(baseCollapseMaxInterval, 5f, t);
            terrainCollapse.SetCollapseFrequency(interval);
        }

        // 4. 列车追击速度
        if (trainController != null)
        {
            trainController.baseApproachRate = Mathf.Lerp(baseTrainApproach, baseTrainApproach * 2f, t);
        }
    }

    public void OnRewindTriggered()
    {
        rewindProtectionEnd = Time.time + rewindProtectionDuration;
    }

    public void ResetForNewGame()
    {
        currentRunStartZ = 0f;
        recentSurvivalDistances.Clear();
        difficultyLevel = 3; // 初始难度适中
        ApplyDifficulty();
    }

    public int GetDifficultyLevel()
    {
        return difficultyLevel;
    }
}
