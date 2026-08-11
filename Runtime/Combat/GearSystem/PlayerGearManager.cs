using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages the player's modular gear system.
/// Spawns and attaches gear pieces to permanent holders.
/// Holders maintain parent-child hierarchy for animation inheritance.
/// </summary>
public class PlayerGearManager : MonoBehaviour
{
    public event Action<bool> OnCoreGearReadyChanged;

    [Header("Permanent Holders (Assign in Prefab)")]
    [SerializeField] private Transform feetHolder;
    [SerializeField] private Transform chestHolder;
    [SerializeField] private Transform backpackHolder;
    [SerializeField] private Transform headHolder;
    [SerializeField] private Transform offHandsHolder;

    [Header("Current Equipped Gear")]
    [SerializeField] private GameObject currentLegsInstance;
    [SerializeField] private GameObject currentChestInstance;
    [SerializeField] private GameObject currentHeadInstance;
    [SerializeField] private GameObject currentBackpackInstance;

    [Header("Hand/Glove Gear")]
    [Tooltip("The sprite to display on weapon hand holders")]
    [SerializeField] private Sprite equippedHandSprite;

    // Cached target positions for LateUpdate enforcement
    private Vector3 cachedChestLocalPosition;
    private Vector3 cachedHeadLocalPosition;
    private Vector3 cachedChestHolderLocalPosition;
    private Vector3 cachedHeadHolderLocalPosition;
    private int enforcePositionFrames = 0; // How many more frames to enforce positions
    private const int ENFORCE_FRAME_COUNT = 5; // Enforce for this many frames after equip

    private LegGearPiece legGearPiece;
    private Animator legsAnimator;
    private Animator chestAnimator;
    private Animator headAnimator;
    private bool isCoreGearReady;

    public Animator LegsAnimator => legsAnimator;
    public SpriteRenderer LegsSpriteRenderer => legGearPiece?.GetComponent<SpriteRenderer>();
    public bool IsCoreGearReady => isCoreGearReady;

    private void SetCoreGearReady(bool ready)
    {
        if (isCoreGearReady == ready) return;

        isCoreGearReady = ready;
        OnCoreGearReadyChanged?.Invoke(ready);
    }

    private bool EvaluateCoreGearReady()
    {
        // Core animated body pieces must exist before animation playback starts.
        return currentLegsInstance != null && currentChestInstance != null && currentHeadInstance != null;
    }

    /// <summary>
    /// LateUpdate - enforces gear positions AFTER animations have run.
    /// Only active for a few frames after equipping to let animators settle.
    /// </summary>
    private void LateUpdate()
    {
        // Always enforce holder positions after animations run — the legs animator
        // bakes a default ChestHolder Y into its clips, so we must override every frame.
        if (currentChestInstance != null)
        {
            chestHolder.localPosition = cachedChestHolderLocalPosition;
        }
        if (currentHeadInstance != null && currentChestInstance != null)
        {
            headHolder.localPosition = cachedHeadHolderLocalPosition;
        }

        if (enforcePositionFrames <= 0) return;

        // Enforce instance positions for a few frames to let animators settle
        if (currentChestInstance != null)
        {
            currentChestInstance.transform.localPosition = cachedChestLocalPosition;
        }
        if (currentHeadInstance != null)
        {
            currentHeadInstance.transform.localPosition = cachedHeadLocalPosition;
        }

        enforcePositionFrames--;
    }

    /// <summary>
    /// Get all gear animators (legs, chest, head) for synchronized animation
    /// </summary>
    public Animator[] GetAllGearAnimators()
    {
        var animators = new System.Collections.Generic.List<Animator>();
        if (legsAnimator != null) animators.Add(legsAnimator);
        if (chestAnimator != null) animators.Add(chestAnimator);
        if (headAnimator != null) animators.Add(headAnimator);
        return animators.ToArray();
    }

    /// <summary>
    /// Get all gear sprite renderers (legs, chest, head) for synchronized flipping
    /// </summary>
    public SpriteRenderer[] GetAllGearSpriteRenderers()
    {
        var renderers = new System.Collections.Generic.List<SpriteRenderer>();
        if (currentLegsInstance != null)
        {
            SpriteRenderer legRenderer = currentLegsInstance.GetComponent<SpriteRenderer>();
            if (legRenderer != null) renderers.Add(legRenderer);
        }
        if (currentChestInstance != null)
        {
            SpriteRenderer chestRenderer = currentChestInstance.GetComponent<SpriteRenderer>();
            if (chestRenderer != null) renderers.Add(chestRenderer);
        }
        if (currentHeadInstance != null)
        {
            SpriteRenderer headRenderer = currentHeadInstance.GetComponent<SpriteRenderer>();
            if (headRenderer != null) renderers.Add(headRenderer);
        }
        return renderers.ToArray();
    }

    /// <summary>
    /// Equip starting gear from ClassData (no order dependency)
    /// </summary>
    public void EquipStartingGear(ClassData classData)
    {
        if (classData == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip gear from null ClassData!");
            return;
        }
        // Spawn gear pieces in any order - holders are permanent
        if (classData.startingFeetPrefab != null)
        {
            EquipLegs(classData.startingFeetPrefab, classData.startingFeetConfig);
        }
        else
        {
            Debug.LogError("[PlayerGearManager] No starting feet prefab assigned in ClassData!");
        }

        if (classData.startingChestPrefab != null)
        {
            EquipChest(classData.startingChestPrefab, classData.startingChestConfig);
        }

        if (classData.startingHeadPrefab != null)
        {
            EquipHead(classData.startingHeadPrefab, classData.startingHeadConfig);
        }
        if (classData.startingHandsPrefab != null)
        {
            EquipHands(classData.startingHandsPrefab, classData.startingHandsConfig);
        }

        // Use delayed update to allow animators to initialize first
        UpdateAllGearPositionsDelayed();
    }

    /// <summary>
    /// Equip leg gear - attaches to FeetHolder
    /// </summary>
    public void EquipLegs(GameObject legPrefab, ArmorConfig config)
    {
        PlayerController playerController = GetComponent<PlayerController>();
        playerController?.SetGearAnimationReady(false);

        if (legPrefab == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip legs - null prefab");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        if (feetHolder == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip legs - FeetHolder not assigned");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        // Remove old legs
        if (currentLegsInstance != null)
        {
            Destroy(currentLegsInstance);
        }

        // Spawn legs as child of FeetHolder
        currentLegsInstance = Instantiate(legPrefab, feetHolder);
        currentLegsInstance.transform.localPosition = Vector3.zero;
        currentLegsInstance.transform.localRotation = Quaternion.identity;
        currentLegsInstance.transform.localScale = Vector3.one;
        currentLegsInstance.name = "EquippedLegs";

        // Get components
        legGearPiece = currentLegsInstance.GetComponent<LegGearPiece>();
        legsAnimator = currentLegsInstance.GetComponent<Animator>();

        if (legGearPiece == null)
        {
            Debug.LogError("[PlayerGearManager] Leg prefab missing LegGearPiece component!");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        // Initialize with player animator reference
        if (playerController != null)
        {
            // Pass the root animator so LegGearPiece.Update() mirrors it every frame.
            // The root Animator is driven by NetworkAnimator on all clients (local AND remote),
            // so this makes leg animations correct for observers without any extra RPCs.
            Animator rootAnimator = GetComponent<Animator>();
            legGearPiece.Initialize(config, rootAnimator);

            // Refresh PlayerController's animator reference to point to legs
            playerController.RefreshGearAnimators();
        }

        // Update all gear positions (chest and head depend on legs)
        UpdateAllGearPositions();
        bool ready = EvaluateCoreGearReady();
        SetCoreGearReady(ready);
        playerController?.SetGearAnimationReady(ready);
    }

    /// <summary>
    /// Equip chest gear - attaches to ChestHolder
    /// </summary>
    public void EquipChest(GameObject chestPrefab, ArmorConfig config)
    {
        PlayerController playerController = GetComponent<PlayerController>();
        playerController?.SetGearAnimationReady(false);

        if (chestPrefab == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip chest - null prefab");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        if (chestHolder == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip chest - ChestHolder not assigned");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        // Remove old chest
        if (currentChestInstance != null)
        {
            Destroy(currentChestInstance);
        }

        // Spawn chest as child of ChestHolder
        currentChestInstance = Instantiate(chestPrefab, chestHolder);
        currentChestInstance.transform.localRotation = Quaternion.identity;
        currentChestInstance.transform.localScale = Vector3.one;
        currentChestInstance.name = "EquippedChest";

        ChestGearPiece chestPiece = currentChestInstance.GetComponent<ChestGearPiece>();
        chestAnimator = currentChestInstance.GetComponent<Animator>();

        // Explicitly initialize GearAnimatorSync now so it can sync from the first frame,
        // rather than waiting for its deferred Start() to search for a parent animator.
        GearAnimatorSync chestSync = currentChestInstance.GetComponent<GearAnimatorSync>();
        if (chestSync != null)
        {
            Animator rootAnimator = GetComponent<Animator>();
            chestSync.Initialize(rootAnimator);
        }

        if (chestPiece != null)
        {
            // Update all gear positions (chest and head)
            UpdateAllGearPositions();
            bool ready = EvaluateCoreGearReady();
            SetCoreGearReady(ready);
            playerController?.SetGearAnimationReady(ready);
        }
        else
        {
            Debug.LogError("[PlayerGearManager] Chest prefab missing ChestGearPiece component!");
            Destroy(currentChestInstance);
            currentChestInstance = null;
            chestAnimator = null;
            bool ready = EvaluateCoreGearReady();
            SetCoreGearReady(ready);
            playerController?.SetGearAnimationReady(ready);
        }
    }

    /// <summary>
    /// Equip head gear - attaches to HeadHolder
    /// </summary>
    public void EquipHead(GameObject headPrefab, ArmorConfig config)
    {
        PlayerController playerController = GetComponent<PlayerController>();
        playerController?.SetGearAnimationReady(false);

        if (headPrefab == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip head - null prefab");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        if (headHolder == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip head - HeadHolder not assigned");
            SetCoreGearReady(EvaluateCoreGearReady());
            return;
        }

        // Remove old head
        if (currentHeadInstance != null)
        {
            Destroy(currentHeadInstance);
        }

        // Spawn head as child of HeadHolder
        currentHeadInstance = Instantiate(headPrefab, headHolder);
        currentHeadInstance.transform.localPosition = Vector3.zero;
        currentHeadInstance.transform.localRotation = Quaternion.identity;
        currentHeadInstance.transform.localScale = Vector3.one;
        currentHeadInstance.name = "EquippedHead";

        HeadGearPiece headPiece = currentHeadInstance.GetComponent<HeadGearPiece>();
        headAnimator = currentHeadInstance.GetComponent<Animator>();

        // Explicitly initialize GearAnimatorSync now so it can sync from the first frame,
        // rather than waiting for its deferred Start() to search for a parent animator.
        GearAnimatorSync headSync = currentHeadInstance.GetComponent<GearAnimatorSync>();
        if (headSync != null)
        {
            Animator rootAnimator = GetComponent<Animator>();
            headSync.Initialize(rootAnimator);
        }

        if (headPiece != null)
        {
            // Update all gear positions (head depends on chest)
            UpdateAllGearPositions();
            bool ready = EvaluateCoreGearReady();
            SetCoreGearReady(ready);
            playerController?.SetGearAnimationReady(ready);
        }
        else
        {
            Debug.LogError("[PlayerGearManager] Head prefab missing HeadGearPiece component!");
            Destroy(currentHeadInstance);
            currentHeadInstance = null;
            headAnimator = null;
            bool ready = EvaluateCoreGearReady();
            SetCoreGearReady(ready);
            playerController?.SetGearAnimationReady(ready);
        }
    }

    public void EquipHands(GameObject handsPrefab, ArmorConfig config)
    {
        if (handsPrefab == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip hands - null prefab");
            return;
        }

        // For hand gear, we don't have a permanent holder - they are represented as sprites on weapon hand holders
        // So instead of instantiating, we just set the equippedHandSprite and let it apply to the current weapon
        SpriteRenderer sr = handsPrefab.GetComponent<SpriteRenderer>();
        SetHandSprite(sr?.sprite);
    }

    /// <summary>
    /// Equip backpack gear - attaches to BackpackHolder
    /// </summary>
    public void EquipBackpack(GameObject backpackPrefab, ArmorConfig config)
    {
        if (backpackPrefab == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip null backpack prefab!");
            return;
        }

        if (backpackHolder == null)
        {
            Debug.LogError("[PlayerGearManager] BackpackHolder not assigned! Assign in PlayerCharacter prefab.");
            return;
        }

        // Remove old backpack if exists
        if (currentBackpackInstance != null)
        {
            Destroy(currentBackpackInstance);
        }

        // Spawn backpack as child of BackpackHolder
        currentBackpackInstance = Instantiate(backpackPrefab, backpackHolder);
        currentBackpackInstance.transform.localPosition = Vector3.zero;
        currentBackpackInstance.transform.localRotation = Quaternion.identity;
        currentBackpackInstance.transform.localScale = Vector3.one;
        currentBackpackInstance.name = "EquippedBackpack";
    }

    /// <summary>
    /// Load visual gear from saved CharacterData
    /// Equips the actual armor/weapon visuals that were equipped when the character was saved
    /// NOTE: This is legacy - PlayerController.LoadSavedVisualGear() now uses direct iteration
    /// </summary>
    public void LoadVisualGear(Dictionary<GearSlot, ItemInstance> equippedGear, ClassData classData)
    {
        if (equippedGear == null)
        {
            Debug.LogWarning("[PlayerGearManager] Cannot load visual gear - equipped gear is null");
            return;
        }

        if (classData == null)
        {
            Debug.LogWarning("[PlayerGearManager] Cannot load visual gear - ClassData is null");
            return;
        }

        // Track which slots have been loaded
        bool feetLoaded = false;
        bool chestLoaded = false;
        bool headLoaded = false;
        bool handsLoaded = false;

        // Load visual gear for each saved gear slot
        foreach (var kvp in equippedGear)
        {
            GearSlot slot = kvp.Key;
            ItemInstance item = kvp.Value;

            if (item == null || string.IsNullOrEmpty(item.additionalData))
            {
                continue;
            }

            // Load armor visual
            if (item.itemType.ToLower() == "armor")
            {
                ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(item.additionalData);

                if (armorData != null && !string.IsNullOrEmpty(armorData.armorConfigName))
                {
                    ArmorConfig config = ArmorConfigRegistry.GetConfig(armorData.armorConfigName);
                    if (config != null)
                    {
                        // Equip based on slot
                        switch (slot)
                        {
                            case GearSlot.Head:
                                if (config.headGearPrefab != null)
                                {
                                    EquipHead(config.headGearPrefab, config);
                                    headLoaded = true;
                                }
                                break;
                            case GearSlot.Chest:
                                if (config.chestGearPrefab != null)
                                {
                                    EquipChest(config.chestGearPrefab, config);
                                    chestLoaded = true;
                                }
                                break;
                            case GearSlot.Feet:
                                if (config.legGearPrefab != null)
                                {
                                    EquipLegs(config.legGearPrefab, config);
                                    feetLoaded = true;
                                }
                                break;
                            case GearSlot.Hands:
                                if (config.handsGearPrefab != null)
                                {
                                    EquipHands(config.handsGearPrefab, config);
                                    handsLoaded = true;
                                }
                                break;
                        }
                    }
                }
            }
        }

        // Load starter gear for any slots that weren't loaded
        if (!feetLoaded && classData.startingFeetPrefab != null)
        {
            EquipLegs(classData.startingFeetPrefab, classData.startingFeetConfig);
        }

        if (!chestLoaded && classData.startingChestPrefab != null)
        {
            EquipChest(classData.startingChestPrefab, classData.startingChestConfig);
        }

        if (!headLoaded && classData.startingHeadPrefab != null)
        {
            EquipHead(classData.startingHeadPrefab, classData.startingHeadConfig);
        }

        if (!handsLoaded && classData.startingHandsPrefab != null)
        {
            EquipHands(classData.startingHandsPrefab, classData.startingHandsConfig);
        }

        // Final position update
        UpdateAllGearPositionsDelayed();
    }

    /// <summary>
    /// Set the hand/glove sprite that will be displayed on weapon hand holders.
    /// Call this when glove gear is equipped.
    /// </summary>
    public void SetHandSprite(Sprite handSprite)
    {
        equippedHandSprite = handSprite;

        // Update both main and offhand weapon's hand holders if weapons are equipped
        ApplyHandSpriteToWeapon();

        // Also apply to offhand weapon if one is equipped
        OffHandWeaponHolder offhandHolder = GetComponent<OffHandWeaponHolder>();
        if (offhandHolder != null && offhandHolder.HasWeapon())
        {
            ApplyHandSpriteToOffhandWeapon();
        }
    }

    /// <summary>
    /// Apply the equipped hand sprite to the current weapon's hand holders.
    /// Finds any child named "HandHolder" and sets its sprite.
    /// </summary>
    private void ApplyHandSpriteToWeapon(string sortingLayer = null, int? sortingOrder = null)
    {
        WeaponHolder weaponHolder = GetComponent<WeaponHolder>();
        if (weaponHolder == null)
        {
            return;
        }

        GameObject currentWeapon = weaponHolder.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            return;
        }
        // If sorting info not provided, read from weapon sprite
        if (sortingLayer == null || sortingOrder == null)
        {
            SpriteRenderer weaponSR = null;
            Transform weaponSpriteChild = currentWeapon.transform.Find("WeaponSprite");
            if (weaponSpriteChild != null)
            {
                weaponSR = weaponSpriteChild.GetComponent<SpriteRenderer>();
            }
            if (weaponSR == null)
            {
                foreach (SpriteRenderer sr in currentWeapon.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (!sr.gameObject.name.Contains("HandHolder"))
                    {
                        weaponSR = sr;
                        break;
                    }
                }
            }

            if (weaponSR != null)
            {
                sortingLayer = sortingLayer ?? weaponSR.sortingLayerName;
                sortingOrder = sortingOrder ?? weaponSR.sortingOrder;
            }
        }

        string weaponSortingLayer = sortingLayer ?? "Default";
        int weaponSortingOrder = sortingOrder ?? 0;

        // Find all children named "HandHolder" and set their sprite
        int foundCount = 0;
        foreach (Transform child in currentWeapon.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains("HandHolder"))
            {
                foundCount++;
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = child.gameObject.AddComponent<SpriteRenderer>();
                }
                sr.sprite = equippedHandSprite;
                sr.sortingLayerName = weaponSortingLayer;
                sr.sortingOrder = weaponSortingOrder + 1; // Render in front of weapon
            }
        }

        // If weapon has fewer than 2 HandHolders AND no offhand weapon is equipped, also equip hand sprite in offHandsHolder
        OffHandWeaponHolder offhandHolder = GetComponent<OffHandWeaponHolder>();
        bool hasOffhandWeapon = offhandHolder != null && offhandHolder.HasWeapon();

        if (foundCount < 2 && !hasOffhandWeapon && offHandsHolder != null && equippedHandSprite != null)
        {

            // Find or create EquippedHands child
            Transform equippedHandsChild = offHandsHolder.Find("EquippedHands");
            if (equippedHandsChild == null)
            {
                GameObject equippedHandsObj = new GameObject("EquippedHands");
                equippedHandsObj.transform.SetParent(offHandsHolder, false);
                equippedHandsObj.transform.localPosition = Vector3.zero;
                equippedHandsObj.transform.localRotation = Quaternion.identity;
                equippedHandsObj.transform.localScale = Vector3.one;
                equippedHandsChild = equippedHandsObj.transform;
            }

            // Get or add SpriteRenderer on EquippedHands child
            SpriteRenderer offHandSR = equippedHandsChild.GetComponent<SpriteRenderer>();
            if (offHandSR == null)
            {
                offHandSR = equippedHandsChild.gameObject.AddComponent<SpriteRenderer>();
            }

            offHandSR.sprite = equippedHandSprite;
            offHandSR.sortingLayerName = weaponSortingLayer;
            offHandSR.sortingOrder = weaponSortingOrder + 1; // Render in front of weapon

        }
        else if ((foundCount >= 2 || hasOffhandWeapon) && offHandsHolder != null)
        {
            // If weapon has 2+ HandHolders OR offhand weapon is equipped, clear the offhand sprite
            Transform equippedHandsChild = offHandsHolder.Find("EquippedHands");
            if (equippedHandsChild != null)
            {
                SpriteRenderer offHandSR = equippedHandsChild.GetComponent<SpriteRenderer>();
                if (offHandSR != null)
                {
                    offHandSR.sprite = null;
                }
            }
        }
    }

    /// <summary>
    /// Called when a new weapon is equipped to apply hand sprites.
    /// </summary>
    public void OnWeaponEquipped(string sortingLayer, int sortingOrder)
    {
        ApplyHandSpriteToWeapon(sortingLayer, sortingOrder);
        UpdateAllGearPositionsDelayed();
    }

    /// <summary>
    /// Called when a new offhand weapon is equipped to apply hand sprites.
    /// </summary>
    public void OnOffhandWeaponEquipped(string sortingLayer, int sortingOrder)
    {
        ApplyHandSpriteToOffhandWeapon(sortingLayer, sortingOrder);
        UpdateAllGearPositionsDelayed();
    }

    /// <summary>
    /// Apply equipped hand sprite to the offhand weapon's HandHolder children.
    /// Also clears the offHandsHolder sprite since the hand is now on the weapon.
    /// </summary>
    private void ApplyHandSpriteToOffhandWeapon(string sortingLayer = null, int? sortingOrder = null)
    {
        OffHandWeaponHolder offhandHolder = GetComponent<OffHandWeaponHolder>();
        if (offhandHolder == null)
        {
            return;
        }

        GameObject offhandWeapon = offhandHolder.GetCurrentWeapon();
        // If sorting info not provided, read from weapon sprite
        if (sortingLayer == null || sortingOrder == null)
        {
            SpriteRenderer weaponSR = null;
            Transform weaponSpriteChild = offhandWeapon.transform.Find("WeaponSprite");
            if (weaponSpriteChild != null)
            {
                weaponSR = weaponSpriteChild.GetComponent<SpriteRenderer>();
            }
            if (weaponSR == null)
            {
                foreach (SpriteRenderer sr in offhandWeapon.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (!sr.gameObject.name.Contains("HandHolder"))
                    {
                        weaponSR = sr;
                        break;
                    }
                }
            }

            if (weaponSR != null)
            {
                sortingLayer = sortingLayer ?? weaponSR.sortingLayerName;
                sortingOrder = sortingOrder ?? weaponSR.sortingOrder;
            }
        }

        string weaponSortingLayer = sortingLayer ?? "Default";
        int weaponSortingOrder = sortingOrder ?? 0;


        // Find all children named "HandHolder" and set their sprite
        int foundCount = 0;
        foreach (Transform child in offhandWeapon.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains("HandHolder"))
            {
                foundCount++;
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = child.gameObject.AddComponent<SpriteRenderer>();
                }
                sr.sprite = equippedHandSprite;
                sr.sortingLayerName = weaponSortingLayer;
                sr.sortingOrder = weaponSortingOrder + 1; // Render in front of weapon
            }
        }
        // Clear the offHandsHolder sprite since the hand is now on the weapon itself
        if (offHandsHolder != null)
        {
            Transform equippedHandsChild = offHandsHolder.Find("EquippedHands");
            if (equippedHandsChild != null)
            {
                SpriteRenderer offHandSR = equippedHandsChild.GetComponent<SpriteRenderer>();
                if (offHandSR != null)
                {
                    offHandSR.sprite = null;
                }
            }
        }
    }

    /// <summary>
    /// Update positions of all equipped gear pieces to align lockpoints with holders.
    /// Call this whenever any gear piece is equipped to ensure proper alignment.
    /// </summary>
    public void UpdateAllGearPositions()
    {
        // Position chest relative to legs
        UpdateChestPosition();

        // Position head relative to chest
        UpdateHeadPosition();
    }

    /// <summary>
    /// Update gear positions after a one-frame delay to allow animators to initialize.
    /// Use this when equipping gear at spawn time.
    /// </summary>
    public void UpdateAllGearPositionsDelayed()
    {
        // Guard: FishNet may fire OnStartNetwork on a client-side clone that is
        // still inactive — StartCoroutine would throw.
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[PlayerGearManager] Skipping delayed gear update — GameObject is inactive");
            return;
        }
        StartCoroutine(UpdateGearPositionsNextFrame());
    }

    private System.Collections.IEnumerator UpdateGearPositionsNextFrame()
    {

        // Wait multiple frames to let animators fully initialize and settle
        // WaitForEndOfFrame alone is not enough - animators may override positions
        yield return new WaitForEndOfFrame();
        yield return null; // Wait one more frame
        yield return null; // And another for good measure


        // Now update positions
        UpdateAllGearPositions();

        // Enable position enforcement for a few frames to prevent animator override
        enforcePositionFrames = ENFORCE_FRAME_COUNT;
    }

    /// <summary>
    /// Position the ChestHolder based on the equipped chest piece's Y offset.
    /// The EquippedChest itself sits at localPosition zero — the holder moves.
    /// </summary>
    private void UpdateChestPosition()
    {
        if (currentChestInstance == null)
        {
            return;
        }

        ChestGearPiece chestPiece = currentChestInstance.GetComponent<ChestGearPiece>();
        if (chestPiece == null) return;

        // Move the permanent ChestHolder so the chest piece sits at the right height
        chestHolder.localPosition = new Vector3(0f, chestPiece.ChestHolderYOffset, 0f);
        cachedChestHolderLocalPosition = chestHolder.localPosition;

        // Equipped gear sits at zero relative to its holder
        currentChestInstance.transform.localPosition = Vector3.zero;
        cachedChestLocalPosition = Vector3.zero;
    }

    /// <summary>
    /// Position the HeadHolder based on the equipped chest piece's head offset.
    /// The EquippedHead itself sits at localPosition zero — the holder moves.
    /// </summary>
    private void UpdateHeadPosition()
    {
        if (currentHeadInstance == null)
        {
            return;
        }

        if (currentChestInstance == null)
        {
            return;
        }

        ChestGearPiece chestPiece = currentChestInstance.GetComponent<ChestGearPiece>();
        if (chestPiece == null) return;

        // Move the permanent HeadHolder so the head piece sits at the right height
        headHolder.localPosition = new Vector3(0f, chestPiece.HeadHolderYOffset, 0f);
        cachedHeadHolderLocalPosition = headHolder.localPosition;

        // Equipped gear sits at zero relative to its holder
        currentHeadInstance.transform.localPosition = Vector3.zero;
        cachedHeadLocalPosition = Vector3.zero;
    }

    /// <summary>
    /// Get the legs animator for PlayerController to use
    /// </summary>
    public Animator GetLegsAnimator()
    {
        return legsAnimator;
    }

    #region Visual Gear Equipping (moved from PlayerController)

    /// <summary>
    /// Equip armor visual from an ItemInstance.
    /// Uses the same logic as InventoryItemUI.EquipArmorOnPlayer.
    /// </summary>
    public void EquipArmorVisual(ItemInstance armorItem, GearSlot slotType)
    {
        if (armorItem == null)
        {
            Debug.LogError("[PlayerGearManager] Cannot equip armor - armorItem is null");
            return;
        }

        if (string.IsNullOrEmpty(armorItem.additionalData))
        {
            Debug.LogError($"[PlayerGearManager] Cannot equip armor '{armorItem.displayName}' - no additionalData");
            return;
        }

        ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(armorItem.additionalData);
        if (armorData == null || string.IsNullOrEmpty(armorData.armorConfigName))
        {
            Debug.LogError($"[PlayerGearManager] Cannot equip armor '{armorItem.displayName}' - invalid ArmorGearData");
            return;
        }

        EquipArmorByConfigName(armorData.armorConfigName, slotType);
    }

    /// <summary>
    /// Equip armor visual by config name (used by network sync).
    /// </summary>
    public void EquipArmorByConfigName(string configName, GearSlot slotType)
    {
        if (string.IsNullOrEmpty(configName))
        {
            Debug.LogWarning($"[PlayerGearManager] Cannot equip armor - configName is null/empty for slot {slotType}");
            return;
        }

        ArmorConfig armorConfig = ArmorConfigRegistry.GetConfig(configName);
        if (armorConfig == null)
        {
            Debug.LogError($"[PlayerGearManager] Armor config '{configName}' not found in registry");
            return;
        }

        // Equip based on slot type
        switch (slotType)
        {
            case GearSlot.Head:
                if (armorConfig.headGearPrefab != null)
                {
                    EquipHead(armorConfig.headGearPrefab, armorConfig);
                }
                break;

            case GearSlot.Chest:
                if (armorConfig.chestGearPrefab != null)
                {
                    EquipChest(armorConfig.chestGearPrefab, armorConfig);
                }
                break;

            case GearSlot.Hands:
                if (armorConfig.handsGearPrefab != null)
                {
                    EquipHands(armorConfig.handsGearPrefab, armorConfig);
                }
                break;

            case GearSlot.Feet:
                if (armorConfig.legGearPrefab != null)
                {
                    EquipLegs(armorConfig.legGearPrefab, armorConfig);
                }
                break;

            case GearSlot.Backpack:
                if (armorConfig.backpackGearPrefab != null)
                {
                    EquipBackpack(armorConfig.backpackGearPrefab, armorConfig);
                }
                break;

            default:
                Debug.LogWarning($"[PlayerGearManager] Unhandled armor slot type: {slotType}");
                break;
        }
    }

    /// <summary>
    /// Load starter gear for a specific slot from ClassData.
    /// </summary>
    public void LoadStarterGearForSlot(GearSlot slot, ClassData classData)
    {
        if (classData == null) return;

        switch (slot)
        {
            case GearSlot.Feet:
                if (classData.startingFeetPrefab != null)
                {
                    EquipLegs(classData.startingFeetPrefab, classData.startingFeetConfig);
                }
                break;
            case GearSlot.Chest:
                if (classData.startingChestPrefab != null)
                {
                    EquipChest(classData.startingChestPrefab, classData.startingChestConfig);
                }
                break;
            case GearSlot.Head:
                if (classData.startingHeadPrefab != null)
                {
                    EquipHead(classData.startingHeadPrefab, classData.startingHeadConfig);
                }
                break;
            case GearSlot.Hands:
                if (classData.startingHandsPrefab != null)
                {
                    EquipHands(classData.startingHandsPrefab, classData.startingHandsConfig);
                }
                break;
        }
    }

    /// <summary>
    /// Populate CharacterData.equippedGear with starter ItemInstances from ClassData.
    /// Called on death to reset gear inventory without doing visual equipping.
    /// Visual gear is handled by LoadVisualGear when the character respawns.
    /// </summary>
    public static void PopulateStarterGearItems(CharacterData characterData)
    {
        if (characterData == null || characterData.classData == null)
        {
            Debug.LogWarning("[PlayerGearManager] Cannot populate starter gear - missing CharacterData or ClassData");
            return;
        }

        // Initialize equippedGear dictionary if null
        if (characterData.equippedGear == null)
        {
            characterData.equippedGear = new Dictionary<GearSlot, ItemInstance>();
        }

        ClassData classData = characterData.classData;

        // Generate starter weapon (main hand)
        if (classData.availableWeapons != null && classData.availableWeapons.Length > 0)
        {
            WeaponConfig weaponConfig = classData.availableWeapons[0];
            if (weaponConfig != null)
            {
                ItemInstance weaponItem = ItemGenerator.GenerateWeaponFromConfig(weaponConfig, 0);
                if (weaponItem != null)
                {
                    characterData.equippedGear[GearSlot.Weapon] = weaponItem;
                    characterData.mainHandWeaponConfig = weaponConfig;
                }
            }
        }

        // Generate starter feet
        if (classData.startingFeetConfig != null)
        {
            ItemInstance feetItem = ItemGenerator.GenerateArmorFromConfig(classData.startingFeetConfig, 0);
            if (feetItem != null)
            {
                characterData.equippedGear[GearSlot.Feet] = feetItem;
            }
        }

        // Generate starter chest
        if (classData.startingChestConfig != null)
        {
            ItemInstance chestItem = ItemGenerator.GenerateArmorFromConfig(classData.startingChestConfig, 0);
            if (chestItem != null)
            {
                characterData.equippedGear[GearSlot.Chest] = chestItem;
            }
        }

        // Generate starter head
        if (classData.startingHeadConfig != null)
        {
            ItemInstance headItem = ItemGenerator.GenerateArmorFromConfig(classData.startingHeadConfig, 0);
            if (headItem != null)
            {
                characterData.equippedGear[GearSlot.Head] = headItem;
            }
        }

        // Generate starter hands
        if (classData.startingHandsConfig != null)
        {
            ItemInstance handsItem = ItemGenerator.GenerateArmorFromConfig(classData.startingHandsConfig, 0);
            if (handsItem != null)
            {
                characterData.equippedGear[GearSlot.Hands] = handsItem;
            }
        }

    }

    #endregion
}
