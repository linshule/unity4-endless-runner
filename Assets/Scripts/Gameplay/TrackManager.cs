using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public PlayerController player;
    
    // === 赛道配置 ===
    public float trackLength = 200f;
    public float trackWidth = 1.5f;
    public float laneSpacing = 2f;
    public float trackY = 0f;
    public float trackThickness = 0.5f;

    // === 预制体/材质颜色 ===
    public Color leftColor = new Color(0.5f, 0.5f, 0.5f);
    public Color centerColor = new Color(0.3f, 0.3f, 0.3f);
    public Color rightColor = new Color(0.5f, 0.5f, 0.5f);

    private GameObject[] tracks = new GameObject[3];

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        CreateTracks();
    }

    void CreateTracks()
    {
        float[] lanesX = new float[] { -2f, 0f, 2f };
        Color[] colors = new Color[] { leftColor, centerColor, rightColor };

        for (int i = 0; i < 3; i++)
        {
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.name = "Track_Lane" + i;
            track.transform.position = new Vector3(lanesX[i], trackY, trackLength * 0.5f);
            track.transform.localScale = new Vector3(trackWidth, trackThickness, trackLength);

            Renderer renderer = track.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = colors[i];
            }

            tracks[i] = track;

            // 添加 CollapseSegment 组件用于塌陷
            CollapseSegment seg = track.AddComponent<CollapseSegment>();
            seg.laneIndex = i;
        }
    }

    public GameObject GetTrack(int lane)
    {
        if (lane >= 0 && lane < 3)
            return tracks[lane];
        return null;
    }
}
