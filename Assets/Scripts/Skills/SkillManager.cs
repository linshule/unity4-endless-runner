using UnityEngine;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    public PlayerController player;

    // === 技能冷却状态 ===
    private Dictionary<string, float> cooldowns = new Dictionary<string, float>();
    private Dictionary<string, float> cooldownDurations = new Dictionary<string, float>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        // 注册技能冷却
        RegisterSkill("Dash", 45f);
        RegisterSkill("Clone", 25f);
    }

    void Update()
    {
        if (player == null || player.isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.state != GameState.Playing) return;

        // 更新冷却
        List<string> keys = new List<string>(cooldowns.Keys);
        foreach (string key in keys)
        {
            if (cooldowns[key] > 0f)
            {
                cooldowns[key] -= Time.deltaTime;
                if (cooldowns[key] < 0f) cooldowns[key] = 0f;
            }
        }

        // 技能输入
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryUseSkill("Dash");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryUseSkill("Clone");
        }
    }

    public void RegisterSkill(string skillName, float cd)
    {
        cooldowns[skillName] = 0f;
        cooldownDurations[skillName] = cd;
    }

    public bool TryUseSkill(string skillName)
    {
        if (!cooldowns.ContainsKey(skillName)) return false;
        if (cooldowns[skillName] > 0f) return false;

        cooldowns[skillName] = cooldownDurations[skillName];
        return true;
    }

    public float GetCooldownRemaining(string skillName)
    {
        if (!cooldowns.ContainsKey(skillName)) return 0f;
        return cooldowns[skillName];
    }

    public float GetCooldownDuration(string skillName)
    {
        if (!cooldownDurations.ContainsKey(skillName)) return 1f;
        return cooldownDurations[skillName];
    }

    public bool IsSkillReady(string skillName)
    {
        if (!cooldowns.ContainsKey(skillName)) return false;
        return cooldowns[skillName] <= 0f;
    }
}
