using UnityEngine;

public class TrainController : MonoBehaviour
{
    public PlayerController player;
    
    // === 列车参数 ===
    public float initialDistance = 150f;
    public float minDistance = 10f;
    public float currentDistance;
    
    // === 距离变化 ===
    public float baseApproachRate = 1.5f;
    public float penaltyApproachRate = 15f;

    // === 饥饿机制 ===
    public float starvationThreshold = 15f;
    public float starvationMultiplier = 4f;
    private float starvationTimer = 0f;
    private bool isStarving = false;

    // === 视觉反馈 ===
    public Color warningColor = new Color(0.2f, 0f, 0f, 0f);
    private float warningIntensity = 0f;
    private Renderer trainRenderer;
    private Color originalTrainColor;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        
        currentDistance = initialDistance;

        trainRenderer = GetComponent<Renderer>();
        if (trainRenderer != null)
            originalTrainColor = trainRenderer.material.color;
        
        UpdateTrainPosition();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        // 饥饿计时
        starvationTimer += Time.deltaTime;
        isStarving = (starvationTimer >= starvationThreshold);

        // 匀速逼近（饥饿时加速）
        float approachSpeed = baseApproachRate;
        if (isStarving)
            approachSpeed *= starvationMultiplier;
        currentDistance -= approachSpeed * Time.deltaTime;

        if (currentDistance > initialDistance)
            currentDistance = initialDistance;

        // 饥饿视觉：列车变红
        if (trainRenderer != null)
        {
            if (isStarving)
                trainRenderer.material.color = Color.red;
            else
                trainRenderer.material.color = originalTrainColor;
        }

        float dangerRatio = 1f - (currentDistance / initialDistance);
        warningIntensity = Mathf.Clamp01(dangerRatio);

        UpdateTrainPosition();

        if (currentDistance <= minDistance)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
                gm.OnTrainCaught();
        }
    }

    void UpdateTrainPosition()
    {
        float playerZ = player.transform.position.z;
        transform.position = new Vector3(0f, 1.5f, playerZ - currentDistance);
    }

    public void OnCoinCollected()
    {
        starvationTimer = 0f;
        if (isStarving && trainRenderer != null)
            trainRenderer.material.color = originalTrainColor;
        isStarving = false;
    }

    public float GetDistance()
    {
        return currentDistance;
    }

    public float GetWarningIntensity()
    {
        return warningIntensity;
    }

    public bool IsStarving()
    {
        return isStarving;
    }

    public void ApplyDeathPenalty()
    {
        currentDistance -= penaltyApproachRate;
    }

    public void RestoreDistance()
    {
        currentDistance = initialDistance * 0.7f;
        OnCoinCollected();
    }

    public void AddDistance(float amount)
    {
        currentDistance += amount;
        if (currentDistance > initialDistance)
            currentDistance = initialDistance;
    }
}