using UnityEngine;
using System.Collections;

public class PhantomClone : MonoBehaviour
{
    // S03 幻影分身
    public PlayerController player;
    public float cloneDuration = 6f;
    public float cloneOffsetZ = 10f;

    private bool clonesActive = false;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;
        if (clonesActive) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            SkillManager sm = SkillManager.Instance;
            if (sm != null && sm.TryUseSkill("Clone"))
            {
                StartCoroutine(SpawnClones());
            }
        }
    }

    IEnumerator SpawnClones()
    {
        clonesActive = true;

        // 三轨道各生成一个半透明分身
        GameObject[] clones = new GameObject[3];
        float[] laneX = new float[] { -4f, 0f, 4f };

        for (int i = 0; i < 3; i++)
        {
            clones[i] = CreateCloneVisual();
            clones[i].transform.position = new Vector3(
                laneX[i],
                player.transform.position.y,
                player.transform.position.z + cloneOffsetZ
            );
        }

        float elapsed = 0f;
        while (elapsed < cloneDuration)
        {
            elapsed += Time.deltaTime;

            // 分身跟随玩家 Z 轴偏移
            for (int i = 0; i < 3; i++)
            {
                if (clones[i] != null)
                {
                    Vector3 pos = clones[i].transform.position;
                    pos.z = player.transform.position.z + cloneOffsetZ;
                    clones[i].transform.position = pos;
                }
            }

            yield return null;
        }

        // 清除分身
        for (int i = 0; i < 3; i++)
        {
            if (clones[i] != null)
                GameObject.Destroy(clones[i]);
        }

        clonesActive = false;
    }

    GameObject CreateCloneVisual()
    {
        GameObject clone = new GameObject("PhantomClone");

        // Capsule 身体
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.parent = clone.transform;
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            Color c = new Color(0.4f, 0.6f, 1f, 0.5f);
            bodyRenderer.material.color = c;
        }

        // Sphere 头部
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.parent = clone.transform;
        head.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        Renderer headRenderer = head.GetComponent<Renderer>();
        if (headRenderer != null)
        {
            headRenderer.material.color = new Color(0.4f, 0.6f, 1f, 0.5f);
        }

        // 触发碰撞器用于拾取金币
        SphereCollider sc = clone.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.5f;
        sc.center = new Vector3(0f, 1f, 0f);
        clone.AddComponent<PhantomCloneCollector>();

        return clone;
    }
}
