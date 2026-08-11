using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Player-side orchestration layer for level-up rewards.
/// Phase 1 owns reward round scheduling and tracks in-round investment into
/// trait-granted abilities and the run's starting weapon.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LevelUpManager))]
public class LevelUpRewardDirector : MonoBehaviour
{
    [Header("Round Rules")]
    [Tooltip("Ordered rules that decide which reward round is requested for a given player level. First match wins.")]
    [SerializeField] private List<LevelUpRewardRoundRule> roundRules = new List<LevelUpRewardRoundRule>
    {
        new LevelUpRewardRoundRule
        {
            roundType = LevelUpRewardRoundType.Ability,
            firstLevel = 2,
            repeatInterval = 5,
            matchFirstLevelOnly = false
        }
    };

    [Header("Dispatch")]
    [Tooltip("When true, the director automatically forwards reward rounds to the existing TraitRoller as a legacy provider.")]
    [SerializeField] private bool autoDispatchToTraitRoller = true;
    
    /// <summary>
    /// Allows external components (e.g. LevelUpSequencer) to take over dispatching to TraitRoller.
    /// Set to false before OnEnable is called, or during Awake of the overriding component.
    /// </summary>
    public bool AutoDispatchToTraitRoller { get => autoDispatchToTraitRoller; set => autoDispatchToTraitRoller = value; }

    [Tooltip("Ability rounds still use legacy ability-trait rolls until the dedicated ability reward provider is added.")]
    [SerializeField] private TraitRollType legacyAbilityRoundTraitRollType = TraitRollType.Ability;

    [Header("Upgrade Rules")]
    [SerializeField] private int maxAbilityTraits = 4;
    [SerializeField] private int levelsPerAbilityUpgrade = 5;
    [SerializeField] private int maxAbilityUpgradesPerAbility = 2;

    [Header("Keystone Rules")]
    [Tooltip("Number of tag-levels (traits carrying that tag) a player must accumulate before a Keystone roll is triggered. " +
             "Each multiple of this threshold triggers one additional roll.")]
    [SerializeField] private int keystoneTagThreshold = 10;

    private PlayerController playerController;
    private CharacterTraitManager traitManager;
    private CharacterAbilityManager abilityManager;
    private TraitRoller traitRoller;
    private CharacterData characterData;
    
    // Track ability upgrade rolls already triggered for current level to prevent duplicates
    private HashSet<string> _triggeredUpgradeRolls = new HashSet<string>();
    // Track which keystone tag thresholds have already been triggered ("tagName@N" format)
    private readonly HashSet<string> _triggeredKeystoneTags = new HashSet<string>();

    public event Action<LevelUpRewardRoundContext> OnRewardRoundRequested;
    public event Action<AbilityRewardProgression> OnAbilityLevelChanged;
    public event Action<WeaponRewardProgression> OnWeaponLevelChanged;

    public CharacterData CharacterData => characterData;

    private bool IsOwner
    {
        get
        {
            if (playerController != null && playerController.IsSpawned)
                return playerController.IsOwner;

            return true;
        }
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        traitManager = GetComponent<CharacterTraitManager>();
        abilityManager = GetComponent<CharacterAbilityManager>();
        traitRoller = GetComponent<TraitRoller>();
    }

    private void OnEnable()
    {
        LevelUpManager.OnLevelUp += HandleLevelUp;

        if (traitManager != null)
            traitManager.OnTraitUnlocked += HandleTraitUnlocked;

        if (abilityManager != null)
            abilityManager.OnWeaponAbilityChanged += HandleWeaponAbilityChanged;

        OnAbilityLevelChanged += HandleAbilityLevelChanged;
    }

    private void OnDisable()
    {
        LevelUpManager.OnLevelUp -= HandleLevelUp;

        if (traitManager != null)
            traitManager.OnTraitUnlocked -= HandleTraitUnlocked;

        if (abilityManager != null)
            abilityManager.OnWeaponAbilityChanged -= HandleWeaponAbilityChanged;

        OnAbilityLevelChanged -= HandleAbilityLevelChanged;
    }

    /// <summary>
    /// When an ability reaches the upgrade threshold (e.g., level 5), trigger a special roll
    /// for ability upgrade traits that specifically target that ability.
    /// </summary>
    private void HandleAbilityLevelChanged(AbilityRewardProgression progression)
    {
        if (progression == null || string.IsNullOrEmpty(progression.abilityID)) return;
        if (!IsOwner) return;

        // Log ability level tracking
        int upgradesAvailable = progression.level / levelsPerAbilityUpgrade;
        int upgradesTaken = progression.upgradeCount;
        bool canTakeUpgrade = progression.CanTakeUpgrade(levelsPerAbilityUpgrade, maxAbilityUpgradesPerAbility);
        
        Debug.Log($"[LevelUpRewardDirector] Ability '{progression.abilityName}' level changed to {progression.level}. " +
                  $"Upgrades: {upgradesTaken}/{upgradesAvailable} (max {maxAbilityUpgradesPerAbility}). " +
                  $"Next upgrade at level {(upgradesTaken + 1) * levelsPerAbilityUpgrade}. " +
                  $"Can take upgrade: {canTakeUpgrade}");

        // Check if the ability just reached the upgrade threshold
        if (progression.level == levelsPerAbilityUpgrade && traitRoller != null && abilityManager != null)
        {
            // Prevent duplicate rolls - use ability ID + level as the key
            string rollKey = $"{progression.abilityID}@{progression.level}";
            if (_triggeredUpgradeRolls.Contains(rollKey))
            {
                Debug.Log($"[LevelUpRewardDirector] Skipping duplicate upgrade roll for '{progression.abilityName}' at level {progression.level}");
                return;
            }
            _triggeredUpgradeRolls.Add(rollKey);
            
            // Find the AbilityConfig from the CharacterAbilityManager
            AbilityConfig targetConfig = FindAbilityConfigByID(progression.abilityID);
            if (targetConfig != null)
            {
                Debug.Log($"<color=yellow>[LevelUpRewardDirector] ABILITY UPGRADE ROLL TRIGGERED!</color> " +
                          $"Ability '{progression.abilityName}' reached level {progression.level} — rolling ability upgrade traits");
                traitRoller.RollAbilityUpgradeTraitsFor(targetConfig, progression.level);
            }
            else
            {
                Debug.LogWarning($"[LevelUpRewardDirector] Could not find AbilityConfig for '{progression.abilityID}' to roll upgrades");
            }
        }
        else if (progression.level > 0 && progression.level % levelsPerAbilityUpgrade == 0 && progression.level > levelsPerAbilityUpgrade)
        {
            // Ability reached another milestone but we only roll on first threshold for now
            Debug.Log($"[LevelUpRewardDirector] Ability '{progression.abilityName}' reached milestone level {progression.level} " +
                      $"(upgrade roll only triggers at level {levelsPerAbilityUpgrade})");
        }
    }

    /// <summary>
    /// Find an AbilityConfig by its ID (asset name) from the CharacterAbilityManager.
    /// </summary>
    private AbilityConfig FindAbilityConfigByID(string abilityID)
    {
        if (abilityManager == null || string.IsNullOrEmpty(abilityID)) return null;

        // Check weapon ability
        AbilityConfig weaponConfig = abilityManager.GetWeaponAbilityRef()?.Config;
        if (weaponConfig != null && weaponConfig.name == abilityID)
            return weaponConfig;

        // Check dash ability
        AbilityConfig dashConfig = abilityManager.GetDashAbilityRef()?.Config;
        if (dashConfig != null && dashConfig.name == abilityID)
            return dashConfig;

        // Check active trait abilities
        foreach (var abilityRef in abilityManager.GetActiveTraitAbilityRefs())
        {
            if (abilityRef?.Config != null && abilityRef.Config.name == abilityID)
                return abilityRef.Config;
        }

        // Check passive trait abilities
        foreach (var abilityRef in abilityManager.GetPassiveTraitAbilityRefs())
        {
            if (abilityRef?.Config != null && abilityRef.Config.name == abilityID)
                return abilityRef.Config;
        }

        return null;
    }

    public void SetCharacterData(CharacterData data)
    {
        characterData = data;
        if (characterData == null)
            return;

        _ = characterData.AbilityRewardProgressionList;
        SyncTrackedAbilities();
        SyncTrackedWeapon(resetIfWeaponChanged: false);
        // Pre-mark all already-reached thresholds so loading a save doesn't re-fire rolls.
        PreloadKeystoneTagThresholds();
    }

    public LevelUpRewardRoundContext BuildRoundContext(int newLevel)
    {
        bool includeAbilityTraits = false;

        if (roundRules != null)
        {
            foreach (LevelUpRewardRoundRule rule in roundRules)
            {
                if (rule == null || !rule.MatchesLevel(newLevel))
                    continue;

                if (rule.roundType == LevelUpRewardRoundType.Ability)
                    includeAbilityTraits = true;
            }
        }

        bool includeGeneralTraits = !includeAbilityTraits;
        // AbilityUpgrade traits ONLY appear on milestone rolls (ability level 5/10/etc), never in regular pools
        // Use General traits with requiredAbility + abilityConfigModifiers for ability-enhancing traits in regular rounds
        bool includeAbilityUpgradeTraits = false;

        return new LevelUpRewardRoundContext
        {
            playerLevel = newLevel,
            roundType = DetermineRoundType(newLevel),
            ownedAbilityCount = characterData?.AbilityRewardProgressionList?.Count ?? 0,
            includeGeneralTraits = includeGeneralTraits,
            includeAbilityTraits = includeAbilityTraits,
            includeAbilityUpgradeTraits = includeAbilityUpgradeTraits,
            maxAbilityTraits = maxAbilityTraits,
            levelsPerAbilityUpgrade = levelsPerAbilityUpgrade,
            maxAbilityUpgradesPerAbility = maxAbilityUpgradesPerAbility
        };
    }

    public LevelUpRewardRoundType DetermineRoundType(int newLevel)
    {
        if (roundRules != null)
        {
            foreach (LevelUpRewardRoundRule rule in roundRules)
            {
                if (rule != null && rule.MatchesLevel(newLevel))
                    return rule.roundType;
            }
        }

        return LevelUpRewardRoundType.Standard;
    }

    public AbilityRewardProgression EnsureTrackedAbility(AbilityConfig abilityConfig, int minimumLevel = 1)
    {
        if (characterData == null || abilityConfig == null)
            return null;

        AbilityRewardProgression record = characterData.EnsureAbilityRewardProgression(abilityConfig, minimumLevel);
        if (record != null)
            OnAbilityLevelChanged?.Invoke(record);

        return record;
    }

    public AbilityRewardProgression AddAbilityLevel(AbilityConfig abilityConfig, string contributingNodeID)
    {
        if (characterData == null || abilityConfig == null)
            return null;

        AbilityRewardProgression record = characterData.EnsureAbilityRewardProgression(abilityConfig, 0);
        if (record == null)
            return null;

        if (!string.IsNullOrEmpty(contributingNodeID))
        {
            record.contributingNodeIDs ??= new List<string>();
            if (record.HasContribution(contributingNodeID))
                return record;

            record.contributingNodeIDs.Add(contributingNodeID);
        }

        int previousLevel = record.level;
        record.level = Mathf.Max(0, record.level) + 1;
        
        Debug.Log($"[LevelUpRewardDirector] AddAbilityLevel: '{record.abilityName}' {previousLevel} → {record.level} " +
                  $"(upgrade threshold: {levelsPerAbilityUpgrade}, node: {contributingNodeID})");
        
        OnAbilityLevelChanged?.Invoke(record);
        return record;
    }

    public WeaponRewardProgression AddWeaponLevel(string contributingNodeID)
    {
        if (characterData == null)
            return null;

        WeaponRewardProgression weaponRecord = characterData.WeaponRewardProgression;
        if (weaponRecord == null || string.IsNullOrEmpty(weaponRecord.weaponName))
            return null;

        weaponRecord.contributingNodeIDs ??= new List<string>();
        if (!string.IsNullOrEmpty(contributingNodeID) && weaponRecord.HasContribution(contributingNodeID))
            return weaponRecord;

        if (!string.IsNullOrEmpty(contributingNodeID))
            weaponRecord.contributingNodeIDs.Add(contributingNodeID);

        weaponRecord.level = Mathf.Max(0, weaponRecord.level) + 1;
        OnWeaponLevelChanged?.Invoke(weaponRecord);
        return weaponRecord;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (!IsOwner)
            return;

        LevelUpRewardRoundContext context = BuildRoundContext(newLevel);
        Debug.Log($"[LevelUpRewardDirector] Level {newLevel} requested {context.roundType} round. Owned abilities={context.ownedAbilityCount}");

        OnRewardRoundRequested?.Invoke(context);

        if (!autoDispatchToTraitRoller || traitRoller == null)
            return;

        traitRoller.RollTraitsForLevelUp(context);
    }

    private void HandleTraitUnlocked(string nodeID, TraitData traitData)
    {
        if (characterData == null || traitData == null)
            return;

        RegisterGenericProgressFromTrait(nodeID, traitData);
        RegisterAbilityProgressFromTrait(nodeID, traitData);
        CheckKeystoneTagThresholds();
    }

    /// <summary>
    /// Scans current tag counts and fires a Keystone roll for any newly-crossed multiples of
    /// <see cref="keystoneTagThreshold"/>. Called after each trait is unlocked.
    /// </summary>
    private void CheckKeystoneTagThresholds()
    {
        if (!IsOwner || traitManager == null || keystoneTagThreshold <= 0) return;

        Dictionary<string, int> tagCounts = traitManager.GetTraitTagCollection();
        foreach (var kvp in tagCounts)
        {
            string tag = kvp.Key;
            int count = kvp.Value;
            int thresholdsReached = count / keystoneTagThreshold;

            for (int i = 1; i <= thresholdsReached; i++)
            {
                string key = $"{tag}@{i * keystoneTagThreshold}";
                if (_triggeredKeystoneTags.Contains(key)) continue;

                _triggeredKeystoneTags.Add(key);
                LevelUpRewardRoundContext context = BuildKeystoneRoundContext(tag);
                Debug.Log($"<color=yellow>[LevelUpRewardDirector] KEYSTONE TRIGGERED!</color> " +
                          $"Tag '{tag}' reached {i * keystoneTagThreshold} — queuing Keystone roll");

                OnRewardRoundRequested?.Invoke(context);

                if (autoDispatchToTraitRoller && traitRoller != null)
                    traitRoller.RollTraitsForLevelUp(context);
            }
        }
    }

    /// <summary>
    /// Pre-marks all currently-reached keystone thresholds as already triggered.
    /// Prevents re-firing rolls when traits are loaded from a saved game.
    /// </summary>
    private void PreloadKeystoneTagThresholds()
    {
        _triggeredKeystoneTags.Clear();
        if (traitManager == null || keystoneTagThreshold <= 0) return;

        Dictionary<string, int> tagCounts = traitManager.GetTraitTagCollection();
        foreach (var kvp in tagCounts)
        {
            int thresholdsReached = kvp.Value / keystoneTagThreshold;
            for (int i = 1; i <= thresholdsReached; i++)
                _triggeredKeystoneTags.Add($"{kvp.Key}@{i * keystoneTagThreshold}");
        }
    }

    private LevelUpRewardRoundContext BuildKeystoneRoundContext(string tag)
    {
        return new LevelUpRewardRoundContext
        {
            playerLevel = 0,
            roundType = LevelUpRewardRoundType.Keystone,
            includeKeystoneTraits = true,
            keystoneTag = tag,
            includeGeneralTraits = false,
            includeAbilityTraits = false,
            includeAbilityUpgradeTraits = false,
            maxAbilityTraits = maxAbilityTraits,
            levelsPerAbilityUpgrade = levelsPerAbilityUpgrade,
            maxAbilityUpgradesPerAbility = maxAbilityUpgradesPerAbility
        };
    }

    private void RegisterGenericProgressFromTrait(string nodeID, TraitData traitData)
    {
        // NOTE: AbilityUpgrade traits don't need special handling here.
        // They only come from milestone rolls and apply their effects like any other trait.
    }

    private void HandleWeaponAbilityChanged(AbilityReference _, Ability __)
    {
        SyncTrackedWeapon(resetIfWeaponChanged: true);
    }

    private void RegisterAbilityProgressFromTrait(string nodeID, TraitData traitData)
    {
        // Track abilities unlocked by this trait
        if (traitData.unlockedAbilities != null)
        {
            foreach (TraitAbilityUnlock unlock in traitData.unlockedAbilities)
            {
                if (unlock?.abilityConfig == null)
                    continue;

                AbilityRewardProgression record = characterData.EnsureAbilityRewardProgression(unlock.abilityConfig, 1);
                if (record == null)
                    continue;

                record.contributingNodeIDs ??= new List<string>();
                if (!string.IsNullOrEmpty(nodeID) && !record.HasContribution(nodeID))
                    record.contributingNodeIDs.Add(nodeID);

                Debug.Log($"[LevelUpRewardDirector] RegisterAbilityProgress (unlocked): '{record.abilityName}' now at level {record.level} " +
                          $"(needs level {levelsPerAbilityUpgrade} for upgrade roll)");
                
                OnAbilityLevelChanged?.Invoke(record);
            }
        }

        // Track abilities REQUIRED by this trait - taking a trait that requires an ability levels up that ability
        AbilityConfig requiredAbility = traitData.requiredAbility;
        if (requiredAbility == null && traitData.abilityReplacement != null)
            requiredAbility = traitData.abilityReplacement.requiredAbility;
        
        if (requiredAbility != null && traitData.traitType != TraitType.AbilityUpgrade)
        {
            // This trait requires an ability - increment that ability's level
            AbilityRewardProgression record = characterData.EnsureAbilityRewardProgression(requiredAbility, 0);
            if (record != null)
            {
                record.contributingNodeIDs ??= new List<string>();
                if (!string.IsNullOrEmpty(nodeID) && !record.HasContribution(nodeID))
                {
                    record.contributingNodeIDs.Add(nodeID);
                    int previousLevel = record.level;
                    record.level = Mathf.Max(1, record.level) + 1;
                    
                    Debug.Log($"[LevelUpRewardDirector] RegisterAbilityProgress (required): '{record.abilityName}' {previousLevel} → {record.level} " +
                              $"(trait '{traitData.displayName}' requires this ability, upgrade roll at level {levelsPerAbilityUpgrade})");
                    
                    OnAbilityLevelChanged?.Invoke(record);
                }
            }
        }

        // Track replacement abilities
        if (traitData.abilityReplacement != null && traitData.abilityReplacement.newAbilityConfig != null)
        {
            AbilityRewardProgression record = characterData.EnsureAbilityRewardProgression(traitData.abilityReplacement.newAbilityConfig, 1);
            if (record != null)
            {
                Debug.Log($"[LevelUpRewardDirector] RegisterAbilityProgress (replacement): '{record.abilityName}' now at level {record.level}");
                OnAbilityLevelChanged?.Invoke(record);
            }
        }
    }

    private void SyncTrackedAbilities()
    {
        if (characterData == null)
            return;
        
        // Register the weapon ability (e.g., Snipe from a sniper rifle) so that 
        // AbilityUpgrade traits requiring it can be rolled
        CharacterAbilityManager abilityManager = GetComponent<CharacterAbilityManager>();
        AbilityConfig weaponAbilityConfig = abilityManager?.GetWeaponAbilityRef()?.Config;
        if (weaponAbilityConfig != null)
        {
            characterData.EnsureAbilityRewardProgression(weaponAbilityConfig, 1);
            Debug.Log($"[LevelUpRewardDirector] Tracking weapon ability: {weaponAbilityConfig.abilityName}");
        }
        
        // Register dash ability if present
        AbilityConfig dashAbilityConfig = abilityManager?.GetDashAbilityRef()?.Config;
        if (dashAbilityConfig != null)
        {
            characterData.EnsureAbilityRewardProgression(dashAbilityConfig, 1);
            Debug.Log($"[LevelUpRewardDirector] Tracking dash ability: {dashAbilityConfig.abilityName}");
        }

        if (characterData.abilityLoadout == null)
            return;

        foreach (AbilityReference abilityRef in characterData.abilityLoadout.TraitAbilities)
        {
            if (abilityRef?.Config != null)
                characterData.EnsureAbilityRewardProgression(abilityRef.Config, 1);
        }
    }

    private void SyncTrackedWeapon(bool resetIfWeaponChanged)
    {
        WeaponConfig currentWeapon = characterData?.mainHandWeaponConfig;
        WeaponRewardProgression weaponRecord = characterData?.WeaponRewardProgression;
        if (characterData == null)
            return;

        if (currentWeapon == null)
        {
            characterData.SetWeaponRewardProgression(null, 0);
            return;
        }

        bool isSameWeapon = weaponRecord != null
            && string.Equals(weaponRecord.weaponName, currentWeapon.weaponName, StringComparison.OrdinalIgnoreCase);

        if (!isSameWeapon)
        {
            int carriedLevel = resetIfWeaponChanged ? 0 : Mathf.Max(0, weaponRecord?.level ?? 0);
            List<string> previousContributions = !resetIfWeaponChanged && weaponRecord?.contributingNodeIDs != null
                ? new List<string>(weaponRecord.contributingNodeIDs)
                : new List<string>();

            characterData.SetWeaponRewardProgression(currentWeapon, carriedLevel);
            characterData.WeaponRewardProgression.contributingNodeIDs = previousContributions;

            // Register the weapon's granted ability for AbilityUpgrade trait tracking
            if (currentWeapon.grantedPrimaryAbility != null)
            {
                characterData.EnsureAbilityRewardProgression(currentWeapon.grantedPrimaryAbility, 1);
                Debug.Log($"[LevelUpRewardDirector] Tracking weapon ability: {currentWeapon.grantedPrimaryAbility.abilityName}");
            }

            Debug.Log($"[LevelUpRewardDirector] Tracking weapon progression for {currentWeapon.weaponName} ({currentWeapon.weaponType}) at level {characterData.WeaponRewardProgression.level}");
            OnWeaponLevelChanged?.Invoke(characterData.WeaponRewardProgression);
        }
    }
}

[Serializable]
public class LevelUpRewardRoundRule
{
    public LevelUpRewardRoundType roundType = LevelUpRewardRoundType.Standard;
    public int firstLevel = 1;
    public int repeatInterval = 0;
    public bool matchFirstLevelOnly = false;

    public bool MatchesLevel(int level)
    {
        if (level < firstLevel)
            return false;

        if (matchFirstLevelOnly)
            return level == firstLevel;

        if (repeatInterval <= 0)
            return level == firstLevel;

        return (level - firstLevel) % repeatInterval == 0;
    }
}

public enum LevelUpRewardRoundType
{
    Standard,
    Ability,
    Keystone
}

public class LevelUpRewardRoundContext
{
    public int playerLevel;
    public LevelUpRewardRoundType roundType;
    public int ownedAbilityCount;
    public bool includeGeneralTraits;
    public bool includeAbilityTraits;
    public bool includeAbilityUpgradeTraits;
    public bool includeKeystoneTraits;
    /// <summary>The tag that triggered this Keystone roll. Only Keystones with a matching requiredTag (or no requiredTag) will be included.</summary>
    public string keystoneTag;
    public int maxAbilityTraits;
    public int levelsPerAbilityUpgrade;
    public int maxAbilityUpgradesPerAbility;
}