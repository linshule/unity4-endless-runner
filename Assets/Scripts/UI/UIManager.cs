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
        Screen.lockCursor = false;
        Screen.showCursor = true;
    }

    public void OnStartGameClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.StartGame();
    }

    public void OnRestartClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.RestartGame();
    }

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int w = Screen.width;
        int h = Screen.height;

        // === 死亡闪红遮罩 ===
        if (gm.IsDeathFlashing())
        {
            Color flash = new Color(1f, 0f, 0f, 0.4f);
            GUI.color = flash;
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        switch (gm.state)
        {
            case GameState.Menu:
                DrawMainMenu(w, h, gm);
                break;
            case GameState.Playing:
                DrawHUD(w, h, gm);
                break;
            case GameState.Dead:
                DrawDeathScreen(w, h, gm);
                break;
            case GameState.GameOver:
                DrawGameOver(w, h, gm);
                break;
            case GameState.Paused:
                DrawPauseScreen(w, h, gm);
                break;
        }
    }

    void DrawMainMenu(int w, int h, GameManager gm)
    {
        // 半透明背景
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 20;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 24;
        btnStyle.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(0, h * 0.2f, w, 70), "无尽跑酷", titleStyle);
        GUI.Label(new Rect(0, h * 0.33f, w, 30), "Primitive Parkour — Unity 4.6.8", subStyle);

        if (gm.highScore > 0)
        {
            GUIStyle hsStyle = new GUIStyle(subStyle);
            hsStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(new Rect(0, h * 0.42f, w, 30), "最高分: " + gm.highScore, hsStyle);
        }

        if (GUI.Button(new Rect(w * 0.35f, h * 0.55f, w * 0.3f, 55), "开始游戏", btnStyle))
            OnStartGameClicked();

        // 操作提示
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 14;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        GUI.Label(new Rect(0, h * 0.78f, w, 25), "AD/←→ 变道 | 空格 跳跃 | S/↓ 滑铲 | E 冲刺 | R 分身", hintStyle);
    }

    void DrawHUD(int w, int h, GameManager gm)
    {
        // === 左上：分数 ===
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 28;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(30, 20, 300, 35), Mathf.FloorToInt(gm.score).ToString(), scoreStyle);

        // 分数倍率
        if (gm.scoreMultiplier > 1f)
        {
            GUIStyle multStyle = new GUIStyle(GUI.skin.label);
            multStyle.fontSize = 18;
            multStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(30, 55, 200, 25), "x" + gm.scoreMultiplier.ToString("F0"), multStyle);
        }

        // === 右上：金币 ===
        GUIStyle coinStyle = new GUIStyle(GUI.skin.label);
        coinStyle.fontSize = 22;
        coinStyle.alignment = TextAnchor.UpperRight;
        coinStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(w - 200, 20, 170, 30), "金币 " + gm.coinCount, coinStyle);

        // === 列车距离条（顶部居中） ===
        TrainController train = FindObjectOfType<TrainController>();
        if (train != null)
        {
            float dist = train.GetDistance();
            float ratio = Mathf.Clamp01(dist / 100f);

            // 背景条
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            GUI.DrawTexture(new Rect(w * 0.3f, 15, w * 0.4f, 8), Texture2D.whiteTexture);
            // 距离条
            Color barColor = Color.Lerp(Color.red, Color.green, ratio);
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(w * 0.3f, 15, w * 0.4f * ratio, 8), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle trainStyle = new GUIStyle(GUI.skin.label);
            trainStyle.fontSize = 12;
            trainStyle.alignment = TextAnchor.UpperCenter;
            trainStyle.normal.textColor = barColor;
            GUI.Label(new Rect(w * 0.3f, 25, w * 0.4f, 20), "列车 " + Mathf.FloorToInt(dist) + "m", trainStyle);
        }

        // === 底部：技能栏 ===
        SkillManager sm = SkillManager.Instance;
        if (sm != null)
        {
            int barY = h - 65;
            int btnW = 100;
            int gap = 20;
            int startX = w / 2 - (btnW * 2 + gap) / 2;

            GUIStyle skillStyle = new GUIStyle(GUI.skin.label);
            skillStyle.fontSize = 14;
            skillStyle.alignment = TextAnchor.MiddleCenter;
            skillStyle.normal.textColor = Color.white;

            // E 冲刺
            float dashCD = sm.GetCooldownRemaining("Dash");
            float dashTotal = sm.GetCooldownDuration("Dash");
            bool dashReady = dashCD <= 0f;
            Color dashColor = dashReady ? Color.green : new Color(0.3f, 0.5f, 0.3f);
            GUI.color = dashColor;
            GUI.DrawTexture(new Rect(startX, barY, btnW, 30), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(startX, barY, btnW, 30), dashReady ? "[E] 就绪" : "[E] " + Mathf.CeilToInt(dashCD) + "s", skillStyle);

            // R 分身
            float cloneCD = sm.GetCooldownRemaining("Clone");
            float cloneTotal = sm.GetCooldownDuration("Clone");
            bool cloneReady = cloneCD <= 0f;
            Color cloneColor = cloneReady ? Color.cyan : new Color(0.2f, 0.4f, 0.5f);
            GUI.color = cloneColor;
            GUI.DrawTexture(new Rect(startX + btnW + gap, barY, btnW, 30), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(startX + btnW + gap, barY, btnW, 30), cloneReady ? "[R] 就绪" : "[R] " + Mathf.CeilToInt(cloneCD) + "s", skillStyle);
        }

        // === 回溯状态 ===
        if (gm.rewindUnlocked && !gm.rewindUsed)
        {
            GUIStyle rewindStyle = new GUIStyle(GUI.skin.label);
            rewindStyle.fontSize = 14;
            rewindStyle.alignment = TextAnchor.UpperRight;
            rewindStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(w - 200, 45, 170, 25), "回溯: 可用", rewindStyle);
        }
    }

    void DrawDeathScreen(int w, int h, GameManager gm)
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 48;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        if (gm.rewindUnlocked && !gm.rewindUsed)
        {
            // 回溯提示
            GUI.Label(new Rect(0, h * 0.25f, w, 60), "致命碰撞！", titleStyle);

            GUIStyle rewindStyle = new GUIStyle(GUI.skin.label);
            rewindStyle.fontSize = 32;
            rewindStyle.alignment = TextAnchor.MiddleCenter;
            rewindStyle.normal.textColor = Color.yellow;

            float alpha = 0.7f + Mathf.Sin(Time.unscaledTime * 3f) * 0.3f;
            rewindStyle.normal.textColor = new Color(1f, 0.9f, 0.2f, alpha);
            GUI.Label(new Rect(0, h * 0.4f, w, 50), "按 空格 时间回溯！", rewindStyle);
        }
        else
        {
            GUI.Label(new Rect(0, h * 0.35f, w, 60), "游戏结束", titleStyle);
        }
    }

    void DrawGameOver(int w, int h, GameManager gm)
    {
        // 遮罩
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 42;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUIStyle valStyle = new GUIStyle(GUI.skin.label);
        valStyle.fontSize = 26;
        valStyle.alignment = TextAnchor.MiddleCenter;
        valStyle.normal.textColor = Color.white;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 22;
        btnStyle.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(0, h * 0.15f, w, 55), "结算", titleStyle);
        GUI.Label(new Rect(0, h * 0.28f, w, 40), "分数: " + Mathf.FloorToInt(gm.score), valStyle);

        if (Mathf.FloorToInt(gm.score) >= gm.highScore && gm.highScore > 0)
        {
            GUIStyle newStyle = new GUIStyle(valStyle);
            newStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, h * 0.35f, w, 40), "新纪录！最高分: " + gm.highScore, newStyle);
        }
        else
        {
            valStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(0, h * 0.35f, w, 40), "最高分: " + gm.highScore, valStyle);
        }

        GUI.Label(new Rect(0, h * 0.42f, w, 40), "金币: " + gm.coinCount, valStyle);

        float btnW = w * 0.28f;
        if (GUI.Button(new Rect(w * 0.22f, h * 0.58f, btnW, 50), "重新开始", btnStyle))
            OnRestartClicked();
    }

    void DrawPauseScreen(int w, int h, GameManager gm)
    {
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 42;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, h * 0.35f, w, 60), "已暂停", titleStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 22;
        if (GUI.Button(new Rect(w * 0.35f, h * 0.5f, w * 0.3f, 50), "继续游戏", btnStyle))
            gm.ResumeGame();
    }
}