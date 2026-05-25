using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 主菜单默认显示
        Screen.lockCursor = false;
        Screen.showCursor = true;
    }

    public void OnStartGameClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.StartGame();
        }
    }

    public void OnRestartClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.RestartGame();
        }
    }

    public void OnMainMenuClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.LoadMainMenu();
        }
    }
    
    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int w = Screen.width;
        int h = Screen.height;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 24;
        labelStyle.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;

        if (gm.state == GameState.Menu)
        {
            // 主菜单
            labelStyle.fontSize = 48;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.25f, w, 80), "无尽跑酷", labelStyle);

            labelStyle.fontSize = 24;
            GUI.Label(new Rect(0, h * 0.4f, w, 40), "Unity 4.6.8 Parkour", labelStyle);

            if (GUI.Button(new Rect(w * 0.35f, h * 0.55f, w * 0.3f, 50), "开始游戏", buttonStyle))
            {
                OnStartGameClicked();
            }
        }
        else if (gm.state == GameState.Playing)
        {
            // HUD
            labelStyle.fontSize = 20;
            GUI.Label(new Rect(20, 20, 300, 30), "得分: " + Mathf.FloorToInt(gm.score), labelStyle);
            GUI.Label(new Rect(20, 45, 300, 30), "金币: " + gm.coinCount, labelStyle);

            // 列车距离
            TrainController train = FindObjectOfType<TrainController>();
            if (train != null)
            {
                float dist = train.GetDistance();
                Color barColor = Color.Lerp(Color.green, Color.red, train.GetWarningIntensity());
                labelStyle.normal.textColor = barColor;
                GUI.Label(new Rect(20, 70, 300, 30), "列车: " + Mathf.FloorToInt(dist) + "m", labelStyle);
                labelStyle.normal.textColor = Color.white;
            }

            // 技能冷却
            SkillManager sm = SkillManager.Instance;
            if (sm != null)
            {
                float dashCD = sm.GetCooldownRemaining("Dash");
                float cloneCD = sm.GetCooldownRemaining("Clone");
                GUI.Label(new Rect(w * 0.4f, h - 60, 200, 30), 
                    string.Format("E 冲刺 {0}s", dashCD > 0f ? Mathf.CeilToInt(dashCD).ToString() : "OK"), 
                    labelStyle);
                GUI.Label(new Rect(w * 0.6f, h - 60, 200, 30), 
                    string.Format("R 分身 {0}s", cloneCD > 0f ? Mathf.CeilToInt(cloneCD).ToString() : "OK"), 
                    labelStyle);
            }

            // 双倍分数
            PowerUpManager pm = FindObjectOfType<PowerUpManager>();
            if (pm != null && pm.IsDoubleScoreActive())
            {
                float remaining = pm.GetDoubleScoreRemaining();
                GUI.Label(new Rect(w * 0.3f, 10, w * 0.4f, 30), 
                    string.Format("x2 分数 {0:F1}s", remaining * pm.GetDoubleScoreRemaining() * 10f).Replace("GetDoubleScoreRemaining", ""),
                    labelStyle);
                // 简化显示
                GUI.Label(new Rect(w * 0.35f, 10, w * 0.3f, 30), 
                    "x2 分数 " + (pm.GetDoubleScoreRemaining() * 10f).ToString("F1") + "s", 
                    labelStyle);
            }

            // 回溯解锁状态
            if (gm.rewindUnlocked && !gm.rewindUsed)
            {
                labelStyle.normal.textColor = Color.cyan;
                GUI.Label(new Rect(20, 95, 200, 30), "回溯: 就绪", labelStyle);
                labelStyle.normal.textColor = Color.white;
            }
        }
        else if (gm.state == GameState.Dead)
        {
            // 死亡回溯提示
            TimeRewind rewind = FindObjectOfType<TimeRewind>();
            if (rewind != null && rewind.IsRewindAvailable() && !gm.rewindUsed)
            {
                labelStyle.fontSize = 36;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(0, h * 0.3f, w, 60), "按空格键 时间回溯！", labelStyle);
                labelStyle.normal.textColor = Color.white;
            }
            else
            {
                labelStyle.fontSize = 36;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(0, h * 0.3f, w, 60), "游戏结束", labelStyle);
            }
        }
        else if (gm.state == GameState.GameOver)
        {
            // 结算界面
            labelStyle.fontSize = 36;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.2f, w, 50), "结算", labelStyle);

            labelStyle.fontSize = 28;
            GUI.Label(new Rect(0, h * 0.3f, w, 40), "分数: " + Mathf.FloorToInt(gm.score), labelStyle);
            GUI.Label(new Rect(0, h * 0.37f, w, 40), "最高分: " + gm.highScore, labelStyle);
            GUI.Label(new Rect(0, h * 0.44f, w, 40), "金币: " + gm.coinCount, labelStyle);

            if (GUI.Button(new Rect(w * 0.35f, h * 0.55f, w * 0.3f, 50), "重新开始", buttonStyle))
            {
                OnRestartClicked();
            }
        }
        else if (gm.state == GameState.Paused)
        {
            labelStyle.fontSize = 36;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.3f, w, 50), "已暂停", labelStyle);

            if (GUI.Button(new Rect(w * 0.35f, h * 0.45f, w * 0.3f, 50), "继续游戏", buttonStyle))
            {
                gm.ResumeGame();
            }
        }
    }
}
