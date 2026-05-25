using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === 轨道配置 ===
    public float[] laneX = new float[] { -2f, 0f, 2f };
    public int currentLane = 1; // 初始在中间轨道
    public float laneSwitchSpeed = 8f;

    // === 移动参数 ===
    public float baseSpeed = 10f;
    public float currentSpeed;
    public float speedIncreaseRate = 0.05f;
    public float maxSpeed = 25f;

    // === 跳跃参数 ===
    public float jumpHeight = 4f;
    public float gravity = 20f;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;

    // === 滑铲参数 ===
    public float slideDuration = 0.6f;
    public float slideHeight = 1f;
    private float originalHeight;
    private float originalCenterY;
    private bool isSliding = false;
    private float slideTimer = 0f;

    // === 死亡状态 ===
    public bool isDead = false;

    // === 组件引用 ===
    private CharacterController controller;

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
    }

    void Update()
    {
        if (isDead) return;

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

        // 前向移动
        move.z = currentSpeed * Time.deltaTime;

        // 横向移动（变道）
        float targetX = laneX[currentLane];
        float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        move.x = newX - transform.position.x;

        // 纵向移动（跳跃/重力）
        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = -1f; // 保持贴地
        }
        move.y = verticalVelocity * Time.deltaTime;

        // 执行移动
        controller.Move(move);

        // 接地检测（CharacterController 的 isGrounded 在 4.x 不可靠，用射线辅助）
        CheckGrounded();
    }

    void HandleLaneSwitch()
    {
        if (isSliding) return; // 滑铲中不可变道

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
        controller.height = originalHeight;
        controller.center = new Vector3(0f, originalCenterY, 0f);
    }

    void CheckGrounded()
    {
        // CharacterController.isGrounded 在 4.x 不可靠
        // 用简单的 Y 坐标判断
        if (!isSliding)
        {
            isGrounded = (transform.position.y <= 1.2f && verticalVelocity <= 0f);
        }
        else
        {
            float slideGroundY = 1.55f - (originalHeight - slideHeight) * 0.5f;
            isGrounded = (transform.position.y <= slideGroundY && verticalVelocity <= 0f);
        }

        if (isGrounded && transform.position.y > 0.5f)
        {
            // 修正位置到地面
            Vector3 pos = transform.position;
            pos.y = 1.5f;
            transform.position = pos;
        }
    }

    public void Die()
    {
        isDead = true;
        currentSpeed = 0f;
    }

    // 供 TimeRewind 调用
    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    // 供外部获取速度
    public float GetSpeed()
    {
        return currentSpeed;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 碰撞检测在 GameManager 中统一处理
    }
}
