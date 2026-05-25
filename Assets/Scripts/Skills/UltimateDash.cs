using UnityEngine;
using System.Collections;

public class UltimateDash : MonoBehaviour
{
    public PlayerController player;
    public float dashDistance = 30f;
    public float dashDuration = 0.4f;

    private bool isDashing = false;
    private CharacterController controller;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        if (player != null)
            controller = player.GetComponent<CharacterController>();
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

        Vector3 startPos = player.transform.position;
        Vector3 endPos = startPos + Vector3.forward * dashDistance;

        // 冲刺轨迹（临时Cube）
        GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trail.name = "DashTrail";
        trail.transform.position = startPos + Vector3.up * 1f;
        trail.transform.localScale = new Vector3(0.5f, 1.5f, dashDistance);
        trail.transform.rotation = Quaternion.identity;
        Renderer tr = trail.GetComponent<Renderer>();
        if (tr != null)
        {
            tr.material.color = new Color(0f, 0.8f, 1f, 0.5f);
        }
        Collider tc = trail.GetComponent<Collider>();
        if (tc != null) tc.enabled = false;

        // 关闭碰撞（冲刺无敌）
        if (controller != null)
            controller.detectCollisions = false;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            player.SetPosition(Vector3.Lerp(startPos, endPos, t));

            // 轨迹淡出
            if (tr != null)
                tr.material.color = new Color(0f, 0.8f, 1f, 0.5f * (1f - t));

            yield return null;
        }

        player.SetPosition(endPos);

        if (controller != null)
            controller.detectCollisions = true;

        // 冲刺推开列车
        TrainController train = FindObjectOfType<TrainController>();
        if (train != null)
        {
            train.AddDistance(15f);
        }

        GameObject.Destroy(trail, 0.3f);
        isDashing = false;
    }
}