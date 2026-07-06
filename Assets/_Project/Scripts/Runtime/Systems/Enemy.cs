using UnityEngine;
using UnityEngine.InputSystem.XR;

public class Enemy : Interactable
{
    protected override void OnInteract(PlayerMovement player)
    {
       

        Destroy(gameObject, 0.5f);
    }
}
