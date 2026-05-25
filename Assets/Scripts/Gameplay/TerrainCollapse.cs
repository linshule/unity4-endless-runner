using UnityEngine;
using System.Collections;

public class TerrainCollapse : MonoBehaviour
{
    // === 塌陷参数 ===
    public float minCollapseInterval = 10f;
    public float maxCollapseInterval = 20f;
    public float collapseDuration = 4f;
    public float warningTime = 0.5f;
    public float sinkDepth = 3f;

    private float nextCollapseTime;
    private TrackManager trackManager;
    private PlayerController player;

    void Start()
    {
        trackManager = FindObjectOfType<TrackManager>();
        player = FindObjectOfType<PlayerController>();
        nextCollapseTime = Time.time + Random.Range(5f, 10f);
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        if (Time.time >= nextCollapseTime)
        {
            int lane = Random.Range(0, 3);
            StartCoroutine(CollapseLane(lane));

            float interval = Random.Range(minCollapseInterval, maxCollapseInterval);
            nextCollapseTime = Time.time + interval;
        }
    }

    IEnumerator CollapseLane(int lane)
    {
        GameObject track = null;
        if (trackManager != null)
        {
            track = trackManager.GetTrack(lane);
        }

        // 0.5s 黄色闪烁预警
        if (track != null)
        {
            float warnEnd = Time.time + warningTime;
            Renderer renderer = track.GetComponent<Renderer>();
            Color originalColor = renderer != null ? renderer.material.color : Color.gray;

            while (Time.time < warnEnd)
            {
                if (renderer != null)
                {
                    renderer.material.color = (Mathf.Floor(Time.time * 8f) % 2 == 0) 
                        ? Color.yellow 
                        : originalColor;
                }
                yield return null;
            }

            if (renderer != null)
            {
                renderer.material.color = originalColor;
            }
        }

        // 下沉消失
        float sinkStart = Time.time;
        Vector3 originalPos = (track != null) ? track.transform.position : Vector3.zero;

        while (Time.time - sinkStart < collapseDuration)
        {
            if (track != null)
            {
                float progress = (Time.time - sinkStart) / collapseDuration;
                float y = Mathf.Lerp(originalPos.y, originalPos.y - sinkDepth, progress);
                track.transform.position = new Vector3(originalPos.x, y, originalPos.z);
            }
            yield return null;
        }

        // 保持下沉状态一段时间
        yield return new WaitForSeconds(1f);

        // 恢复升起
        float riseStart = Time.time;
        while (Time.time - riseStart < 1.5f)
        {
            if (track != null)
            {
                float progress = (Time.time - riseStart) / 1.5f;
                float y = Mathf.Lerp(originalPos.y - sinkDepth, originalPos.y, progress);
                track.transform.position = new Vector3(originalPos.x, y, originalPos.z);
            }
            yield return null;
        }

        if (track != null)
        {
            track.transform.position = originalPos;
        }
    }

    public void SetCollapseFrequency(float interval)
    {
        maxCollapseInterval = interval;
        minCollapseInterval = interval * 0.5f;
    }
}
