using UnityEngine;

public class PhantomCloneCollector : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 分身自动拾取金币
        CoinPickup coin = other.GetComponent<CoinPickup>();
        if (coin != null)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.coinCount += coin.coinValue;
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.OnCoinCollected();
            }

            other.gameObject.SetActive(false);
        }
    }
}
