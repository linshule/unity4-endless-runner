using UnityEngine;
using System.Collections;

public class TrackManager : MonoBehaviour
{
    public PlayerController player;

    public float trackWidth = 6f;
    public float segmentLength = 200f;
    public int segmentCount = 3;
    public float trackY = 0f;
    public float trackThickness = 0.5f;

    public Color leftColor = new Color(0.5f, 0.5f, 0.5f);
    public Color centerColor = new Color(0.3f, 0.3f, 0.3f);
    public Color rightColor = new Color(0.5f, 0.5f, 0.5f);

    private GameObject[][] segments; // [lane][segment]
    private float[] laneX = new float[] { -6f, 0f, 6f };
    private Color[] laneColors = new Color[3];
    private float segmentSpan;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        laneColors[0] = leftColor;
        laneColors[1] = centerColor;
        laneColors[2] = rightColor;

        segmentSpan = segmentLength * segmentCount;
        segments = new GameObject[3][];

        for (int lane = 0; lane < 3; lane++)
        {
            segments[lane] = new GameObject[segmentCount];
            for (int seg = 0; seg < segmentCount; seg++)
            {
                GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
                track.name = "Track_L" + lane + "_S" + seg;
                float zCenter = seg * segmentLength + segmentLength * 0.5f;
                track.transform.position = new Vector3(laneX[lane], trackY, zCenter);
                track.transform.localScale = new Vector3(trackWidth, trackThickness, segmentLength);

                Renderer renderer = track.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = laneColors[lane];
                }

                // 碰撞体保持非trigger
                Collider col = track.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;

                segments[lane][seg] = track;
            }
        }
    }

    void Update()
    {
        if (player == null || player.isDead) return;

        float playerZ = player.transform.position.z;

        // 当玩家跑过一段，把最后那段挪到前面
        for (int lane = 0; lane < 3; lane++)
        {
            for (int seg = 0; seg < segmentCount; seg++)
            {
                GameObject track = segments[lane][seg];
                float trackEnd = track.transform.position.z + segmentLength * 0.5f;

                if (playerZ > trackEnd + segmentLength)
                {
                    // 挪到前面
                    float newZ = track.transform.position.z + segmentSpan;
                    track.transform.position = new Vector3(laneX[lane], trackY, newZ);
                }
            }
        }
    }

    public GameObject GetTrack(int lane)
    {
        if (lane < 0 || lane >= 3) return null;
        float playerZ = player != null ? player.transform.position.z : 0f;
        foreach (GameObject seg in segments[lane])
        {
            float segStart = seg.transform.position.z - segmentLength * 0.5f;
            float segEnd = seg.transform.position.z + segmentLength * 0.5f;
            if (playerZ >= segStart && playerZ < segEnd)
                return seg;
        }
        return segments[lane][0];
    }

    public void DisableTrackColliderAt(int lane, float worldZ)
    {
        if (lane < 0 || lane >= 3) return;
        foreach (GameObject seg in segments[lane])
        {
            float segStart = seg.transform.position.z - segmentLength * 0.5f;
            float segEnd = seg.transform.position.z + segmentLength * 0.5f;
            if (worldZ >= segStart && worldZ < segEnd)
            {
                Collider col = seg.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                    StartCoroutine(EnableColliderDelayed(col, 1.5f));
                }
                return;
            }
        }
    }

    IEnumerator EnableColliderDelayed(Collider col, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null) col.enabled = true;
    }
}