using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProjectBuilder
{
    // === 安全着色（避免 Edit Mode 材质泄露） ===
    static void SetCubeColor(GameObject cube, Color color)
    {
        Renderer r = cube.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Diffuse"));
            mat.color = color;
            r.sharedMaterial = mat;
        }
    }
    // ============================================================
    // 菜单入口
    // ============================================================

    [MenuItem("Tools/无尽跑酷/一键构建全部（场景+预制体+项目设置）", false, 0)]
    static void BuildAll()
    {
        SetupProjectSettings();
        BuildPrefabs();
        BuildGameScene();
        SetupBuildSettings();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            "场景、预制体、项目设置全部构建完毕。\n\n请打开 Assets/Scenes/Game.unity 开始。",
            "好的");
    }

    [MenuItem("Tools/无尽跑酷/仅构建场景", false, 10)]
    static void BuildSceneOnly()
    {
        BuildGameScene();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("[ProjectBuilder] Game.unity 构建完成");
    }

    [MenuItem("Tools/无尽跑酷/仅构建预制体", false, 11)]
    static void BuildPrefabsOnly()
    {
        BuildPrefabs();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("[ProjectBuilder] 预制体构建完成");
    }

    [MenuItem("Tools/无尽跑酷/项目设置（Force Text + Build Settings）", false, 12)]
    static void SetupOnly()
    {
        SetupProjectSettings();
        SetupBuildSettings();
        UnityEngine.Debug.Log("[ProjectBuilder] 项目设置完成");
    }

    // ============================================================
    // 项目设置
    // ============================================================

    static void SetupProjectSettings()
    {
        // Force Text 序列化
        EditorSettings.serializationMode = SerializationMode.ForceText;

        // Visible Meta Files
        EditorSettings.externalVersionControl = "Visible Meta Files";

        UnityEngine.Debug.Log("[ProjectBuilder] 项目设置: Force Text + Visible Meta Files");
    }

    static void SetupBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        // 查找所有场景
        string[] sceneGuids = AssetDatabase.FindAssets("t:scene", new string[] { "Assets/Scenes" });
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        if (scenes.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[ProjectBuilder] 未找到任何场景，请先构建 Game.unity");
            return;
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log("[ProjectBuilder] Build Settings: " + scenes.Count + " 个场景");
    }

    // ============================================================
    // 构建 Game.unity 场景
    // ============================================================

    static void BuildGameScene()
    {
        // 确保目录存在
        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        // 创建新场景
        EditorApplication.NewScene();

        // === 1. Directional Light ===
        // Unity 默认创建时已有，获取或创建
        Light light = GameObject.FindObjectOfType<Light>();
        if (light == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // === 2. Main Camera ===
        Camera cam = GameObject.FindObjectOfType<Camera>();
        GameObject camObj;
        if (cam != null)
        {
            camObj = cam.gameObject;
            camObj.name = "Main Camera";
        }
        else
        {
            camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
        }
        camObj.transform.position = new Vector3(0f, 8f, -12f); // CameraFollow 会自动跟随玩家
        camObj.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
        camObj.AddComponent<CameraFollow>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.5f;

        // === 3. 三条赛道 ===
        string[] laneNames = new string[] { "Track_Lane0", "Track_Lane1", "Track_Lane2" };
        float[] laneX = new float[] { -4f, 0f, 4f };
        Color[] laneColors = new Color[] {
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.35f, 0.35f, 0.35f),
            new Color(0.5f, 0.5f, 0.5f)
        };

        for (int i = 0; i < 3; i++)
        {
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.name = laneNames[i];
            track.transform.position = new Vector3(laneX[i], 0f, 200f);
            track.transform.localScale = new Vector3(5f, 0.5f, 400f);
            track.transform.parent = null; // 根节点

            Renderer renderer = track.GetComponent<Renderer>();
            if (renderer != null)
            {
                SetCubeColor(track, laneColors[i]);
            }

            // 确保有碰撞体
            Collider col = track.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false;
            }
        }

        // === 4. 游戏管理器集合 ===
        // GameManager
        GameObject gmObj = CreateScriptObject("GameManager", typeof(GameManager));
        
        // TrackManager  
        CreateScriptObject("TrackManager", typeof(TrackManager));

        // ObstacleSpawner
        GameObject osObj = CreateScriptObject("ObstacleSpawner", typeof(ObstacleSpawner));

        // TrainController
        GameObject tcObj = CreateScriptObject("TrainController", typeof(TrainController));
        tcObj.transform.position = new Vector3(0f, 1.5f, -100f);

        // TerrainCollapse
        GameObject tcolObj = CreateScriptObject("TerrainCollapse", typeof(TerrainCollapse));

        // CoinSpawner
        GameObject csObj = CreateScriptObject("CoinSpawner", typeof(CoinSpawner));

        // PowerUpManager
        GameObject puObj = CreateScriptObject("PowerUpManager", typeof(PowerUpManager));

        // SkillManager
        GameObject smObj = CreateScriptObject("SkillManager", typeof(SkillManager));

        // TimeRewind
        GameObject trObj = CreateScriptObject("TimeRewind", typeof(TimeRewind));

        // UltimateDash
        GameObject udObj = CreateScriptObject("UltimateDash", typeof(UltimateDash));

        // PhantomClone
        GameObject pcObj = CreateScriptObject("PhantomClone", typeof(PhantomClone));

        // HUDController
        GameObject hudObj = CreateScriptObject("HUDController", typeof(HUDController));

        // UIManager
        GameObject uiObj = CreateScriptObject("UIManager", typeof(UIManager));

        // AdaptiveDifficulty
        GameObject adObj = CreateScriptObject("AdaptiveDifficulty", typeof(AdaptiveDifficulty));


        // === 5. 玩家 ===
        GameObject playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(0f, 1.5f, 0f);

        // CharacterController
        CharacterController controller = playerObj.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;

        // PlayerController 脚本
        playerObj.AddComponent<PlayerController>();

        // 玩家视觉：Capsule 身体
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "PlayerBody";
        body.transform.parent = playerObj.transform;
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

        Collider bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null) bodyCol.enabled = false;

        if (bodyRenderer != null) SetCubeColor(body, Color.white);

        // 玩家视觉：Sphere 头部
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "PlayerHead";
        head.transform.parent = playerObj.transform;
        head.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        Collider headCol = head.GetComponent<Collider>();
        if (headCol != null) headCol.enabled = false;

        if (head.GetComponent<Renderer>() != null) SetCubeColor(head, Color.white);

        // === 6. 保存场景 ===
        string scenePath = "Assets/Scenes/Game.unity";
        EditorApplication.SaveScene(scenePath);

        UnityEngine.Debug.Log("[ProjectBuilder] Game.unity 构建完成，包含 " +
            (17 + 1) + " 个管理对象 + 玩家");
    }

    static GameObject CreateScriptObject(string name, System.Type scriptType)
    {
        GameObject obj = new GameObject(name);
        obj.AddComponent(scriptType);
        return obj;
    }

    // ============================================================
    // 构建预制体
    // ============================================================

    static void BuildPrefabs()
    {
        // 确保目录存在
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }
        if (!Directory.Exists("Assets/Prefabs/Obstacles"))
        {
            Directory.CreateDirectory("Assets/Prefabs/Obstacles");
        }
        if (!Directory.Exists("Assets/Prefabs/Items"))
        {
            Directory.CreateDirectory("Assets/Prefabs/Items");
        }

        // === 静态障碍物 ===
        CreateStonePrefab();
        CreateWallPrefab();
        CreateSpikesPrefab();
        CreateGapPrefab();

        // === 动态障碍物 ===
        CreateSpinnerPrefab();

        // === 陷阱（标记预制体）===
        CreatePitPrefab();
        CreateDeathZonePrefab();

        // === 道具 ===
        CreateCoinPrefab();
        CreateDoubleScorePrefab();

        // === 列车 ===
        CreateTrainPrefab();

        // === 地形塌陷标记 ===
        // （不需要预制体，TrackManager已处理）

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log("[ProjectBuilder] 预制体全部构建完成");
    }

    static void CreateStonePrefab()
    {
        GameObject obj = new GameObject("Stone_O01");
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = obj.transform;
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        // Renderer handled by SetCubeColor
        SetCubeColor(cube, new Color(0.25f, 0.25f, 0.25f));

        Collider col = cube.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        obj.AddComponent<ObstacleTag>();

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/Stone.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateWallPrefab()
    {
        GameObject obj = new GameObject("Wall_O02");
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = obj.transform;
        cube.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        cube.transform.localScale = new Vector3(6f, 5f, 1f);

        // Renderer handled by SetCubeColor
        SetCubeColor(cube, new Color(0.6f, 0.15f, 0.15f));

        Collider col = cube.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        obj.AddComponent<ObstacleTag>();

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/Wall.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateSpikesPrefab()
    {
        GameObject obj = new GameObject("Spikes_O03");
        obj.AddComponent<ObstacleTag>();

        for (int i = 0; i < 3; i++)
        {
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "Spike_" + i;
            spike.transform.parent = obj.transform;
            spike.transform.localPosition = new Vector3((i - 1) * 0.5f, 0.3f, 0f);
            spike.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);

            Renderer r = spike.GetComponent<Renderer>();
            if (r != null) SetCubeColor(spike, Color.red);

            Collider col = spike.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
        }

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/Spikes.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateGapPrefab()
    {
        // 断台只是一个标记对象，实际处理在碰撞检测中
        GameObject obj = new GameObject("Gap_O04");
        obj.AddComponent<ObstacleTag>().isTrap = true;

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/Gap.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateSpinnerPrefab()
    {
        GameObject obj = new GameObject("Spinner_O05");

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.transform.parent = obj.transform;
        bar.transform.localPosition = new Vector3(0f, 2f, 0f);
        bar.transform.localScale = new Vector3(6f, 0.5f, 0.5f);

        // Renderer handled by SetCubeColor
        SetCubeColor(bar, new Color(0.7f, 0.6f, 0.1f));

        Collider col = bar.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        ObstacleTag tag = obj.AddComponent<ObstacleTag>();
        tag.isDynamic = true;

        // 旋转轴立柱
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.parent = obj.transform;
        pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        pole.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);

        Renderer pr = pole.GetComponent<Renderer>();
        if (pr != null) SetCubeColor(pole, new Color(0.4f, 0.35f, 0.1f));

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/Spinner.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreatePitPrefab()
    {
        // 深坑标记
        GameObject obj = new GameObject("Pit_O08");
        ObstacleTag tag = obj.AddComponent<ObstacleTag>();
        tag.isTrap = true;

        // 可视指示器：黑色平板
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.transform.parent = obj.transform;
        plate.transform.localPosition = new Vector3(0f, -2f, 0f);
        plate.transform.localScale = new Vector3(6f, 0.1f, 10f);

        Renderer r = plate.GetComponent<Renderer>();
        if (r != null) SetCubeColor(plate, Color.black);

        Collider col = plate.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/Pit.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateDeathZonePrefab()
    {
        GameObject obj = new GameObject("DeathZone_O09");
        ObstacleTag tag = obj.AddComponent<ObstacleTag>();
        tag.isTrap = true;

        // 红色警示区域
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.transform.parent = obj.transform;
        zone.transform.localPosition = new Vector3(0f, 1f, 0f);
        zone.transform.localScale = new Vector3(2.5f, 3f, 3f);

        // Renderer handled by SetCubeColor
        SetCubeColor(zone, new Color(1f, 0.1f, 0.1f));

        Collider col = zone.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        CreatePrefabAsset(obj, "Assets/Prefabs/Obstacles/DeathZone.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateCoinPrefab()
    {
        GameObject obj = new GameObject("Coin");

        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.transform.parent = obj.transform;
        coin.transform.localPosition = Vector3.zero;
        coin.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        Renderer r = coin.GetComponent<Renderer>();
        if (r != null) SetCubeColor(coin, new Color(1f, 0.85f, 0.1f));

        Collider col = coin.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        obj.AddComponent<CoinPickup>();

        CreatePrefabAsset(obj, "Assets/Prefabs/Items/Coin.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateDoubleScorePrefab()
    {
        GameObject obj = new GameObject("DoubleScore");

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = obj.transform;
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        Renderer r = cube.GetComponent<Renderer>();
        if (r != null) SetCubeColor(cube, Color.yellow);

        Collider col = cube.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        obj.AddComponent<DoubleScorePickup>();

        CreatePrefabAsset(obj, "Assets/Prefabs/Items/DoubleScore.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreateTrainPrefab()
    {
        GameObject obj = new GameObject("Train");

        // 车头
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "TrainHead";
        head.transform.parent = obj.transform;
        head.transform.localPosition = new Vector3(0f, 2f, 3f);
        head.transform.localScale = new Vector3(5f, 3f, 3f);

        Renderer hr = head.GetComponent<Renderer>();
        if (hr != null) SetCubeColor(head, new Color(0.5f, 0.1f, 0.1f));

        Collider hc = head.GetComponent<Collider>();
        if (hc != null) hc.isTrigger = false;

        // 车身
        for (int i = 0; i < 3; i++)
        {
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "TrainCar_" + i;
            car.transform.parent = obj.transform;
            car.transform.localPosition = new Vector3(0f, 2f, -1f - i * 3f);
            car.transform.localScale = new Vector3(4.5f, 3f, 3.5f);

            Renderer cr = car.GetComponent<Renderer>();
            if (cr != null) SetCubeColor(car, new Color(0.4f, 0.08f, 0.08f));

            Collider cc = car.GetComponent<Collider>();
            if (cc != null) cc.isTrigger = false;
        }

        CreatePrefabAsset(obj, "Assets/Prefabs/Items/Train.prefab");
        GameObject.DestroyImmediate(obj);
    }

    static void CreatePrefabAsset(GameObject obj, string path)
    {
        // 确保父目录存在
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 删除已存在的预制体
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        PrefabUtility.CreatePrefab(path, obj);
        UnityEngine.Debug.Log("[ProjectBuilder] 预制体: " + path);
    }

    // ============================================================
    // 自动连线预制体引用到场景组件
    // ============================================================

    [MenuItem("Tools/无尽跑酷/连线预制体引用", false, 20)]
    static void WireUpPrefabReferences()
    {
        // 加载预制体
        GameObject stonePrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/Stone.prefab", typeof(GameObject)) as GameObject;
        GameObject wallPrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/Wall.prefab", typeof(GameObject)) as GameObject;
        GameObject spikesPrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/Spikes.prefab", typeof(GameObject)) as GameObject;
        GameObject gapPrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/Gap.prefab", typeof(GameObject)) as GameObject;
        GameObject spinnerPrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/Spinner.prefab", typeof(GameObject)) as GameObject;
        GameObject pitPrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/Pit.prefab", typeof(GameObject)) as GameObject;
        GameObject deathZonePrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Obstacles/DeathZone.prefab", typeof(GameObject)) as GameObject;
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Items/Coin.prefab", typeof(GameObject)) as GameObject;
        GameObject doubleScorePrefab = AssetDatabase.LoadAssetAtPath(
            "Assets/Prefabs/Items/DoubleScore.prefab", typeof(GameObject)) as GameObject;
        AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Items/Train.prefab", typeof(GameObject));
            "Assets/Prefabs/Items/Train.prefab", typeof(GameObject)) as GameObject;

        // 连线 ObstacleSpawner
        ObstacleSpawner obstacleSpawner = GameObject.FindObjectOfType<ObstacleSpawner>();
        if (obstacleSpawner != null)
        {
            obstacleSpawner.staticObstaclePrefabs = new GameObject[] {
                stonePrefab, wallPrefab, spikesPrefab, gapPrefab
            };
            obstacleSpawner.dynamicObstaclePrefab = spinnerPrefab;
            obstacleSpawner.trapPrefabs = new GameObject[] {
                pitPrefab, deathZonePrefab
            };
            UnityEngine.Debug.Log("[ProjectBuilder] ObstacleSpawner: 预制体连线完成");
        }

        // 连线 CoinSpawner
        CoinSpawner coinSpawner = GameObject.FindObjectOfType<CoinSpawner>();
        if (coinSpawner != null && coinPrefab != null)
        {
            coinSpawner.coinPrefab = coinPrefab;
            UnityEngine.Debug.Log("[ProjectBuilder] CoinSpawner: 预制体连线完成");
        }

        // 连线 PowerUpManager
        PowerUpManager powerUpManager = GameObject.FindObjectOfType<PowerUpManager>();
        if (powerUpManager != null && doubleScorePrefab != null)
        {
            powerUpManager.doubleScorePrefab = doubleScorePrefab;
            UnityEngine.Debug.Log("[ProjectBuilder] PowerUpManager: 预制体连线完成");
        }

        EditorUtility.SetDirty(obstacleSpawner);
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log("[ProjectBuilder] 预制体引用连线全部完成");
    }
}
