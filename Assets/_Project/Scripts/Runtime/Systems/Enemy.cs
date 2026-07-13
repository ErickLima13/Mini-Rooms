using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class Enemy : Interactable
{



    protected override void OnInteract(PlayerMovement player)
    {

        //player.TakeHit();

        Consequence?.Invoke();

       // Destroy(gameObject, 0.5f);


    }
}
