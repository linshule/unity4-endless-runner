using UnityEngine;

public class TrapController : MonoBehaviour
{
    public enum TrapType
    {
        Pit,        // O08 掉落深坑
        DeathZone   // O09 即死区域
    }

    public TrapType trapType = TrapType.Pit;

    // === 即死区域间歇参数 ===
    public float activeDuration = 2f;
    public float inactiveDuration = 2f;
    private float timer = 0f;
    private bool isActive = true;

    private Renderer trapRenderer;
    private Collider trapCollider;

    void Start()
    {
        trapRenderer = GetComponent<Renderer>();
        trapCollider = GetComponent<Collider>();

        if (trapType == TrapType.DeathZone)
        {
            timer = Random.Range(0f, activeDuration + inactiveDuration);
        }
    }

    void Update()
    {
        if (trapType != TrapType.DeathZone) return;

        timer += Time.deltaTime;
        float cycleLength = activeDuration + inactiveDuration;

        bool shouldBeActive = (timer % cycleLength) < activeDuration;
        if (shouldBeActive != isActive)
        {
            isActive = shouldBeActive;
            if (trapCollider != null)
            {
                trapCollider.enabled = isActive;
            }
            if (trapRenderer != null)
            {
                // 激活时红色脉冲（通过 scale 变化模拟）
                if (isActive)
                {
                    trapRenderer.material.color = new Color(1f, 0f, 0f);
                }
                else
                {
                    trapRenderer.material.color = new Color(0.3f, 0f, 0f);
                }
            }
        }

        // 激活时脉冲动画
        if (isActive && trapRenderer != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.2f;
            transform.localScale = Vector3.one * pulse;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive && trapType == TrapType.DeathZone) return;

        if (other.CompareTag("Player"))
        {
            TriggerTrap();
        }
    }

    void TriggerTrap()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnPlayerHitObstacle();
        }
    }
}
