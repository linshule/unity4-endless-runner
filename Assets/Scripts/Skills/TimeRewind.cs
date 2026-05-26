using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeRewind : MonoBehaviour
{
    public PlayerController player;
    public GameManager gameManager;

    // === 快照 ===
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
    public float rewindDuration = 0.8f;

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

        // 记录快照
        if (!player.isDead && !isRewinding && gameManager != null && gameManager.state == GameState.Playing)
        {
            recordTimer += Time.deltaTime;
            if (recordTimer >= recordInterval)
            {
                recordTimer = 0f;
                Snapshot snap = new Snapshot();
                snap.position = player.transform.position;
                snap.time = Time.time;
                snapshots.Add(snap);

                while (snapshots.Count > 0 && Time.time - snapshots[0].time > recordDuration)
                    snapshots.RemoveAt(0);
            }
        }

        // 死亡时检查回溯
        if (player.isDead && canRewind && !isRewinding)
        {
            // 空格触发回溯
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerRewind();
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

        // 回溯期间无敌，防止原地复活被障碍物秒杀
        player.isInvincible = true;

        gameManager.TriggerRewind();

        // 找 2 秒前快照
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

        // 黑白倒放
        Vector3 startPos = player.transform.position;
        Vector3 endPos = targetSnap.position;
        float elapsed = 0f;

        while (elapsed < rewindDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / rewindDuration;
            player.SetPosition(Vector3.Lerp(startPos, endPos, t));
            yield return null;
        }

        player.SetPosition(endPos);
        isRewinding = false;

        // 延迟取消无敌，确保回溯后不会立即被碰撞检测杀死
        yield return new WaitForSeconds(0.3f);
        player.isInvincible = false;
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