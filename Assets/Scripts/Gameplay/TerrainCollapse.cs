using UnityEngine;
using System.Collections;

public class TerrainCollapse : MonoBehaviour
{
    // === 塌陷参数 ===
    public float minCollapseInterval = 25f;
    public float maxCollapseInterval = 40f;
    public float collapseDuration = 4f;
    public float warningTime = 1f;
    public float sinkDepth = 3f;

    private float nextCollapseTime;
    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        nextCollapseTime = Time.time + Random.Range(15f, 25f);
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
        float patchZ = player.transform.position.z + Random.Range(15f, 30f);

        // 创建小块塌陷标记（4x4m，仅视觉）
        GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        patch.name = "CollapsePatch";
        patch.transform.position = new Vector3(laneX, 0.26f, patchZ);
        patch.transform.localScale = new Vector3(4f, 0.06f, 4f);
        Renderer patchR = patch.GetComponent<Renderer>();
        if (patchR != null) patchR.material.color = new Color(0.3f, 0.3f, 0.1f);
        Collider patchC = patch.GetComponent<Collider>();
        if (patchC != null) patchC.enabled = false;

        // 黄色闪烁预警
        float warnEnd = Time.time + warningTime;
        while (Time.time < warnEnd)
        {
            if (patchR != null)
            {
                patchR.material.color = (Mathf.Floor(Time.time * 4f) % 2 == 0)
                    ? Color.yellow
                    : new Color(0.3f, 0.3f, 0.1f);
            }
            yield return null;
        }

        // 小块下沉
        float sinkStart = Time.time;
        Vector3 origPos = patch.transform.position;
        while (Time.time - sinkStart < collapseDuration)
        {
            float t = (Time.time - sinkStart) / collapseDuration;
            patch.transform.position = new Vector3(origPos.x, origPos.y - t * sinkDepth, origPos.z);
            yield return null;
        }

        // 停留一会后销毁
        yield return new WaitForSeconds(1f);
        GameObject.Destroy(patch);

        isCollapsing = false;
    }

    public void SetCollapseFrequency(float interval)
    {
        maxCollapseInterval = interval;
        minCollapseInterval = interval * 0.5f;
    }
}