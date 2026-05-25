using UnityEngine;
using System.Collections;

public class UltimateDash : MonoBehaviour
{
    // S02 虚空冲刺
    public PlayerController player;
    public float dashDistance = 30f;
    public float dashDuration = 0.5f;
    
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
        float elapsed = 0f;

        // 冲刺期间无敌（关闭碰撞检测）
        if (controller != null)
        {
            controller.detectCollisions = false;
        }

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            player.SetPosition(Vector3.Lerp(startPos, endPos, t));
            yield return null;
        }

        player.SetPosition(endPos);

        // 恢复碰撞
        if (controller != null)
        {
            controller.detectCollisions = true;
        }

        isDashing = false;
    }
}
