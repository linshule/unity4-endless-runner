using UnityEngine;
using System.Collections;

public class TerrainCollapse : MonoBehaviour
{
    public float minCollapseInterval = 8f;
    public float maxCollapseInterval = 15f;
    public float warningTime = 0.8f;
    public float trapDuration = 3f;

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
            nextCollapseTime = Time.time + Random.Range(minCollapseInterval, maxCollapseInterval);
        }
    }

    IEnumerator CollapseLane(int lane)
    {
        isCollapsing = true;

        float laneX = (lane - 1) * 6f;
        float trapZ = player.transform.position.z + Random.Range(10f, 22f);

        // 创建塌陷标记：贴在跑道表面的薄片
        GameObject trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trap.name = "CollapseTrap";
        trap.transform.position = new Vector3(laneX, 0.27f, trapZ);
        trap.transform.localScale = new Vector3(4.5f, 0.08f, 4.5f);
        Renderer trapR = trap.GetComponent<Renderer>();
        if (trapR != null) trapR.material.color = new Color(1f, 0.5f, 0f);

        // 禁用默认碰撞体
        Collider defCol = trap.GetComponent<Collider>();
        if (defCol != null) defCol.enabled = false;

        // 预警闪烁（黄色↔橙色）
        float warnEnd = Time.time + warningTime;
        while (Time.time < warnEnd)
        {
            if (trapR != null)
            {
                trapR.material.color = (Mathf.Floor(Time.time * 6f) % 2 == 0)
                    ? Color.yellow
                    : new Color(1f, 0.3f, 0f);
            }
            yield return null;
        }

        // 变成致命陷阱：变红 + 添加即死触发器
        if (trapR != null) trapR.material.color = new Color(0.8f, 0.1f, 0.1f);

        BoxCollider bc = trap.AddComponent<BoxCollider>();
        bc.size = new Vector3(4.5f, 1.5f, 4.5f);
        bc.center = new Vector3(0f, 0.8f, 0f);
        bc.isTrigger = true;

        Rigidbody rb = trap.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        CollapseTrap ct = trap.AddComponent<CollapseTrap>();

        // 陷阱持续
        yield return new WaitForSeconds(trapDuration);

        // 恢复：移除陷阱组件，标记变暗消失
        Destroy(ct);
        Destroy(bc);
        Destroy(rb);

        float fadeStart = Time.time;
        while (Time.time - fadeStart < 0.5f)
        {
            if (trapR != null)
            {
                float t = (Time.time - fadeStart) / 0.5f;
                trapR.material.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(0.3f, 0.3f, 0.3f), t);
            }
            yield return null;
        }

        GameObject.Destroy(trap);
        isCollapsing = false;
    }

    public void SetCollapseFrequency(float interval)
    {
        maxCollapseInterval = interval;
        minCollapseInterval = interval * 0.5f;
    }
}