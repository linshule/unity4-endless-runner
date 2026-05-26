using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === 轨道配置 ===
    public float[] laneX = new float[] { -6f, 0f, 6f };
    public int currentLane = 1;
    public float laneSwitchSpeed = 12f;

    // === 移动参数 ===
    public float baseSpeed = 10f;
    public float currentSpeed;
    public float speedIncreaseRate = 0.05f;
    public float maxSpeed = 25f;

    // === 跳跃参数 ===
    public float jumpHeight = 25f;
    public float gravity = 60f;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;

    // === 滑铲参数 ===
    public float slideDuration = 0.6f;
    public float slideHeight = 1f;
    public float slideCooldown = 0.3f;
    private float originalHeight;
    private float originalCenterY;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;

    // === 死亡状态 ===
    public bool isDead = false;
    private float deathAnimTimer = 0f;

    // === 组件引用 ===
    private CharacterController controller;
    private GameObject bodyObj;
    private GameObject headObj;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = new Vector3(0f, 1f, 0f);

        originalHeight = controller.height;
        originalCenterY = controller.center.y;

        currentSpeed = baseSpeed;

        // 初始位置
        Vector3 startPos = transform.position;
        startPos.x = laneX[currentLane];
        if (startPos.y < 1.5f) startPos.y = 1.5f;
        transform.position = startPos;

        // 缓存子对象引用
        Transform t = transform.Find("PlayerBody");
        if (t != null) bodyObj = t.gameObject;
        t = transform.Find("PlayerHead");
        if (t != null) headObj = t.gameObject;
    }

    void Update()
    {
        if (isDead)
        {
            // 死亡动画
            deathAnimTimer += Time.deltaTime;
            if (deathAnimTimer < 0.5f && bodyObj != null)
            {
                float t = deathAnimTimer / 0.5f;
                bodyObj.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.5f, 0.3f, 1.5f), t);
                if (headObj != null)
                    headObj.transform.localPosition = Vector3.Lerp(new Vector3(0f, 1.8f, 0f), new Vector3(0f, 0.5f, 2f), t);
            }
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.state != GameState.Playing) return;

        // 速度递增
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += speedIncreaseRate * Time.deltaTime;
        }

        // 输入处理
        HandleLaneSwitch();
        HandleJump();
        HandleSlide();

        // 移动
        Vector3 move = Vector3.zero;
        move.z = currentSpeed * Time.deltaTime;

        float targetX = laneX[currentLane];
        float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        move.x = newX - transform.position.x;

        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = -1f;
        }
        move.y = verticalVelocity * Time.deltaTime;

        controller.Move(move);
        CheckGrounded();

        // 掉落即死检测
        if (transform.position.y < -5f)
        {
            GameManager gm2 = GameManager.Instance;
            if (gm2 != null) gm2.OnPlayerHitObstacle();
        }
    }

    void HandleLaneSwitch()
    {
        // 滑铲中也可以变道（移除限制）
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentLane > 0) currentLane--;
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentLane < 2) currentLane++;
        }
    }

    void HandleJump()
    {
        if (isSliding) return;

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            verticalVelocity = jumpHeight;
            isGrounded = false;
        }
    }

    void HandleSlide()
    {
        if (!isGrounded) return;

        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
            return;
        }

        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftControl)) && !isSliding)
        {
            StartSlide();
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                EndSlide();
            }
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        controller.height = slideHeight;
        float newCenterY = originalCenterY - (originalHeight - slideHeight) * 0.5f;
        controller.center = new Vector3(0f, newCenterY, 0f);
    }

    void EndSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;
        controller.height = originalHeight;
        controller.center = new Vector3(0f, originalCenterY, 0f);
    }

    void CheckGrounded()
    {
        if (!isSliding)
        {
            isGrounded = (transform.position.y <= 1.2f && verticalVelocity <= 0f);
        }
        else
        {
            float slideGroundY = 1.2f - (originalHeight - slideHeight) * 0.5f;
            isGrounded = (transform.position.y <= slideGroundY && verticalVelocity <= 0f);
        }
    }

    public void Die()
    {
        isDead = true;
        currentSpeed = 0f;
        deathAnimTimer = 0f;
    }

    public void Revive()
    {
        isDead = false;
        deathAnimTimer = 0f;
        // 恢复视觉
        if (bodyObj != null)
            bodyObj.transform.localScale = Vector3.one;
        if (headObj != null)
            headObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public float GetSpeed()
    {
        return currentSpeed;
    }

    // === 碰撞检测：撞到障碍物即死 ===
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead) return;

        // 检查是否是障碍物
        ObstacleTag tag = hit.gameObject.GetComponent<ObstacleTag>();
        if (tag == null && hit.transform.parent != null)
            tag = hit.transform.parent.GetComponent<ObstacleTag>();

        if (tag != null)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.state == GameState.Playing)
            {
                gm.OnPlayerHitObstacle();
            }
        }
    }
}