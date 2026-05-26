using UnityEngine;

public class CollapseTrap : MonoBehaviour
{
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