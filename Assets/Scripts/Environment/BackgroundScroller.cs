using UnityEngine;
using System.Collections.Generic;

public class BackgroundScroller : MonoBehaviour
{
    // === 背景层配置 ===
    [System.Serializable]
    public class BackgroundLayer
    {
        public string layerName = "Layer";
        public Color cubeColor = Color.gray;
        public int cubeCount = 30;
        public float scrollSpeedFactor = 0.3f;
        public float spawnZRange = 80f;
        public float lateralRange = 15f;
        public float minY = 3f;
        public float maxY = 15f;
        public float minScale = 0.8f;
        public float maxScale = 3f;
        [HideInInspector]
        public List<GameObject> cubes = new List<GameObject>();
    }

    public BackgroundLayer[] layers;
    public PlayerController player;

    // === 默认层配置（Inspector 未设置时使用） ===
    private bool useDefaults = false;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        // 如果未在 Inspector 中配置层，使用默认 4 层
        if (layers == null || layers.Length == 0)
        {
            useDefaults = true;
            layers = new BackgroundLayer[4];

            // 第 1 层：远景天空（浅蓝）
            layers[0] = new BackgroundLayer();
            layers[0].layerName = "SkyDistant";
            layers[0].cubeColor = new Color(0.6f, 0.8f, 1f);
            layers[0].cubeCount = 40;
            layers[0].scrollSpeedFactor = 0.1f;
            layers[0].spawnZRange = 120f;
            layers[0].lateralRange = 25f;
            layers[0].minY = 8f;
            layers[0].maxY = 20f;
            layers[0].minScale = 1.5f;
            layers[0].maxScale = 4f;

            // 第 2 层：远建筑（浅灰）
            layers[1] = new BackgroundLayer();
            layers[1].layerName = "FarBuildings";
            layers[1].cubeColor = new Color(0.7f, 0.7f, 0.75f);
            layers[1].cubeCount = 35;
            layers[1].scrollSpeedFactor = 0.25f;
            layers[1].spawnZRange = 100f;
            layers[1].lateralRange = 20f;
            layers[1].minY = 3f;
            layers[1].maxY = 12f;
            layers[1].minScale = 1f;
            layers[1].maxScale = 3f;

            // 第 3 层：中建筑（中灰）
            layers[2] = new BackgroundLayer();
            layers[2].layerName = "MidBuildings";
            layers[2].cubeColor = new Color(0.55f, 0.55f, 0.6f);
            layers[2].cubeCount = 30;
            layers[2].scrollSpeedFactor = 0.45f;
            layers[2].spawnZRange = 80f;
            layers[2].lateralRange = 18f;
            layers[2].minY = 2f;
            layers[2].maxY = 10f;
            layers[2].minScale = 0.8f;
            layers[2].maxScale = 2.5f;

            // 第 4 层：近景（白色/亮灰）
            layers[3] = new BackgroundLayer();
            layers[3].layerName = "NearDetails";
            layers[3].cubeColor = new Color(0.85f, 0.85f, 0.9f);
            layers[3].cubeCount = 20;
            layers[3].scrollSpeedFactor = 0.7f;
            layers[3].spawnZRange = 60f;
            layers[3].lateralRange = 12f;
            layers[3].minY = 1.5f;
            layers[3].maxY = 6f;
            layers[3].minScale = 0.5f;
            layers[3].maxScale = 1.5f;
        }

        // 初始化所有层的 Cube
        for (int i = 0; i < layers.Length; i++)
        {
            InitializeLayer(layers[i]);
        }
    }

    void InitializeLayer(BackgroundLayer layer)
    {
        float playerZ = (player != null) ? player.transform.position.z : 0f;

        for (int j = 0; j < layer.cubeCount; j++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = layer.layerName + "_" + j;

            // 随机大小
            float scale = Random.Range(layer.minScale, layer.maxScale);
            float scaleX = scale * Random.Range(0.5f, 1.5f);
            float scaleY = scale * Random.Range(0.5f, 2f);
            float scaleZ = scale * Random.Range(0.5f, 1.5f);
            cube.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            // 随机位置
            float x = Random.Range(-layer.lateralRange, layer.lateralRange);
            float y = Random.Range(layer.minY, layer.maxY) + scaleY * 0.5f;
            float z = playerZ + Random.Range(0f, layer.spawnZRange);
            cube.transform.position = new Vector3(x, y, z);

            // 颜色
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 微调颜色变化，增加视觉丰富度
                float variation = Random.Range(-0.1f, 0.1f);
                Color c = layer.cubeColor;
                c.r = Mathf.Clamp01(c.r + variation);
                c.g = Mathf.Clamp01(c.g + variation);
                c.b = Mathf.Clamp01(c.b + variation);
                renderer.material.color = c;
            }

            // 移除碰撞体（仅装饰）
            Collider col = cube.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            layer.cubes.Add(cube);
        }
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        float playerSpeed = player.GetSpeed();
        float playerZ = player.transform.position.z;

        for (int i = 0; i < layers.Length; i++)
        {
            BackgroundLayer layer = layers[i];
            float scrollSpeed = playerSpeed * layer.scrollSpeedFactor;

            for (int j = 0; j < layer.cubes.Count; j++)
            {
                GameObject cube = layer.cubes[j];
                if (cube == null) continue;

                // 向玩家方向移动（视差滚动）
                Vector3 pos = cube.transform.position;
                pos.z -= scrollSpeed * Time.deltaTime;

                // 回收：超出玩家后方距离则重置到前方
                if (pos.z < playerZ - 20f)
                {
                    pos.z = playerZ + layer.spawnZRange;
                    pos.x = Random.Range(-layer.lateralRange, layer.lateralRange);

                    float scaleY = cube.transform.localScale.y;
                    pos.y = Random.Range(layer.minY, layer.maxY) + scaleY * 0.5f;
                }

                cube.transform.position = pos;
            }
        }
    }

    // 重置所有层（用于新游戏）
    public void ResetAllLayers()
    {
        float playerZ = (player != null) ? player.transform.position.z : 0f;

        for (int i = 0; i < layers.Length; i++)
        {
            BackgroundLayer layer = layers[i];
            for (int j = 0; j < layer.cubes.Count; j++)
            {
                GameObject cube = layer.cubes[j];
                if (cube == null) continue;

                Vector3 pos = cube.transform.position;
                pos.z = playerZ + Random.Range(0f, layer.spawnZRange);
                pos.x = Random.Range(-layer.lateralRange, layer.lateralRange);
                cube.transform.position = pos;
            }
        }
    }
}