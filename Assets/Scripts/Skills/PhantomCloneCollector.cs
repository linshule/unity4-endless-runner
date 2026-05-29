using UnityEngine;

public class PhantomCloneCollector : MonoBehaviour
{
    private PlayerController player;
    private TrainController train;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        train = FindObjectOfType<TrainController>();
    }

    void OnTriggerEnter(Collider other)
    {
        CoinPickup coin = other.GetComponent<CoinPickup>();
        if (coin != null)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
                gm.coinCount += coin.coinValue;

            if (HUDController.Instance != null)
                HUDController.Instance.OnCoinCollected();

            if (player != null)
                player.IncreaseSpeed(0.5f);

            if (train != null)
            {
                train.AddDistance(10f);
                train.OnCoinCollected();
            }

            other.gameObject.SetActive(false);
        }
    }
}
