using UnityEngine;
using Mirror;

public class Door : NetworkBehaviourNonAlloc, Interactable
{
    // components to be assigned in inspector
    public Animator animator;

    [SyncVar] public bool open;

    // animate on client AND on server, otherwise the collider stays always
    // closed on the server
    void Update()
    {
        animator.SetBool("Open", open);
    }

    // interactable ////////////////////////////////////////////////////////////
    public string GetInteractionText()
    {
        return (open ? "Close" : "Open") + " door";
    }

    [Client]
    public void OnInteractClient(Player player) {}

    [Server]
    public void OnInteractServer(Player player)
    {
        open = !open;
    }

    // validation //////////////////////////////////////////////////////////////
    void OnValidate()
    {
        // door colliders move with the animation.
        // door meshes are hidding in host and server-only mode.
        // => door colliders only move if we animate hidden meshes too!
        if (animator != null &&
            animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
        {
            Debug.LogWarning(name + " animator cull mode needs to be set to Always, otherwise the door collider won't move in host or server-only mode.");
        }
    }
}
