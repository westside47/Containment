// The Entity class is rather simple. It contains a few basic entity properties
// like health, mana and level that all inheriting classes like Players and
// Monsters can use.
//
// Note: in a component based architecture we don't necessarily need Entity.cs,
//       but it does help us to avoid lots of GetComponent calls. Passing an
//       Entity to combat and accessing entity.health is faster than passing a
//       GameObject and calling gameObject.GetComponent<Health>() each time!
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable] public class UnityEventEntityInt : UnityEvent<Entity, int> {}

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Combat))]
public abstract class Entity : NetworkBehaviourNonAlloc
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    [Header("Components")]
    public Health health;
    public Combat combat;
#pragma warning disable CS0109 // member does not hide accessible member
    new public Collider collider; // this is the main collider
#pragma warning restore CS0109 // member does not hide accessible member
}
