using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    private List<GameObject> obstaclePool = new List<GameObject>();
    public int poolSize = 30;

    public float spawnDistanceMin = 60f;
    public float spawnDistanceMax = 120f;
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 6f;
    private float nextSpawnZ;

    public PlayerController player;

    public GameObject[] staticObstaclePrefabs;
    public GameObject dynamicObstaclePrefab;
    public GameObject[] trapPrefabs;

    public int difficultyLevel = 1;
    public bool dynamicUnlocked = false;
    public bool trapsUnlocked = false;

    public float recycleDistance = 25f;

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

        RecycleObstacles();

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
        int type = SelectObstacleType();
        int lanesToBlock = Random.Range(1, 3);
        List<int> availableLanes = new List<int>() { 0, 1, 2 };
        List<int> blockedLanes = new List<int>();

        for (int i = 0; i < lanesToBlock; i++)
        {
            if (availableLanes.Count == 0) break;
            int idx = Random.Range(0, availableLanes.Count);
            blockedLanes.Add(availableLanes[idx]);
            availableLanes.RemoveAt(idx);
        }

        float spawnZ = player.transform.position.z + Random.Range(spawnDistanceMin, spawnDistanceMax);

        foreach (int lane in blockedLanes)
        {
            float laneX = (lane - 1) * 6f;

            GameObject prefab = null;
            if (type == 0 && staticObstaclePrefabs != null && staticObstaclePrefabs.Length > 0)
                prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];
            else if (type == 1 && dynamicUnlocked && dynamicObstaclePrefab != null)
                prefab = dynamicObstaclePrefab;
            else if (type == 2 && trapsUnlocked && trapPrefabs != null && trapPrefabs.Length > 0)
                prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];

            if (prefab == null && staticObstaclePrefabs != null && staticObstaclePrefabs.Length > 0)
                prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];

            Vector3 pos = new Vector3(laneX, 1.5f, spawnZ);
            if (prefab != null)
                CreateObstacle(prefab, pos);
            else
                CreateObstacleGeometry(type, pos);
        }
    }

    int SelectObstacleType()
    {
        if (difficultyLevel >= 7 && trapsUnlocked && Random.value < 0.15f)
            return 2;
        if (difficultyLevel >= 4 && dynamicUnlocked && Random.value < 0.3f)
            return 1;
        return 0;
    }

    GameObject CreateObstacle(GameObject prefab, Vector3 position)
    {
        GameObject obj = GetPooledObject();
        if (obj == null)
        {
            obj = (GameObject)GameObject.Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            foreach (Transform child in obj.transform)
                GameObject.Destroy(child.gameObject);

            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;

            foreach (Transform child in prefab.transform)
            {
                GameObject copy = (GameObject)GameObject.Instantiate(child.gameObject);
                copy.transform.parent = obj.transform;
                copy.transform.localPosition = child.localPosition;
                copy.transform.localRotation = child.localRotation;
                copy.transform.localScale = child.localScale;
            }

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

        // 特殊处理：断台(Gap)需要碰撞体让玩家碰撞检测生效
        ObstacleTag tag = obj.GetComponent<ObstacleTag>();
        if (tag != null && tag.isTrap && !tag.isDynamic)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider bc = obj.AddComponent<BoxCollider>();
                bc.size = new Vector3(5f, 0.5f, 2f);
                bc.center = new Vector3(0f, -0.5f, 0f);
                bc.isTrigger = false;
            }
        }

        return obj;
    }

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
                GameObject.Destroy(child.gameObject);
        }

        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;

        ObstacleTag tag = obj.GetComponent<ObstacleTag>();
        if (tag == null) tag = obj.AddComponent<ObstacleTag>();

        if (type == 0)
        {
            float roll = Random.value;
            int subtype;
            if (roll < 0.4f) subtype = 0;
            else if (roll < 0.5f) subtype = 1;
            else if (roll < 0.8f) subtype = 2;
            else subtype = 3;

            if (subtype == 0)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = obj.transform;
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                cube.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.25f);
                tag.isTrap = false; tag.isDynamic = false;
            }
            else if (subtype == 1)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = obj.transform;
                cube.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                cube.transform.localScale = new Vector3(1.5f, 1.5f, 0.3f);
                cube.GetComponent<Renderer>().material.color = new Color(0.5f, 0.1f, 0.1f);
                tag.isTrap = false; tag.isDynamic = false;
            }
            else if (subtype == 2)
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
            else
            {
                // Gap: 添加实际碰撞体
                tag.isTrap = true; tag.isDynamic = false;
                BoxCollider bc = obj.AddComponent<BoxCollider>();
                bc.size = new Vector3(5f, 0.5f, 2f);
                bc.center = new Vector3(0f, -0.5f, 0f);
            }
        }
        else if (type == 1)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.parent = obj.transform;
            bar.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            bar.transform.localScale = new Vector3(6f, 0.5f, 0.5f);
            bar.GetComponent<Renderer>().material.color = new Color(0.7f, 0.6f, 0.1f);
            tag.isDynamic = true; tag.isTrap = false;
        }
        else if (type == 2)
        {
            int subtype = Random.Range(0, 2);
            if (subtype == 0)
            {
                // Pit: 碰撞体
                tag.isTrap = true; tag.isDynamic = false;
                BoxCollider bc = obj.AddComponent<BoxCollider>();
                bc.size = new Vector3(6f, 0.3f, 4f);
                bc.center = new Vector3(0f, -0.5f, 0f);
            }
            else
            {
                GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                zone.transform.parent = obj.transform;
                zone.transform.localPosition = new Vector3(0f, 1f, 0f);
                zone.transform.localScale = new Vector3(1.5f, 2f, 2f);
                zone.GetComponent<Renderer>().material.color = new Color(0.8f, 0.2f, 0.2f, 0.6f);
                Collider col = zone.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
                tag.isTrap = true; tag.isDynamic = false;
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
                return obj;
        }
        return null;
    }

    void RecycleObstacles()
    {
        float playerZ = player.transform.position.z;
        foreach (GameObject obj in obstaclePool)
        {
            if (obj.activeInHierarchy && obj.transform.position.z < playerZ - recycleDistance)
                obj.SetActive(false);
        }
    }

    public void SetDifficulty(int level)
    {
        difficultyLevel = Mathf.Clamp(level, 1, 10);
        dynamicUnlocked = (difficultyLevel >= 4);
        trapsUnlocked = (difficultyLevel >= 7);
        spawnIntervalMin = 4f - (difficultyLevel * 0.15f);
        spawnIntervalMax = 7f - (difficultyLevel * 0.2f);
        if (spawnIntervalMin < 0.8f) spawnIntervalMin = 0.8f;
        if (spawnIntervalMax < 1.5f) spawnIntervalMax = 1.5f;
    }
}