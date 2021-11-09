using UnityEngine;

public interface Interactable
{
    string GetInteractionText();
    void OnInteractClient(Player player);
    void OnInteractServer(Player player);
}
