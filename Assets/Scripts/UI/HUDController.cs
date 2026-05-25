using UnityEngine;
using System.Collections.Generic;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;
    public GameManager gameManager;

    // === HUD 状态 ===
    private bool showRewindButton = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    void Update()
    {
        // 死亡回溯按钮（空格键触发）
        if (showRewindButton && Input.GetKeyDown(KeyCode.Space))
        {
            TimeRewind rewind = FindObjectOfType<TimeRewind>();
            if (rewind != null)
            {
                rewind.TriggerRewind();
            }
        }
    }

    // === 回调接口 ===
    public void OnPlayerDead()
    {
        // GameManager 处理
    }

    public void OnGameOver()
    {
        // 结算显示
        Screen.lockCursor = false;
        Screen.showCursor = true;
    }

    public void OnCoinCollected()
    {
        // 金币增加反馈
    }

    public void OnRewindUnlocked()
    {
        // 回溯解锁通知
    }

    public void OnDoubleScoreActivated(float duration)
    {
        // 双倍分数激活
    }

    public void OnDoubleScoreDeactivated()
    {
        // 双倍分数结束
    }

    public void ShowRewindButton(bool show)
    {
        showRewindButton = show;
    }

    // === HUD 数据查询 ===
    public float GetScore()
    {
        if (gameManager != null) return gameManager.score;
        return 0f;
    }

    public int GetCoins()
    {
        if (gameManager != null) return gameManager.coinCount;
        return 0;
    }

    public int GetHighScore()
    {
        if (gameManager != null) return gameManager.highScore;
        return 0;
    }

    public float GetTrainDistance()
    {
        TrainController train = FindObjectOfType<TrainController>();
        return train != null ? train.GetDistance() : 100f;
    }

    public float GetSkillCooldown(string skillName)
    {
        SkillManager sm = SkillManager.Instance;
        if (sm != null)
        {
            float remaining = sm.GetCooldownRemaining(skillName);
            float total = sm.GetCooldownDuration(skillName);
            if (total > 0f) return remaining / total;
        }
        return 0f;
    }

    public float GetDoubleScoreRemaining()
    {
        PowerUpManager pm = FindObjectOfType<PowerUpManager>();
        return pm != null ? pm.GetDoubleScoreRemaining() : 0f;
    }

    public bool IsRewindUnlocked()
    {
        return gameManager != null && gameManager.rewindUnlocked && !gameManager.rewindUsed;
    }
}
