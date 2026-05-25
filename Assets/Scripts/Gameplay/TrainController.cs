using UnityEngine;

public class TrainController : MonoBehaviour
{
    public PlayerController player;
    
    // === 列车参数 ===
    public float initialDistance = 100f;
    public float minDistance = 10f;          // 触发即死的距离
    public float currentDistance;
    
    // === 距离变化 ===
    public float baseApproachRate = 2f;      // 基础逼近速度
    public float penaltyApproachRate = 15f;  // 每次死亡加的距离惩罚

    // === 视觉反馈 ===
    public Color warningColor = new Color(0.2f, 0f, 0f, 0f);
    private float warningIntensity = 0f;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        
        currentDistance = initialDistance;
        
        // 列车位置（玩家后方）
        UpdateTrainPosition();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        // 列车追赶
        float approachSpeed = baseApproachRate + (player.GetSpeed() * 0.3f);
        currentDistance -= approachSpeed * Time.deltaTime;

        // 画面边缘红光
        warningIntensity = Mathf.Clamp01(1f - (currentDistance / initialDistance));

        UpdateTrainPosition();

        // 追上判定
        if (currentDistance <= minDistance)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnTrainCaught();
            }
        }
    }

    void UpdateTrainPosition()
    {
        float playerZ = player.transform.position.z;
        transform.position = new Vector3(0f, 1.5f, playerZ - currentDistance);
    }

    public float GetDistance()
    {
        return currentDistance;
    }

    public float GetWarningIntensity()
    {
        return warningIntensity;
    }

    public void ApplyDeathPenalty()
    {
        currentDistance -= penaltyApproachRate;
    }
}
