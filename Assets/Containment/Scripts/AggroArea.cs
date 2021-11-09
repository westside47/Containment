// Catches the Aggro Sphere's OnTrigger functions and forwards them to the
// owner. Make sure that the aggro area's layer is IgnoreRaycast, so that
// clicking on the area won't select the entity.
//
// Note that a player's collider might be on the pelvis for animation reasons,
// so we need to use GetComponentInParent to find the owner script.
//
// IMPORTANT: Monster.OnTriggerEnter would catch it too. But this way we don't
//            need to add OnTriggerEnter code to all the entity types that need
//            an aggro area. We can just reuse it.
//            (adding it to Entity.OnTriggerEnter would be strange too, because
//             not all entity types should react to OnTriggerEnter with aggro!)
using UnityEngine;

[RequireComponent(typeof(Collider))] // aggro area trigger
public class AggroArea : MonoBehaviour
{
    public Monster owner; // set in the inspector

    // same as OnTriggerStay
    void OnTriggerEnter(Collider co)
    {
        // is this a living thing that we could attack?
        // (look in parents because AggroArea doesn't collide with player's main
        //  layer (IgnoreRaycast), only with body part layers. this way
        //  AggroArea only interacts with player layers, not with other
        //  monster's IgnoreRaycast layers etc.)
        Entity entity = co.GetComponentInParent<Entity>();
        if (entity) owner.OnAggro(entity);
    }

    void OnTriggerStay(Collider co)
    {
        // is this a living thing that we could attack?
        // (look in parents because AggroArea doesn't collide with player's main
        //  layer (IgnoreRaycast), only with body part layers. this way
        //  AggroArea only interacts with player layers, not with other
        //  monster's IgnoreRaycast layers etc.)
        Entity entity = co.GetComponentInParent<Entity>();
        if (entity) owner.OnAggro(entity);
    }
}
