using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    // === 对象池 ===
    private List<GameObject> obstaclePool = new List<GameObject>();
    public int poolSize = 20;

    // === 生成参数 ===
    public float spawnDistanceMin = 40f;
    public float spawnDistanceMax = 80f;
    public float spawnIntervalMin = 1.5f;
    public float spawnIntervalMax = 3f;
    private float nextSpawnZ;

    // === 玩家引用 ===
    public PlayerController player;

    // === 预制体引用 ===
    public GameObject[] staticObstaclePrefabs;  // O01~O04
    public GameObject dynamicObstaclePrefab;     // O05
    public GameObject[] trapPrefabs;             // O08~O09

    // === 难度控制 ===
    public int difficultyLevel = 1;
    public bool dynamicUnlocked = false;
    public bool trapsUnlocked = false;

    // === 回收距离 ===
    public float recycleDistance = 20f;

    void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        nextSpawnZ = player.transform.position.z + spawnDistanceMin;
        InitializePool();
    }

    void Update()
    {
        if (player == null || player.isDead) return;

        // 回收后方障碍物
        RecycleObstacles();

        // 检查是否需要生成
        float playerZ = player.transform.position.z;
        if (playerZ + spawnDistanceMax > nextSpawnZ)
        {
            SpawnObstacleSet();
            float interval = Random.Range(spawnIntervalMin, spawnIntervalMax);
            nextSpawnZ = playerZ + spawnDistanceMin + interval;
        }
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = new GameObject("Obstacle_" + i);
            obj.SetActive(false);
            obstaclePool.Add(obj);
        }
    }

    void SpawnObstacleSet()
    {
        // 根据难度选择障碍物类型
        int type = SelectObstacleType();

        // 随机选择占用的轨道数
        int lanesToBlock = Random.Range(1, 4); // 1~3 条轨道
        List<int> availableLanes = new List<int>() { 0, 1, 2 };
        List<int> blockedLanes = new List<int>();

        for (int i = 0; i < lanesToBlock; i++)
        {
            if (availableLanes.Count == 0) break;
            int idx = Random.Range(0, availableLanes.Count);
            blockedLanes.Add(availableLanes[idx]);
            availableLanes.RemoveAt(idx);
        }

        // 保证至少 1 条轨道可通行
        if (blockedLanes.Count >= 3)
        {
            blockedLanes.RemoveAt(Random.Range(0, 3));
        }

        // 在每条被阻塞的轨道上生成障碍物
        float spawnZ = player.transform.position.z + Random.Range(spawnDistanceMin, spawnDistanceMax);

        foreach (int lane in blockedLanes)
        {
            float laneX = (lane - 1) * 2f; // -2, 0, 2

            GameObject prefab = null;
            if (type == 0 && staticObstaclePrefabs != null && staticObstaclePrefabs.Length > 0)
            {
                prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];
            }
            else if (type == 1 && dynamicUnlocked && dynamicObstaclePrefab != null)
            {
                prefab = dynamicObstaclePrefab;
            }
            else if (type == 2 && trapsUnlocked && trapPrefabs != null && trapPrefabs.Length > 0)
            {
                prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
            }

            if (prefab == null && staticObstaclePrefabs != null && staticObstaclePrefabs.Length > 0)
            {
                prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];
            }

            Vector3 pos = new Vector3(laneX, 1.5f, spawnZ);
            if (prefab != null)
            {
                CreateObstacle(prefab, pos);
            }
            else
            {
                // 无预制体时程序化生成
                CreateObstacleGeometry(type, pos);
            }
        }
    }

    int SelectObstacleType()
    {
        // 根据难度等级解锁
        if (difficultyLevel >= 7 && trapsUnlocked && Random.value < 0.2f)
        {
            return 2; // 陷阱
        }
        if (difficultyLevel >= 4 && dynamicUnlocked && Random.value < 0.35f)
        {
            return 1; // 动态
        }
        return 0; // 静态
    }

    GameObject CreateObstacle(GameObject prefab, Vector3 position)
    {
        // 从对象池获取或实例化
        GameObject obj = GetPooledObject();
        if (obj == null)
        {
            obj = (GameObject)GameObject.Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            // 清除旧的子对象
            foreach (Transform child in obj.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;

            // 复制预制体的几何体
            foreach (Transform child in prefab.transform)
            {
                GameObject copy = (GameObject)GameObject.Instantiate(child.gameObject);
                copy.transform.parent = obj.transform;
                copy.transform.localPosition = child.localPosition;
                copy.transform.localRotation = child.localRotation;
                copy.transform.localScale = child.localScale;
            }

            // 复制碰撞体
            Collider prefabCol = prefab.GetComponent<Collider>();
            if (prefabCol != null)
            {
                Collider objCol = obj.GetComponent<Collider>();
                if (objCol == null)
                {
                    if (prefabCol is BoxCollider)
                    {
                        BoxCollider bc = obj.AddComponent<BoxCollider>();
                        BoxCollider pbc = (BoxCollider)prefabCol;
                        bc.center = pbc.center;
                        bc.size = pbc.size;
                        bc.isTrigger = pbc.isTrigger;
                    }
                    else if (prefabCol is SphereCollider)
                    {
                        SphereCollider sc = obj.AddComponent<SphereCollider>();
                        SphereCollider psc = (SphereCollider)prefabCol;
                        sc.center = psc.center;
                        sc.radius = psc.radius;
                        sc.isTrigger = psc.isTrigger;
                    }
                    else if (prefabCol is CapsuleCollider)
                    {
                        CapsuleCollider cc = obj.AddComponent<CapsuleCollider>();
                        CapsuleCollider pcc = (CapsuleCollider)prefabCol;
                        cc.center = pcc.center;
                        cc.radius = pcc.radius;
                        cc.height = pcc.height;
                        cc.direction = pcc.direction;
                        cc.isTrigger = pcc.isTrigger;
                    }
                }
            }

            // 复制 Obstacle 标签
            ObstacleTag prefabTag = prefab.GetComponent<ObstacleTag>();
            if (prefabTag != null)
            {
                ObstacleTag objTag = obj.GetComponent<ObstacleTag>();
                if (objTag == null) objTag = obj.AddComponent<ObstacleTag>();
                objTag.isTrap = prefabTag.isTrap;
                objTag.isDynamic = prefabTag.isDynamic;
            }
        }

        obj.SetActive(true);
        return obj;
    }

    GameObject GetPooledObject()
    {
        foreach (GameObject obj in obstaclePool)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return null;
    }

    void RecycleObstacles()
    {
        float playerZ = player.transform.position.z;
        foreach (GameObject obj in obstaclePool)
        {
            if (obj.activeInHierarchy && obj.transform.position.z < playerZ - recycleDistance)
            {
                obj.SetActive(false);
            }
        }
    }

    public void SetDifficulty(int level)
    {
        difficultyLevel = Mathf.Clamp(level, 1, 10);
        dynamicUnlocked = (difficultyLevel >= 4);
        trapsUnlocked = (difficultyLevel >= 7);

        // 调整生成频率
        spawnIntervalMin = 2f - (difficultyLevel * 0.1f);
        spawnIntervalMax = 4f - (difficultyLevel * 0.15f);
        if (spawnIntervalMin < 0.5f) spawnIntervalMin = 0.5f;
        if (spawnIntervalMax < 1f) spawnIntervalMax = 1f;
    }

    // === 程序化几何体创建（无预制体时的兜底） ===
    GameObject CreateObstacleGeometry(int type, Vector3 position)
    {
        GameObject obj = GetPooledObject();
        if (obj == null)
        {
            obj = new GameObject("Obstacle_Fallback");
        }
        else
        {
            foreach (Transform child in obj.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;

        ObstacleTag tag = obj.GetComponent<ObstacleTag>();
        if (tag == null) tag = obj.AddComponent<ObstacleTag>();

        if (type == 0) // 静态障碍
        {
            int subtype = Random.Range(0, 4);
            if (subtype == 0) // O01 石块
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = obj.transform;
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(1f, 1f, 1f);
                cube.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.25f);
                tag.isTrap = false; tag.isDynamic = false;
            }
            else if (subtype == 1) // O02 墙体
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = obj.transform;
                cube.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                cube.transform.localScale = new Vector3(3f, 3f, 0.5f);
                cube.GetComponent<Renderer>().material.color = new Color(0.6f, 0.15f, 0.15f);
                tag.isTrap = false; tag.isDynamic = false;
            }
            else if (subtype == 2) // O03 尖刺
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    spike.transform.parent = obj.transform;
                    spike.transform.localPosition = new Vector3((i - 1) * 0.5f, 0.3f, 0f);
                    spike.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
                    spike.GetComponent<Renderer>().material.color = Color.red;
                }
                tag.isTrap = false; tag.isDynamic = false;
            }
            else // O04 断台
            {
                tag.isTrap = true; tag.isDynamic = false;
            }
        }
        else if (type == 1) // O05 旋转机关
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.parent = obj.transform;
            bar.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            bar.transform.localScale = new Vector3(4f, 0.3f, 0.3f);
            bar.GetComponent<Renderer>().material.color = new Color(0.7f, 0.6f, 0.1f);
            tag.isDynamic = true; tag.isTrap = false;
        }
        else if (type == 2) // 陷阱
        {
            int subtype = Random.Range(0, 2);
            if (subtype == 0) // O08 深坑
            {
                tag.isTrap = true; tag.isDynamic = false;
            }
            else // O09 即死区域
            {
                GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                zone.transform.parent = obj.transform;
                zone.transform.localPosition = new Vector3(0f, 1f, 0f);
                zone.transform.localScale = new Vector3(1.5f, 2f, 2f);
                zone.GetComponent<Renderer>().material.color = new Color(1f, 0.1f, 0.1f);
                Collider col = zone.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
                tag.isTrap = true; tag.isDynamic = false;
            }
        }

        obj.SetActive(true);
        return obj;
    }
}
