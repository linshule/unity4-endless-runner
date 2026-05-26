using UnityEngine;

public class CollapseTrap : MonoBehaviour
{
    void Start()
    {
        // OnTriggerEnter 需要 Rigidbody
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.state == GameState.Playing)
            {
                gm.OnPlayerHitObstacle();
            }
        }
    }
}