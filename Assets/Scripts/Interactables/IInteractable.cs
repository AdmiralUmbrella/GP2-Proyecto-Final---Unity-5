using UnityEngine;

public interface IInteractable
{
    string GetInteractionPrompt();  // Texto que se mostrar� al jugador
    void OnInteract(PlayerStateManager player);  // M�todo que se ejecuta al interactuar
    bool CanInteract(PlayerStateManager player); // Verificar si se puede interactuar
}
