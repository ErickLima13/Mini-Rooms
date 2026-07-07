using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    // Action genérica que passa o Player que colidiu como parâmetro
    // Útil caso o dano ou coletável precise acessar dados do player específico
    protected Action<PlayerMovement> OnPlayerInteract;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Tenta pegar o componente do Player de forma otimizada
        if (collision.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            ExecuteInteraction(player);
        }
    }

    private void ExecuteInteraction(PlayerMovement player)
    {
        // Dispara a Action para quem estiver ouvindo internamente
        OnPlayerInteract?.Invoke(player);

        // Chama o método abstrato que os filhos são OBRIGADOS a implementar
        OnInteract(player);
    }

    // Cada filho decide o que fazer aqui (dar dano, coletar, etc.)
    protected abstract void OnInteract(PlayerMovement player);
}
