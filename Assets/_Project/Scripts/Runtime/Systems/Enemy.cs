using Cysharp.Threading.Tasks;

public class Enemy : Interactable
{
    protected override void OnInteract(PlayerMovement player)
    {

        player.TakeHit();

       // Destroy(gameObject, 0.5f);
    }
}
