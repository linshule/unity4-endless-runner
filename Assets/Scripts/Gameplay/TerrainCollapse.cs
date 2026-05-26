using UnityEngine;
using System.Collections;

public class TerrainCollapse : MonoBehaviour
{
    public float minCollapseInterval = 8f;
    public float maxCollapseInterval = 15f;
    public float warningTime = 0.8f;
    public float sinkDuration = 1.5f;
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
        float trapZ = player.transform.position.z + Random.Range(10f, 22f);

        // 创建"地板碎片"——贴在跑道表面，模拟即将塌陷的地板
        GameObject floorPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorPiece.name = "CollapseFloor";
        floorPiece.transform.position = new Vector3(laneX, 0.27f, trapZ);
        floorPiece.transform.localScale = new Vector3(4.5f, 0.08f, 4.5f);
        Renderer pieceR = floorPiece.GetComponent<Renderer>();
        if (pieceR != null) pieceR.material.color = new Color(0.5f, 0.5f, 0.5f);

        // 创建底下的黑洞标记
        GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hole.name = "CollapseHole";
        hole.transform.position = new Vector3(laneX, 0.25f, trapZ);
        hole.transform.localScale = new Vector3(4.5f, 0.04f, 4.5f);
        Renderer holeR = hole.GetComponent<Renderer>();
        if (holeR != null) holeR.material.color = new Color(0.05f, 0.05f, 0.05f);
        Collider holeCol = hole.GetComponent<Collider>();
        if (holeCol != null) holeCol.enabled = false;
        hole.SetActive(false);

        // 预警：地板碎片黄色闪烁
        float warnEnd = Time.time + warningTime;
        while (Time.time < warnEnd)
        {
            if (pieceR != null)
            {
                pieceR.material.color = (Mathf.Floor(Time.time * 6f) % 2 == 0)
                    ? Color.yellow
                    : new Color(0.5f, 0.5f, 0.5f);
            }
            yield return null;
        }

        // 下沉动画：地板碎片掉下去
        float sinkStart = Time.time;
        Vector3 origPos = floorPiece.transform.position;
        while (Time.time - sinkStart < sinkDuration)
        {
            float t = (Time.time - sinkStart) / sinkDuration;
            float y = Mathf.Lerp(origPos.y, origPos.y - sinkDepth, t * t);
            floorPiece.transform.position = new Vector3(origPos.x, y, origPos.z);
            yield return null;
        }

        // 碎片沉到底部后隐藏
        floorPiece.SetActive(false);

        // 显示黑洞 + 添加致死碰撞体
        hole.SetActive(true);

        // 用非触发器 + ObstacleTag 让 CharacterController 的 OnControllerColliderHit 检测到
        BoxCollider killerCol = hole.AddComponent<BoxCollider>();
        killerCol.size = new Vector3(4.5f, 1.5f, 4.5f);
        killerCol.center = new Vector3(0f, 0.8f, 0f);
        killerCol.isTrigger = false;

        ObstacleTag tag = hole.AddComponent<ObstacleTag>();
        tag.isTrap = true;

        // 致命陷阱持续
        yield return new WaitForSeconds(trapDuration);

        // 恢复：移除陷阱碰撞体，黑洞渐隐
        Destroy(killerCol);
        Destroy(tag);

        float fadeStart = Time.time;
        while (Time.time - fadeStart < 0.8f)
        {
            if (holeR != null)
            {
                float t = (Time.time - fadeStart) / 0.8f;
                holeR.material.color = Color.Lerp(new Color(0.05f, 0.05f, 0.05f), new Color(0.4f, 0.4f, 0.4f), t);
            }
            yield return null;
        }

        GameObject.Destroy(hole);
        GameObject.Destroy(floorPiece);
        isCollapsing = false;
    }

    public void SetCollapseFrequency(float interval)
    {
        maxCollapseInterval = interval;
        minCollapseInterval = interval * 0.5f;
    }
}