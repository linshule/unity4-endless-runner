using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    public PlayerController player;

    public int buildingsPerLayer = 12;
    public float recycleDistance = 30f;
    public float spawnRangeZ = 120f;

    private class LayerConfig
    {
        public float speedMultiplier;
        public Color color;
        public float xRange;
        public float yBase;
        public float scaleMin;
        public float scaleMax;
    }

    private LayerConfig[] layers = new LayerConfig[]
    {
        new LayerConfig { speedMultiplier = 0.15f, color = new Color(0.4f, 0.4f, 0.4f), xRange = 20f, yBase = 1f, scaleMin = 3f, scaleMax = 8f },
        new LayerConfig { speedMultiplier = 0.08f, color = new Color(0.55f, 0.55f, 0.55f), xRange = 30f, yBase = 0.5f, scaleMin = 5f, scaleMax = 12f },
        new LayerConfig { speedMultiplier = 0.04f, color = new Color(0.7f, 0.7f, 0.8f), xRange = 40f, yBase = 0f, scaleMin = 8f, scaleMax = 16f },
    };

    private GameObject[][] layerBuildings;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        layerBuildings = new GameObject[layers.Length][];

        for (int layer = 0; layer < layers.Length; layer++)
        {
            layerBuildings[layer] = new GameObject[buildingsPerLayer];
            LayerConfig cfg = layers[layer];

            for (int i = 0; i < buildingsPerLayer; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "BgBuilding_L" + layer + "_" + i;
                building.transform.parent = transform;

                Renderer r = building.GetComponent<Renderer>();
                if (r != null) r.material.color = cfg.color;

                building.transform.localScale = new Vector3(
                    Random.Range(2f, 5f),
                    Random.Range(cfg.scaleMin, cfg.scaleMax),
                    Random.Range(2f, 4f)
                );

                float x = (Random.value < 0.5f ? -1f : 1f) * Random.Range(6f, cfg.xRange);
                float z = Random.Range(0f, spawnRangeZ);
                building.transform.position = new Vector3(x, cfg.yBase + building.transform.localScale.y * 0.5f, z);

                layerBuildings[layer][i] = building;
            }
        }
    }

    void Update()
    {
        if (player == null || player.isDead) return;

        float playerZ = player.transform.position.z;

        for (int layer = 0; layer < layers.Length; layer++)
        {
            float speed = player.GetSpeed() * layers[layer].speedMultiplier * Time.deltaTime;

            for (int i = 0; i < buildingsPerLayer; i++)
            {
                GameObject b = layerBuildings[layer][i];
                if (b == null) continue;

                Vector3 pos = b.transform.position;
                pos.z -= speed;

                if (pos.z < playerZ - recycleDistance)
                {
                    pos.z = playerZ + spawnRangeZ;
                    pos.x = (Random.value < 0.5f ? -1f : 1f) * Random.Range(6f, layers[layer].xRange);
                    pos.y = layers[layer].yBase + b.transform.localScale.y * 0.5f;
                }

                b.transform.position = pos;
            }
        }
    }
}
