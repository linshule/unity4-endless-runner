using UnityEngine;
using System.Collections;

public class UltimateDash : MonoBehaviour
{
    public PlayerController player;
    public float dashDistance = 30f;
    public float dashDuration = 0.35f;

    private bool isDashing = false;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (isDashing) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            SkillManager sm = SkillManager.Instance;
            if (sm != null && sm.TryUseSkill("Dash"))
            {
                StartCoroutine(PerformDash());
            }
        }
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        player.isInvincible = true;
        player.isDashing = true;

        Vector3 startPos = player.transform.position;
        float distancePerStep = dashDistance / (dashDuration / Time.fixedDeltaTime);
        if (distancePerStep < 1f) distancePerStep = 1f;

        // 摧毁路径上所有障碍物
        DestroyObstaclesAlongPath(startPos, dashDistance);

        // 轨迹特效
        GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trail.name = "DashTrail";
        trail.transform.rotation = Quaternion.identity;
        Renderer tr = trail.GetComponent<Renderer>();
        Shader transS = Shader.Find("Transparent/Diffuse");
        if (transS == null) transS = Shader.Find("Diffuse");
        if (tr != null)
        {
            tr.material.shader = transS;
            tr.material.color = new Color(0f, 0.8f, 1f, 0.4f);
        }
        Collider tc = trail.GetComponent<Collider>();
        if (tc != null) tc.enabled = false;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, startPos + Vector3.forward * dashDistance, t);
            currentPos.y = startPos.y;
            player.SetPosition(currentPos);

            // 更新轨迹位置
            trail.transform.position = startPos + Vector3.up * 1f + Vector3.forward * dashDistance * 0.5f * t;
            trail.transform.localScale = new Vector3(0.5f, 1.5f, dashDistance * t);
            if (tr != null)
                tr.material.color = new Color(0f, 0.8f, 1f, 0.4f * (1f - t));

            // 沿途收集金币
            CollectCoinsAlongPath(currentPos);

            yield return null;
        }

        Vector3 endPos = startPos + Vector3.forward * dashDistance;
        endPos.y = startPos.y;
        player.SetPosition(endPos);
        CollectCoinsAlongPath(endPos);

        // 延迟取消无敌（防止恢复碰撞瞬间被判定碰撞）
        yield return new WaitForSeconds(0.25f);
        player.isInvincible = false;

        // 推开列车
        TrainController train = FindObjectOfType<TrainController>();
        if (train != null)
        {
            train.AddDistance(15f);
        }

        player.isDashing = false;
        GameObject.Destroy(trail, 0.3f);
        isDashing = false;
    }

    void CollectCoinsAlongPath(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 2.5f);
        foreach (Collider hit in hits)
        {
            CoinPickup coin = hit.GetComponent<CoinPickup>();
            if (coin != null && hit.gameObject.activeInHierarchy)
            {
                GameManager gm = GameManager.Instance;
                if (gm != null)
                    gm.coinCount += coin.coinValue;

                if (player != null)
                    player.IncreaseSpeed(0.5f);

                TrainController train = FindObjectOfType<TrainController>();
                if (train != null)
                {
                    train.AddDistance(3f);
                    train.OnCoinCollected();
                }

                hit.gameObject.SetActive(false);
            }
        }
    }

    void DestroyObstaclesAlongPath(Vector3 origin, float distance)
    {
        float step = 3f;
        for (float z = 0f; z <= distance; z += step)
        {
            Vector3 checkPos = origin + Vector3.forward * z;
            checkPos.y += 1.5f;
            Collider[] hits = Physics.OverlapSphere(checkPos, 4f);
            foreach (Collider hit in hits)
            {
                GameObject obj = hit.gameObject;
                ObstacleTag tag = obj.GetComponentInParent<ObstacleTag>();
                if (tag != null && obj.activeInHierarchy)
                {
                    if (obj.transform.parent != null)
                        obj.transform.parent.gameObject.SetActive(false);
                    else
                        obj.SetActive(false);
                }
            }
        }
    }
}