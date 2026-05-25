using UnityEngine;
using System.Collections;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Dead,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // === 游戏状态 ===
    public GameState state = GameState.Menu;
    public PlayerController player;

    // === 分数系统 ===
    public float score = 0f;
    public float scoreMultiplier = 1f;
    public int coinCount = 0;
    public int highScore = 0;
    public int totalCoins = 0;

    // === 速度控制 ===
    public AnimationCurve speedCurve;
    public float gameTime = 0f;

    // === 回溯解锁 ===
    public bool rewindUnlocked = false;
    public int rewindUnlockScore = 1500;
    public bool rewindUsed = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadHighScore();
        if (speedCurve == null || speedCurve.keys.Length == 0)
        {
            speedCurve = AnimationCurve.Linear(0f, 1f, 300f, 2.5f);
        }
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (state == GameState.Playing)
        {
            gameTime += Time.deltaTime;

            // 距离分数
            if (player != null && !player.isDead)
            {
                score += player.GetSpeed() * scoreMultiplier * Time.deltaTime;

                // 检查回溯解锁
                if (!rewindUnlocked && score >= rewindUnlockScore)
                {
                    rewindUnlocked = true;
                    if (HUDController.Instance != null)
                    {
                        HUDController.Instance.OnRewindUnlocked();
                    }
                }
            }

            // 暂停
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame();
            }
        }
        else if (state == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
            }
        }
    }

    void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Player");
            }
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
            }
        }
    }

    public void StartGame()
    {
        FindPlayer();
        state = GameState.Playing;
        score = 0f;
        coinCount = 0;
        gameTime = 0f;
        scoreMultiplier = 1f;
        rewindUnlocked = false;
        rewindUsed = false;

        if (player != null)
        {
            player.isDead = false;
        }

        Time.timeScale = 1f;
        Screen.lockCursor = true;
        Screen.showCursor = false;
    }

    public void PauseGame()
    {
        if (state == GameState.Playing)
        {
            state = GameState.Paused;
            Time.timeScale = 0f;
            Screen.lockCursor = false;
            Screen.showCursor = true;
        }
    }

    public void ResumeGame()
    {
        if (state == GameState.Paused)
        {
            state = GameState.Playing;
            Time.timeScale = 1f;
            Screen.lockCursor = true;
            Screen.showCursor = false;
        }
    }

    public void OnPlayerHitObstacle()
    {
        if (state != GameState.Playing) return;
        if (player.isDead) return;

        // 碰撞即死
        player.Die();
        state = GameState.Dead;

        // 通知 HUD
        if (HUDController.Instance != null)
        {
            HUDController.Instance.OnPlayerDead();
        }

        // 检查是否可以回溯
        if (rewindUnlocked && !rewindUsed)
        {
            StartCoroutine(WaitForRewindChoice());
        }
        else
        {
            StartCoroutine(EndGameDelay(1.5f));
        }
    }

    IEnumerator WaitForRewindChoice()
    {
        // 显示回溯按钮，等待 3 秒
        float waitTime = 3f;
        while (waitTime > 0f)
        {
            waitTime -= Time.unscaledDeltaTime;
            yield return null;
        }
        // 超时自动进入结算
        StartCoroutine(EndGameDelay(0.5f));
    }

    public void TriggerRewind()
    {
        rewindUsed = true;
        state = GameState.Playing;
        if (player != null) player.isDead = false;
    }

    public void OnTrainCaught()
    {
        if (state != GameState.Playing) return;
        player.Die();
        state = GameState.Dead;
        // 列车追上不可回溯
        StartCoroutine(EndGameDelay(1.5f));
    }

    IEnumerator EndGameDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndGame();
    }

    void EndGame()
    {
        state = GameState.GameOver;

        // 保存最高分
        int intScore = Mathf.FloorToInt(score);
        if (intScore > highScore)
        {
            highScore = intScore;
            SaveHighScore();
        }

        totalCoins += coinCount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();

        Screen.lockCursor = false;
        Screen.showCursor = true;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.OnGameOver();
        }
    }

    public void RestartGame()
    {
        // 重置状态到 Menu，让新场景显示主菜单
        state = GameState.Menu;
        Application.LoadLevel(Application.loadedLevel);
    }

    public void LoadMainMenu()
    {
        Application.LoadLevel("MainMenu");
    }

    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
    }

    void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }
}
