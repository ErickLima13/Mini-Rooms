using UnityEngine;

public class SpeelController : MonoBehaviour
{
    public GameObject target;

    public float speed = 1f;
    public float radius = 2f;
    public float angle = 0f;

    public PlayerMovement player;

    void Update()
    {
        float x = target.transform.position.x + Mathf.Cos(angle) * radius;
        float y = target.transform.position.y + Mathf.Sin(angle) * radius;

        transform.position = new(x, y);

        angle += speed * Time.deltaTime;
    }



    public void AddPoint(int value)
    {
        player.Point += value;

        print("chamei");
    }

}
