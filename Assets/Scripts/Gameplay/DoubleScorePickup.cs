using UnityEngine;

public class DoubleScorePickup : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0f, 120f * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PowerUpManager pm = FindObjectOfType<PowerUpManager>();
            if (pm != null)
            {
                pm.ActivateDoubleScore();
            }
            GameObject.Destroy(gameObject);
        }
    }
}
