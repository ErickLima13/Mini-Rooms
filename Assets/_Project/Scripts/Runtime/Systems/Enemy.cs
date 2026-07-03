using UnityEngine;

public class Enemy : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.TryGetComponent<SpeelController>(out SpeelController controller))
        {
            controller.AddPoint(1);

            Destroy(gameObject, 0.5f);
        }
    }
}
