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
    public static bool restartRequested = false;

    public GameState state = GameState.Menu;
    public PlayerController player;

    // === 分数 ===
    public float score = 0f;
    public float scoreMultiplier = 1f;
    public int coinCount = 0;
    public int highScore = 0;
    public int totalCoins = 0;

    // === 速度曲线 ===
    public AnimationCurve speedCurve;
    public float gameTime = 0f;

    // === 回溯 ===
    public bool rewindUnlocked = false;
    public int rewindUnlockScore = 1500;
    public bool rewindUsed = false;

    // === 死亡闪红 ===
    private float deathFlashTimer = 0f;
    public float deathFlashDuration = 0.3f;

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

    void OnLevelWasLoaded(int level)
    {
        if (restartRequested)
        {
            restartRequested = false;
            StartGame();
        }
    }

    void Update()
    {
        if (state == GameState.Playing)
        {
            gameTime += Time.deltaTime;

            if (player != null && !player.isDead)
            {
                score += player.GetSpeed() * scoreMultiplier * Time.deltaTime;

                if (!rewindUnlocked && score >= rewindUnlockScore)
                {
                    rewindUnlocked = true;
                    if (HUDController.Instance != null)
                        HUDController.Instance.OnRewindUnlocked();
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                PauseGame();
        }
        else if (state == GameState.Dead)
        {
            // 闪红效果计时
            if (deathFlashTimer > 0f)
                deathFlashTimer -= Time.deltaTime;
        }
        else if (state == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ResumeGame();
        }
    }

    void FindPlayer()
    {
        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj == null) obj = GameObject.Find("Player");
            if (obj != null) player = obj.GetComponent<PlayerController>();
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
        deathFlashTimer = 0f;

        if (player != null)
        {
            player.Revive();
            player.isDashing = false;
        }

        // 还原时间缩放
        Time.timeScale = 1f;

        // 恢复列车距离
        TrainController train = FindObjectOfType<TrainController>();
        if (train != null) train.RestoreDistance();

        // 重置自适应难度
        AdaptiveDifficulty ad = AdaptiveDifficulty.Instance;
        if (ad != null) ad.ResetForNewGame();

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

        player.Die();
        state = GameState.Dead;
        deathFlashTimer = deathFlashDuration;

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
        float waitTime = 3f;
        while (waitTime > 0f && state == GameState.Dead)
        {
            waitTime -= Time.deltaTime;
            yield return null;
        }
        if (state == GameState.Dead)
            StartCoroutine(EndGameDelay(0.5f));
    }

    public void TriggerRewind()
    {
        rewindUsed = true;
        state = GameState.Playing;
        if (player != null) player.Revive();

        // 恢复列车距离
        TrainController train = FindObjectOfType<TrainController>();
        if (train != null)
            train.RestoreDistance();
    }

    public void OnTrainCaught()
    {
        if (state != GameState.Playing) return;
        player.Die();
        state = GameState.Dead;
        rewindUsed = true;
        deathFlashTimer = deathFlashDuration;
        StartCoroutine(EndGameDelay(1.5f));
    }

    IEnumerator EndGameDelay(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay && state == GameState.Dead)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (state == GameState.Dead)
            EndGame();
    }

    void EndGame()
    {
        state = GameState.GameOver;

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

    }

    public void RestartGame()
    {
        restartRequested = true;
        Application.LoadLevel(Application.loadedLevel);
    }

    public void LoadMainMenu()
    {
        restartRequested = false;
        state = GameState.Menu;
        Application.LoadLevel(Application.loadedLevel);
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

    // 供 UI 画死亡闪红
    public bool IsDeathFlashing()
    {
        return deathFlashTimer > 0f;
    }
}