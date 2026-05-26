using UnityEngine;
using System.Collections;

public class PhantomClone : MonoBehaviour
{
    public PlayerController player;
    public float cloneDuration = 6f;
    public float cloneOffsetZ = 5f;

    private bool clonesActive = false;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (clonesActive) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

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

        float[] laneX = new float[] { -6f, 0f, 6f };
        GameObject[] clones = new GameObject[3];

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

            for (int i = 0; i < 3; i++)
            {
                if (clones[i] != null)
                {
                    Rigidbody crb = clones[i].GetComponent<Rigidbody>();
                    Vector3 newPos = new Vector3(
                        clones[i].transform.position.x,
                        player.transform.position.y,
                        player.transform.position.z + cloneOffsetZ
                    );
                    if (crb != null) crb.MovePosition(newPos);
                    else clones[i].transform.position = newPos;

                    // 淡出效果
                    float alpha = Mathf.Lerp(0.5f, 0.1f, elapsed / cloneDuration);
                    Renderer[] renderers = clones[i].GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers)
                    {
                        Color c = r.material.color;
                        c.a = alpha;
                        r.material.color = c;
                    }
                }
            }

            yield return null;
        }

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

        // 需要 Rigidbody 才能触发 OnTriggerEnter（Unity 物理引擎要求）
        Rigidbody rb = clone.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 半透明材质 Shader
        Shader transShader = Shader.Find("Transparent/Diffuse");
        if (transShader == null) transShader = Shader.Find("Diffuse");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.parent = clone.transform;
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        Renderer br = body.GetComponent<Renderer>();
        if (br != null)
        {
            br.material.shader = transShader;
            br.material.color = new Color(0.3f, 0.7f, 1f, 0.4f);
        }
        Collider bc = body.GetComponent<Collider>();
        if (bc != null) bc.enabled = false;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.parent = clone.transform;
        head.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        Renderer hr = head.GetComponent<Renderer>();
        if (hr != null)
        {
            hr.material.shader = transShader;
            hr.material.color = new Color(0.3f, 0.7f, 1f, 0.4f);
        }
        Collider hc = head.GetComponent<Collider>();
        if (hc != null) hc.enabled = false;

        // 触发碰撞器拾取金币（Rigidbody 保证 OnTriggerEnter 生效）
        SphereCollider sc = clone.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 2f;
        sc.center = new Vector3(0f, 1f, 0f);
        clone.AddComponent<PhantomCloneCollector>();

        return clone;
    }
}