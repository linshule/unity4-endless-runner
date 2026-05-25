using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    public PlayerController player;
    public GameObject coinPrefab;

    // === 生成参数 ===
    public float spawnInterval = 3f;
    public float spawnDistanceMin = 30f;
    public float spawnDistanceMax = 70f;
    public float floatCoinHeight = 5f; // 浮空金币高度
    public float floatCoinChance = 0.35f;  // 浮空概率

    private float nextSpawnZ;
    private List<GameObject> coinPool = new List<GameObject>();
    public int poolSize = 30;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        nextSpawnZ = player.transform.position.z + spawnDistanceMin;

        InitializePool();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        float playerZ = player.transform.position.z;
        if (playerZ + spawnDistanceMax > nextSpawnZ)
        {
            SpawnCoinGroup();
            nextSpawnZ = playerZ + Random.Range(spawnDistanceMin, spawnDistanceMax);
        }

        RecycleCoins();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject coin = CreateCoinVisual();
            coin.SetActive(false);
            coinPool.Add(coin);
        }
    }

    GameObject CreateCoinVisual()
    {
        // 黄色旋转 Sphere
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "Coin";
        coin.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        Renderer renderer = coin.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }

        // 设为触发器
        Collider col = coin.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // 添加 Coin 标签脚本
        coin.AddComponent<CoinPickup>();

        return coin;
    }

    void SpawnCoinGroup()
    {
        int count = Random.Range(2, 6);
        float startZ = player.transform.position.z + Random.Range(spawnDistanceMin, spawnDistanceMax);

        for (int i = 0; i < count; i++)
        {
            GameObject coin = GetPooledCoin();
            if (coin == null) break;

            int lane = Random.Range(0, 3);
            float x = (lane - 1) * 6f;
            float z = startZ + i * 2f;

            // 随机浮空或地面，浮空金币颜色更亮
            bool isFloating = (Random.value < floatCoinChance);
            float y = isFloating ? floatCoinHeight : 1f;

            coin.transform.position = new Vector3(x, y, z);

            // 浮空金币亮金色，地面金币深金色
            Renderer cr = coin.GetComponent<Renderer>();
            if (cr != null)
            {
                cr.material.color = isFloating 
                    ? new Color(1f, 0.9f, 0.3f)   // 亮金（空中）
                    : new Color(1f, 0.7f, 0.1f);  // 深金（地面）
            }

            coin.SetActive(true);
        }
    }

    GameObject GetPooledCoin()
    {
        foreach (GameObject coin in coinPool)
        {
            if (!coin.activeInHierarchy)
                return coin;
        }
        return null;
    }

    void RecycleCoins()
    {
        float playerZ = player.transform.position.z;
        foreach (GameObject coin in coinPool)
        {
            if (coin.activeInHierarchy && coin.transform.position.z < playerZ - 15f)
            {
                coin.SetActive(false);
            }
        }
    }
}
