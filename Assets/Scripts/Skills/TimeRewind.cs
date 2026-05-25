using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeRewind : MonoBehaviour
{
    // S01 时间回溯
    public PlayerController player;
    public GameManager gameManager;

    // === 快照记录 ===
    private struct Snapshot
    {
        public Vector3 position;
        public float time;
    }
    private List<Snapshot> snapshots = new List<Snapshot>();
    public float recordDuration = 3f;
    public float recordInterval = 0.1f;
    private float recordTimer = 0f;

    // === 回溯参数 ===
    public float rewindDuration = 1f;

    // === 死亡回溯 ===
    private bool canRewind = false;
    private bool isRewinding = false;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    void Update()
    {
        if (player == null) return;

        // 持续记录位置快照
        if (!player.isDead && !isRewinding)
        {
            recordTimer += Time.deltaTime;
            if (recordTimer >= recordInterval)
            {
                recordTimer = 0f;
                Snapshot snap = new Snapshot();
                snap.position = player.transform.position;
                snap.time = Time.time;
                snapshots.Add(snap);

                // 移除过期快照
                while (snapshots.Count > 0 && Time.time - snapshots[0].time > recordDuration)
                {
                    snapshots.RemoveAt(0);
                }
            }
        }

        // 死亡时检查回溯
        if (player.isDead && canRewind && !isRewinding)
        {
            // 通知 HUD 显示回溯按钮
            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowRewindButton(true);
            }
        }
    }

    public void TriggerRewind()
    {
        if (!canRewind || isRewinding || snapshots.Count == 0) return;
        StartCoroutine(DoRewind());
    }

    IEnumerator DoRewind()
    {
        isRewinding = true;
        canRewind = false;

        // 通知 GameManager
        if (gameManager != null)
        {
            gameManager.TriggerRewind();
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowRewindButton(false);
        }

        // 找到 2 秒前的快照
        float targetTime = Time.time - 2f;
        Snapshot targetSnap = snapshots[0];
        for (int i = snapshots.Count - 1; i >= 0; i--)
        {
            if (snapshots[i].time <= targetTime)
            {
                targetSnap = snapshots[i];
                break;
            }
        }

        // 黑白倒放效果（简化：直接回到目标位置）
        Vector3 startPos = player.transform.position;
        Vector3 endPos = targetSnap.position;
        float elapsed = 0f;

        while (elapsed < rewindDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rewindDuration;
            player.SetPosition(Vector3.Lerp(startPos, endPos, t));
            yield return null;
        }

        player.SetPosition(endPos);
        isRewinding = false;
        canRewind = false;
    }

    public void OnRewindUnlocked()
    {
        canRewind = true;
    }

    public bool IsRewindAvailable()
    {
        return canRewind && !isRewinding && snapshots.Count > 0;
    }
}
