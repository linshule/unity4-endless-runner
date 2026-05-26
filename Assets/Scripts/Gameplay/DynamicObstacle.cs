using UnityEngine;

public class DynamicObstacle : MonoBehaviour
{
    // O05 旋转机关：只旋转横杆子物体
    public float rotationSpeed = 120f;
    private Transform barTransform;
    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        // 查找名含 "Cube" 或第一个子物体作为横杆
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("cube") || child.name.ToLower().Contains("bar") || child.name.ToLower().Contains("spinner"))
            {
                barTransform = child;
                break;
            }
        }
        if (barTransform == null && transform.childCount > 0)
            barTransform = transform.GetChild(0);
    }

    void Update()
    {
        if (player == null || player.isDead) return;

        if (barTransform != null)
        {
            float speed = rotationSpeed + (player.GetSpeed() * 3f);
            barTransform.Rotate(0f, speed * Time.deltaTime, 0f);
        }
    }
}