using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public int coinValue = 1;

    void Update()
    {
        // 持续旋转动画
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

            // 回收
            gameObject.SetActive(false);
        }
    }
}
