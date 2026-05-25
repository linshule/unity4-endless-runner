using UnityEngine;

public class DynamicObstacle : MonoBehaviour
{
    // O05 旋转机关：长条 Cube 绕 Y 轴旋转
    public float rotationSpeed = 90f;
    public float baseRotationSpeed = 90f;
    
    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (player == null || player.isDead) return;

        float speed = baseRotationSpeed + (player.GetSpeed() * 2f);
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}
