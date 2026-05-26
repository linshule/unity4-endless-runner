using UnityEngine;
using System.Collections;

public class TerrainCollapse : MonoBehaviour
{
    public float minCollapseInterval = 8f;
    public float maxCollapseInterval = 15f;
    public float warningTime = 0.3f;
    public float sinkDuration = 0.5f;
    public float sinkDepth = 2f;
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

        // 塌陷区紧跟玩家脚下前方
        float baseZ = player.transform.position.z + Random.Range(2f, 6f);
        float trapLength = 8f; // 足够长，即使跑过去也还在陷阱范围内

        // 地板碎片
        GameObject floorPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorPiece.name = "CollapseFloor";
        floorPiece.transform.position = new Vector3(laneX, 0.27f, baseZ + trapLength * 0.5f);
        floorPiece.transform.localScale = new Vector3(3f, 0.08f, trapLength);
        Renderer pieceR = floorPiece.GetComponent<Renderer>();
        if (pieceR != null) pieceR.material.color = new Color(0.5f, 0.5f, 0.5f);

        // 黑洞（初始隐藏）
        GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hole.name = "CollapseHole";
        hole.transform.position = new Vector3(laneX, 0.25f, baseZ + trapLength * 0.5f);
        hole.transform.localScale = new Vector3(3f, 0.04f, trapLength);
        Renderer holeR = hole.GetComponent<Renderer>();
        if (holeR != null) holeR.material.color = new Color(0.05f, 0.05f, 0.05f);
        Collider holeCol = hole.GetComponent<Collider>();
        if (holeCol != null) holeCol.enabled = false;
        hole.SetActive(false);

        // 极快预警闪烁
        float warnEnd = Time.time + warningTime;
        while (Time.time < warnEnd)
        {
            if (pieceR != null)
                pieceR.material.color = (Mathf.Floor(Time.time * 10f) % 2 == 0) ? Color.yellow : new Color(0.5f, 0.5f, 0.5f);
            yield return null;
        }

        // 迅速下沉
        float sinkStart = Time.time;
        Vector3 origPos = floorPiece.transform.position;
        while (Time.time - sinkStart < sinkDuration)
        {
            float t = (Time.time - sinkStart) / sinkDuration;
            float y = Mathf.Lerp(origPos.y, origPos.y - sinkDepth, t * t);
            floorPiece.transform.position = new Vector3(origPos.x, y, origPos.z);
            yield return null;
        }
        floorPiece.SetActive(false);

        // 黑洞现身 + 致命碰撞体
        hole.SetActive(true);
        BoxCollider killerCol = hole.AddComponent<BoxCollider>();
        killerCol.size = new Vector3(3f, 1.5f, trapLength);
        killerCol.center = new Vector3(0f, 0.8f, 0f);
        killerCol.isTrigger = false;

        ObstacleTag tag = hole.AddComponent<ObstacleTag>();
        tag.isTrap = true;

        yield return new WaitForSeconds(trapDuration);

        // 恢复
        Destroy(killerCol);
        Destroy(tag);
        float fadeStart = Time.time;
        while (Time.time - fadeStart < 0.5f)
        {
            if (holeR != null)
                holeR.material.color = Color.Lerp(new Color(0.05f, 0.05f, 0.05f), new Color(0.4f, 0.4f, 0.4f), (Time.time - fadeStart) / 0.5f);
            yield return null;
        }
        Destroy(hole);
        Destroy(floorPiece);
        isCollapsing = false;
    }

    public void SetCollapseFrequency(float interval)
    {
        maxCollapseInterval = interval;
        minCollapseInterval = interval * 0.5f;
    }
}