using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public PlayerController player;
    
    // === I01 双倍分数 ===
    public GameObject doubleScorePrefab;
    public float doubleScoreDuration = 10f;
    private bool doubleScoreActive = false;
    private float doubleScoreTimer = 0f;

    // === 生成参数 ===
    public float spawnInterval = 30f;
    public float spawnChance = 0.5f;
    private float nextSpawnCheck;
    private GameObject activeDoubleScoreObj;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        nextSpawnCheck = Time.time + spawnInterval;
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        // 双倍分数计时
        if (doubleScoreActive)
        {
            doubleScoreTimer -= Time.deltaTime;
            if (doubleScoreTimer <= 0f)
            {
                DeactivateDoubleScore();
            }
        }

        // 随机生成道具
        if (Time.time >= nextSpawnCheck)
        {
            if (Random.value < spawnChance && !doubleScoreActive)
            {
                SpawnDoubleScore();
            }
            nextSpawnCheck = Time.time + spawnInterval;
        }
    }

    void SpawnDoubleScore()
    {
        if (activeDoubleScoreObj != null) return;

        // 创建黄色 Cube 旋转道具
        activeDoubleScoreObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        activeDoubleScoreObj.name = "DoubleScore";
        activeDoubleScoreObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        Renderer renderer = activeDoubleScoreObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }

        Collider col = activeDoubleScoreObj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        activeDoubleScoreObj.AddComponent<DoubleScorePickup>();

        // 随机位置
        int lane = Random.Range(0, 3);
        float x = (lane - 1) * 5f;
        float z = player.transform.position.z + Random.Range(40f, 80f);
        float y = (Random.value < 0.3f) ? 3f : 1f; // 30% 浮空

        activeDoubleScoreObj.transform.position = new Vector3(x, y, z);
    }

    public void ActivateDoubleScore()
    {
        doubleScoreActive = true;
        doubleScoreTimer = doubleScoreDuration;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.scoreMultiplier = 2f;
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.OnDoubleScoreActivated(doubleScoreDuration);
        }

        activeDoubleScoreObj = null;
    }

    void DeactivateDoubleScore()
    {
        doubleScoreActive = false;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.scoreMultiplier = 1f;
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.OnDoubleScoreDeactivated();
        }
    }

    public float GetDoubleScoreRemaining()
    {
        if (!doubleScoreActive) return 0f;
        return doubleScoreTimer / doubleScoreDuration;
    }

    public bool IsDoubleScoreActive()
    {
        return doubleScoreActive;
    }
}
