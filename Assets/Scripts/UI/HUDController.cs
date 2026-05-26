using UnityEngine;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;
    public GameManager gameManager;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    public void OnPlayerDead() { }
    public void OnGameOver() { }
    public void OnCoinCollected() { }
    public void OnRewindUnlocked() { TimeRewind tr = FindObjectOfType<TimeRewind>(); if (tr != null) tr.OnRewindUnlocked(); }
    public void OnDoubleScoreActivated(float duration) { }
    public void OnDoubleScoreDeactivated() { }

    public float GetScore() { return gameManager != null ? gameManager.score : 0f; }
    public int GetCoins() { return gameManager != null ? gameManager.coinCount : 0; }
    public int GetHighScore() { return gameManager != null ? gameManager.highScore : 0; }

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