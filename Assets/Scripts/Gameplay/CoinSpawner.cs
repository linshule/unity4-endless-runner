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
    public float floatCoinHeight = 2.5f; // 浮空金币高度
    public float floatCoinChance = 0.3f;  // 浮空概率

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
        coin.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

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
            float x = (lane - 1) * 2f;
            float z = startZ + i * 2f;

            // 随机浮空或地面
            float y = 1f;
            if (Random.value < floatCoinChance)
            {
                y = floatCoinHeight;
            }

            coin.transform.position = new Vector3(x, y, z);
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
