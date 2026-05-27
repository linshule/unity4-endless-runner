using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    private List<GameObject> obstaclePool = new List<GameObject>();
    public int poolSize = 40;

    // === 生成参数（大幅拉大间距保证可玩性） ===
    public float spawnDistanceMin = 80f;
    public float spawnDistanceMax = 150f;
    private float nextSpawnZ;

    public PlayerController player;
    private TrackManager trackManager;

    public GameObject[] staticObstaclePrefabs;
    public GameObject dynamicObstaclePrefab;
    public GameObject[] trapPrefabs;

    public int difficultyLevel = 1;
    public bool dynamicUnlocked = false;
    public bool trapsUnlocked = false;

    public float recycleDistance = 30f;

    // === 新手保护 ===
    private float safeZoneEnd = 50f;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        trackManager = FindObjectOfType<TrackManager>();

        // 第一个障碍物在远方
        nextSpawnZ = spawnDistanceMin;
        safeZoneEnd = 50f;

        InitializePool();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        RecycleObstacles();

        float playerZ = player.transform.position.z;

        // 新手保护区
        if (playerZ < safeZoneEnd) return;

        // 正确逻辑：玩家跑过 nextSpawnZ 才生成下一组
        if (playerZ > nextSpawnZ)
        {
            SpawnObstacleSet();
            nextSpawnZ = playerZ + Random.Range(spawnDistanceMin, spawnDistanceMax);
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

        // 大多数时候只堵 1 条道
        int lanesToBlock = (Random.value < 0.7f) ? 1 : 2;

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
            Vector3 pos = new Vector3(laneX, 1.5f, spawnZ);

            GameObject prefab = null;
            if (type == 0 && staticObstaclePrefabs != null && staticObstaclePrefabs.Length > 0)
                prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];
            else if (type == 1 && dynamicUnlocked && dynamicObstaclePrefab != null)
                prefab = dynamicObstaclePrefab;
            else if (type == 2 && trapsUnlocked && trapPrefabs != null && trapPrefabs.Length > 0)
                prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];

            if (prefab == null && staticObstaclePrefabs != null && staticObstaclePrefabs.Length > 0)
                prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];

            if (prefab != null)
                CreateObstacle(prefab, pos);
            else
                CreateObstacleGeometry(type, pos, lane);
        }
    }

    int SelectObstacleType()
    {
        if (difficultyLevel >= 5 && trapsUnlocked && Random.value < 0.12f)
            return 2;
        if (difficultyLevel >= 2 && dynamicUnlocked && Random.value < 0.25f)
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
                GameObject.DestroyImmediate(child.gameObject);

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
                        bc.center = pbc.center; bc.size = pbc.size; bc.isTrigger = pbc.isTrigger;
                    }
                    else if (prefabCol is SphereCollider)
                    {
                        SphereCollider sc = obj.AddComponent<SphereCollider>();
                        SphereCollider psc = (SphereCollider)prefabCol;
                        sc.center = psc.center; sc.radius = psc.radius; sc.isTrigger = psc.isTrigger;
                    }
                    else if (prefabCol is CapsuleCollider)
                    {
                        CapsuleCollider cc = obj.AddComponent<CapsuleCollider>();
                        CapsuleCollider pcc = (CapsuleCollider)prefabCol;
                        cc.center = pcc.center; cc.radius = pcc.radius;
                        cc.height = pcc.height; cc.direction = pcc.direction; cc.isTrigger = pcc.isTrigger;
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

            // 复制 DynamicObstacle 组件（旋转机关脚本）
            DynamicObstacle prefabDyn = prefab.GetComponent<DynamicObstacle>();
            if (prefabDyn != null)
            {
                DynamicObstacle objDyn = obj.GetComponent<DynamicObstacle>();
                if (objDyn == null) obj.AddComponent<DynamicObstacle>();
            }
            }
        }

        obj.SetActive(true);

        // 陷阱类障碍物(Gap/Pit/DeathZone)不应强制添加碰撞体
        // 它们的碰撞逻辑由各自的子对象处理

        return obj;
    }

    GameObject CreateObstacleGeometry(int type, Vector3 position, int lane = -1)
    {
        GameObject obj = GetPooledObject();
        if (obj == null)
            obj = new GameObject("Obstacle_Fallback");
        else
            foreach (Transform child in obj.transform)
                GameObject.DestroyImmediate(child.gameObject);

        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;

        ObstacleTag tag = obj.GetComponent<ObstacleTag>();
        if (tag == null) tag = obj.AddComponent<ObstacleTag>();

        if (type == 0)
        {
            float roll = Random.value;
            int subtype;
            if (roll < 0.35f) subtype = 0;      // 石块 35%
            else if (roll < 0.45f) subtype = 1; // 墙体 10%
            else if (roll < 0.65f) subtype = 2; // 尖刺 20%
            else if (roll < 0.80f) subtype = 3; // 断台 15%
            else subtype = 4;                    // 低位横梁 20%（滑铲目标）

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
            else if (subtype == 3)
            {
                // 断台：显示黑色缺口标记 + 实际移除该轨道段碰撞体 + 下方死亡触发
                tag.isTrap = true; tag.isDynamic = false;
                GameObject gapVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gapVis.transform.parent = obj.transform;
                gapVis.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                gapVis.transform.localScale = new Vector3(4f, 0.05f, 3f);
                gapVis.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.25f);
                Collider gapCol = gapVis.GetComponent<Collider>();
                if (gapCol != null) gapCol.enabled = false;
                if (trackManager != null && lane >= 0)
                    trackManager.DisableTrackColliderAt(lane, position.z);

                // 缺口下方死亡碰撞体（玩家下坠时即死，不等 Y<-5）
                GameObject deathBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deathBar.name = "GapDeathBar";
                deathBar.transform.parent = obj.transform;
                deathBar.transform.localPosition = new Vector3(0f, -1.5f, 0f);
                deathBar.transform.localScale = new Vector3(5f, 0.5f, 4f);
                deathBar.GetComponent<Renderer>().material.color = new Color(0.05f, 0.05f, 0.05f);
                ObstacleTag deathTag = deathBar.AddComponent<ObstacleTag>();
                deathTag.isTrap = true;
            }
            else
            {
                // 低位横梁：横跨 2-3 条轨道，需滑铲通过。站立碰撞即死，滑铲可躲
                tag.isTrap = false; tag.isDynamic = false;
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.transform.parent = obj.transform;
                bar.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                bar.transform.localScale = new Vector3(6f, 0.5f, 1f);
                bar.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
            }
        }
        else if (type == 1)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "SpinnerBar";
            bar.transform.parent = obj.transform;
            bar.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            bar.transform.localScale = new Vector3(6f, 0.5f, 0.5f);
            bar.GetComponent<Renderer>().material.color = new Color(0.7f, 0.6f, 0.1f);
            tag.isDynamic = true; tag.isTrap = false;

            // 旋转机关需要 DynamicObstacle 组件
            DynamicObstacle dyn = obj.GetComponent<DynamicObstacle>();
            if (dyn == null) obj.AddComponent<DynamicObstacle>();
        }
        else if (type == 2)
        {
            int subtype = Random.Range(0, 2);
            if (subtype == 0)
            {
                // 深坑：黑色平板标记 + 实际移除该轨道段碰撞体 + 下方死亡触发
                tag.isTrap = true; tag.isDynamic = false;
                GameObject pitVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pitVis.transform.parent = obj.transform;
                pitVis.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                pitVis.transform.localScale = new Vector3(4f, 0.05f, 3f);
                pitVis.GetComponent<Renderer>().material.color = new Color(0.25f, 0.2f, 0.15f);
                Collider pitCol = pitVis.GetComponent<Collider>();
                if (pitCol != null) pitCol.enabled = false;
                if (trackManager != null && lane >= 0)
                    trackManager.DisableTrackColliderAt(lane, position.z);

                // 深坑下方死亡碰撞体
                GameObject deathBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deathBar.name = "PitDeathBar";
                deathBar.transform.parent = obj.transform;
                deathBar.transform.localPosition = new Vector3(0f, -1.5f, 0f);
                deathBar.transform.localScale = new Vector3(5f, 0.5f, 4f);
                deathBar.GetComponent<Renderer>().material.color = new Color(0.05f, 0.05f, 0.05f);
                ObstacleTag deathTag = deathBar.AddComponent<ObstacleTag>();
                deathTag.isTrap = true;
            }
            else
            {
                GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                zone.transform.parent = obj.transform;
                zone.transform.localPosition = new Vector3(0f, 1f, 0f);
                zone.transform.localScale = new Vector3(1.5f, 2f, 2f);
                zone.GetComponent<Renderer>().material.color = new Color(0.8f, 0.2f, 0.2f);
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
            if (!obj.activeInHierarchy) return obj;
        return null;
    }

    void RecycleObstacles()
    {
        float playerZ = player.transform.position.z;
        foreach (GameObject obj in obstaclePool)
            if (obj.activeInHierarchy && obj.transform.position.z < playerZ - recycleDistance)
                obj.SetActive(false);
    }

    public void SetDifficulty(int level)
    {
        difficultyLevel = Mathf.Clamp(level, 1, 10);
        dynamicUnlocked = (difficultyLevel >= 2);
        trapsUnlocked = (difficultyLevel >= 5);
    }
}