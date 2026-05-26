using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    public PlayerController player;
    public GameObject coinPrefab;

    public float spawnDistanceMin = 25f;
    public float spawnDistanceMax = 100f;
    public float floatCoinHeight = 5f;
    public float floatCoinChance = 0.35f;

    private float nextSpawnZ;
    private List<GameObject> coinPool = new List<GameObject>();
    public int poolSize = 40;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        nextSpawnZ = spawnDistanceMin;
        InitializePool();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        float playerZ = player.transform.position.z;

        // 正确逻辑：跑过 nextSpawnZ 才生成下一组
        if (playerZ > nextSpawnZ)
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
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "Coin";
        coin.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        Renderer renderer = coin.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.yellow;

        SphereCollider col = coin.GetComponent<Collider>() as SphereCollider;
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = 1.5f;
        }

        // 必须加 Rigidbody，否则 CharacterController 不会触发 OnTriggerEnter
        Rigidbody rb = coin.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        coin.AddComponent<CoinPickup>();
        return coin;
    }

    void SpawnCoinGroup()
    {
        int count = Random.Range(3, 8);
        float startZ = player.transform.position.z + Random.Range(spawnDistanceMin, spawnDistanceMax);

        for (int i = 0; i < count; i++)
        {
            GameObject coin = GetPooledCoin();
            if (coin == null) break;

            int lane = Random.Range(0, 3);
            float x = (lane - 1) * 6f;
            float z = startZ + i * 1.8f;

            bool isFloating = (Random.value < floatCoinChance);
            float y = isFloating ? floatCoinHeight : 1f;

            coin.transform.position = new Vector3(x, y, z);

            Renderer cr = coin.GetComponent<Renderer>();
            if (cr != null)
            {
                cr.material.color = isFloating
                    ? new Color(1f, 0.9f, 0.3f)
                    : new Color(1f, 0.7f, 0.1f);
            }

            coin.SetActive(true);
        }
    }

    GameObject GetPooledCoin()
    {
        foreach (GameObject coin in coinPool)
            if (!coin.activeInHierarchy) return coin;
        return null;
    }

    void RecycleCoins()
    {
        float playerZ = player.transform.position.z;
        foreach (GameObject coin in coinPool)
            if (coin.activeInHierarchy && coin.transform.position.z < playerZ - 20f)
                coin.SetActive(false);
    }
}