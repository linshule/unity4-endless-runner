using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public int coinValue = 1;

    void Update()
    {
        transform.Rotate(0f, 180f * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.coinCount += coinValue;
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.OnCoinCollected();
            }

            // 金币增加人物速度 + 推开列车
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.IncreaseSpeed(0.5f);
            }

            TrainController train = FindObjectOfType<TrainController>();
            if (train != null)
            {
                train.AddDistance(3f);
                train.OnCoinCollected();
            }

            gameObject.SetActive(false);
        }
    }
}
