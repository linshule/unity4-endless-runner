using UnityEngine;
using System.Collections;

public class TerrainCollapse : MonoBehaviour
{
    // === 塌陷参数 ===
    public float minCollapseInterval = 8f;
    public float maxCollapseInterval = 15f;
    public float collapseDuration = 2f;
    public float warningTime = 0.6f;
    public float sinkDepth = 3f;
    public float trapDuration = 3f; // 塌陷后陷阱持续多久

    private float nextCollapseTime;
    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        nextCollapseTime = Time.time + Random.Range(5f, 10f);
    }

    private bool isCollapsing = false;

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        if (!isCollapsing && Time.time >= nextCollapseTime)
        {
            int lane = Random.Range(0, 3);
            StartCoroutine(CollapseLane(lane));

            float interval = Random.Range(minCollapseInterval, maxCollapseInterval);
            nextCollapseTime = Time.time + interval;
        }
    }

    IEnumerator CollapseLane(int lane)
    {
        isCollapsing = true;

        float laneX = (lane - 1) * 6f;
        float patchZ = player.transform.position.z + Random.Range(8f, 18f);

        // 创建塌陷标记（5x5m 橙色方块，放在跑道表面）
        GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        patch.name = "CollapsePatch";
        patch.transform.position = new Vector3(laneX, 0.26f, patchZ);
        patch.transform.localScale = new Vector3(5f, 0.1f, 5f);
        Renderer patchR = patch.GetComponent<Renderer>();
        if (patchR != null) patchR.material.color = new Color(1f, 0.4f, 0f);

        // 橙色闪烁预警
        float warnEnd = Time.time + warningTime;
        while (Time.time < warnEnd)
        {
            if (patchR != null)
            {
                patchR.material.color = (Mathf.Floor(Time.time * 6f) % 2 == 0)
                    ? Color.yellow
                    : new Color(1f, 0.4f, 0f);
            }
            yield return null;
        }

        // 小块下沉动画
        float sinkStart = Time.time;
        Vector3 origPos = patch.transform.position;
        while (Time.time - sinkStart < collapseDuration)
        {
            float t = (Time.time - sinkStart) / collapseDuration;
            patch.transform.position = new Vector3(origPos.x, origPos.y - t * sinkDepth, origPos.z);
            yield return null;
        }

        // 塌陷后：添加即死触发器（踩上去就死）
        BoxCollider bc = patch.AddComponent<BoxCollider>();
        bc.size = new Vector3(5f, 1f, 5f);
        bc.center = new Vector3(0f, -sinkDepth + 0.5f, 0f);
        bc.isTrigger = true;

        // 添加陷阱检测脚本
        CollapseTrap trap = patch.AddComponent<CollapseTrap>();

        // 陷阱持续 trapDuration 秒
        yield return new WaitForSeconds(trapDuration);

        // 恢复：移除陷阱，升起标记
        Destroy(trap);
        Destroy(bc);

        float riseStart = Time.time;
        Vector3 sunkPos = patch.transform.position;
        while (Time.time - riseStart < 1f)
        {
            float t = (Time.time - riseStart) / 1f;
            patch.transform.position = new Vector3(sunkPos.x, 
                Mathf.Lerp(sunkPos.y, origPos.y, t), sunkPos.z);
            yield return null;
        }

        GameObject.Destroy(patch);
        isCollapsing = false;
    }

    public void SetCollapseFrequency(float interval)
    {
        maxCollapseInterval = interval;
        minCollapseInterval = interval * 0.5f;
    }
}