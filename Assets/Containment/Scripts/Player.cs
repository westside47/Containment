// keep track of some player data like class, account etc.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Controller2k;
using Mirror;

public class SyncDictionaryIntDouble : SyncDictionary<int, double> {}

[Serializable] public class UnityEventPlayer : UnityEvent<Player> {}

public class Player : Entity
{
    // fields for all player components to avoid costly GetComponent calls
    [Header("Components")]
    public Animator animator;
    public AudioSource audioSource;
    public Endurance endurance;
    public Hydration hydration;
    public Nutrition nutrition;
    public Temperature temperature;
    public CharacterController2k controller;
    public PlayerChat chat;
    public PlayerConstruction construction;
    public PlayerCrafting crafting;
    public PlayerEquipment equipment;
    public PlayerFurnaceUsage furnaceUsage;
    public PlayerHeartbeat heartbeat;
    public PlayerHotbar hotbar;
    public PlayerInteraction interaction;
    public PlayerInventory inventory;
    public PlayerLook look;
    public PlayerMovement movement;
    public PlayerReloading reloading;
    public PlayerRespawning respawning;
    public PlayerStorageUsage storageUsage;

    [Header("Class")]
    public Sprite classIcon; // for character selection
    [HideInInspector] public string account = "";
    [HideInInspector] public string className = "";

    [Header("Animation")]
    public float animationDirectionDampening = 0.05f;
    public float animationTurnDampening = 0.1f;
    Vector3 lastForward;

    // first allowed logout time after combat
    public double allowedLogoutTime => combat.lastCombatTime + ((NetworkManagerSurvival)NetworkManager.singleton).combatLogoutDelay;
    public double remainingLogoutTime => NetworkTime.time < allowedLogoutTime ? (allowedLogoutTime - NetworkTime.time) : 0;

    // online players cache on the server to save lots of computations
    // (otherwise we'd have to iterate NetworkServer.objects all the time)
    public static Dictionary<string, Player> onlinePlayers = new Dictionary<string, Player>();

    // localPlayer singleton for easier access from UI scripts etc.
    public static Player localPlayer;

    // helper flag to check if we should run local prediction for some effects
    // for this player (e.g. when shooting a weapon, shoot directly without
    // waiting for latency).
    // this should only ever happen for the local player, and only if not host
    // because host isn't affected by latency.
    public bool isNonHostLocalPlayer => !isServer && isLocalPlayer;

    // item cooldowns
    // it's based on a 'cooldownCategory' that can be set in ScriptableItems.
    // -> they can use their own name for a cooldown that only applies to them
    // -> they can use a category like 'HealthPotion' for a shared cooldown
    //    amongst all health potions
    // => we use hash(category) as key to significantly reduce bandwidth!
    SyncDictionaryIntDouble itemCooldowns = new SyncDictionaryIntDouble();

    // additional cooldowns dictionary for local player prediction.
    // this way we don't have to experience latency effects while firing fast
    // weapons
    // (we can't use itemCooldowns because that can only be used on server due
    //  to the delta compression nature)
    Dictionary<int, double> itemCooldownsPrediction = new Dictionary<int, double>();

    public override void OnStartLocalPlayer()
    {
        // set singleton
        localPlayer = this;
    }

    public override void OnStartServer()
    {
        onlinePlayers[name] = this;
    }

    void Start()
    {
        lastForward = transform.forward;
    }

    void OnDestroy()
    {
        // try to remove from onlinePlayers first, NO MATTER WHAT
        // -> we can not risk ever not removing it. do this before any early
        //    returns etc.
        // -> ONLY remove if THIS object was saved. this avoids a bug where
        //    a host selects a character preview, then joins the game, then
        //    only after the end of the frame the preview is destroyed,
        //    OnDestroy is called and the preview would actually remove the
        //    world player from onlinePlayers. hence making guild management etc
        //    impossible.
        if (onlinePlayers.TryGetValue(name, out Player entry) && entry == this)
            onlinePlayers.Remove(name);
    }

    // get remaining item cooldown, or 0 if none
    public float GetItemCooldown(string cooldownCategory)
    {
        // get stable hash to reduce bandwidth
        int hash = cooldownCategory.GetStableHashCode();

        // local player? then see if we have a prediction in there
        // (not for host. host has no latency, no need to simulate anything)
        if (isNonHostLocalPlayer)
        {
            if (itemCooldownsPrediction.TryGetValue(hash, out double cooldownPredictionEnd))
            {
                return NetworkTime.time >= cooldownPredictionEnd ? 0 : (float)(cooldownPredictionEnd - NetworkTime.time);
            }
        }

        // otherwise use the regular one
        if (itemCooldowns.TryGetValue(hash, out double cooldownEnd))
        {
            return NetworkTime.time >= cooldownEnd ? 0 : (float)(cooldownEnd - NetworkTime.time);
        }

        // none found
        return 0;
    }

    // reset item cooldown
    public void SetItemCooldown(string cooldownCategory, float cooldown)
    {
        // get stable hash to reduce bandwidth
        int hash = cooldownCategory.GetStableHashCode();

        // calculate end time
        double cooldownEnd = NetworkTime.time + cooldown;

        // called by local player for prediction?
        // (not for host. host has no latency, no need to simulate anything)
        if (isNonHostLocalPlayer)
            itemCooldownsPrediction[hash] = cooldownEnd;
        else
            itemCooldowns[hash] = cooldownEnd;
    }

    // animation ///////////////////////////////////////////////////////////////
    float GetAnimationJumpLeg()
    {
        return isLocalPlayer
            ? movement.jumpLeg
            : 1; // always left leg. saves Cmd+SyncVar bandwidth and no one will notice.
    }

    // Vector.Angle and Quaternion.FromToRotation and Quaternion.Angle all end
    // up clamping the .eulerAngles.y between 0 and 360, so the first overflow
    // angle from 360->0 would result in a negative value (even though we added
    // something to it), causing a rapid twitch between left and right turn
    // animations.
    //
    // the solution is to use the delta quaternion rotation.
    // when turning by 0.5, it is:
    //   0.5 when turning right (0 + angle)
    //   364.6 when turning left (360 - angle)
    // so if we assume that anything >180 is negative then that works great.
    static float AnimationDeltaUnclamped(Vector3 lastForward, Vector3 currentForward)
    {
        Quaternion rotationDelta = Quaternion.FromToRotation(lastForward, currentForward);
        float turnAngle = rotationDelta.eulerAngles.y;
        return turnAngle >= 180 ? turnAngle - 360 : turnAngle;
    }

    [ClientCallback] // don't animate on the server
    void LateUpdate()
    {
        // local velocity (based on rotation) for animations
        Vector3 localVelocity = transform.InverseTransformDirection(movement.velocity);
        float jumpLeg = GetAnimationJumpLeg();

        // Turn value so that mouse-rotating the character plays some animation
        // instead of only raw rotating the model.
        float turnAngle = AnimationDeltaUnclamped(lastForward, transform.forward);
        lastForward = transform.forward;

        // apply animation parameters to all animators.
        // there might be multiple if we use skinned mesh equipment.
        foreach (Animator animator in GetComponentsInChildren<Animator>())
        {
            animator.SetBool("DEAD", health.current == 0);
            animator.SetFloat("DirX", localVelocity.x, animationDirectionDampening, Time.deltaTime); // smooth idle<->run transitions
            animator.SetFloat("DirY", localVelocity.y, animationDirectionDampening, Time.deltaTime); // smooth idle<->run transitions
            animator.SetFloat("DirZ", localVelocity.z, animationDirectionDampening, Time.deltaTime); // smooth idle<->run transitions
            animator.SetFloat("LastFallY", movement.lastFall.y);
            animator.SetFloat("Turn", turnAngle, animationTurnDampening, Time.deltaTime); // smooth turn
            animator.SetBool("CROUCHING", movement.state == MoveState.CROUCHING);
            animator.SetBool("CRAWLING", movement.state == MoveState.CRAWLING);
            animator.SetBool("CLIMBING", movement.state == MoveState.CLIMBING);
            animator.SetBool("SWIMMING", movement.state == MoveState.SWIMMING);
            // smoothest way to do climbing-idle is to stop right where we were
            if (movement.state == MoveState.CLIMBING)
                animator.speed = localVelocity.y == 0 ? 0 : 1;
            else
                animator.speed = 1;

            // grounded detection for other players works best via .state
            // -> check AIRBORNE state instead of controller.isGrounded to have some
            //    minimum fall tolerance so we don't play the AIRBORNE animation
            //    while walking down steps etc.
            animator.SetBool("OnGround", movement.state != MoveState.AIRBORNE);
            if (controller.isGrounded) animator.SetFloat("JumpLeg", jumpLeg);

            // upper body layer
            // note: UPPERBODY_USED is fired from PlayerHotbar.OnUsedItem
            animator.SetBool("UPPERBODY_HANDS", hotbar.slots[hotbar.selection].amount == 0);
            // -> tool parameters are all set to false and then the current tool is
            //    set to true
            animator.SetBool("UPPERBODY_RIFLE", false);
            animator.SetBool("UPPERBODY_PISTOL", false);
            animator.SetBool("UPPERBODY_AXE", false);
            if (movement.state != MoveState.CLIMBING && // not while climbing
                hotbar.slots[hotbar.selection].amount > 0 &&
                hotbar.slots[hotbar.selection].item.data is WeaponItem)
            {
                WeaponItem weapon = (WeaponItem)hotbar.slots[hotbar.selection].item.data;
                if (!string.IsNullOrWhiteSpace(weapon.upperBodyAnimationParameter))
                    animator.SetBool(weapon.upperBodyAnimationParameter, true);
            }
        }
    }
}