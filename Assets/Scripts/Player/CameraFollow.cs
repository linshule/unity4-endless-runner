using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 7f, -15f);
    public float smoothSpeed = 5f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) target = player.transform;
            if (target == null) return;
        }

        Vector3 desiredPos = target.position + offset;

        // X 和 Z 刚性跟随（避免追逐延迟导致抖动）
        // Y 轴平滑跟随（跳跃时缓冲）
        transform.position = new Vector3(
            desiredPos.x,
            Mathf.Lerp(transform.position.y, desiredPos.y, smoothSpeed * Time.deltaTime),
            desiredPos.z
        );

        transform.LookAt(target.position + Vector3.up * 0.4f);
    }
}