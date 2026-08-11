using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Specifies which type of traits to roll.
/// </summary>
public enum TraitRollType
{
    General,    // General-purpose buffs and survivability
    Ability,    // Ability traits and upgrades
    Keystone    // Keystone traits (unique, rolled at high levels)
}

/// <summary>
/// Listens for level-up events and rolls 3 random, distinct TraitData options
/// from the player's trait tree for the player to choose from.
/// Attach this to the Player GameObject alongside LevelUpManager.
/// </summary>
public class TraitRoller : MonoBehaviour
{
    private const int ROLL_COUNT = 3;
    private bool subscribedToLevelUps;

    [Header("Trait Pool")]
    [Tooltip("Global list of all TraitData assets available for rolling. " +
             "Use the 'Find All TraitDatas' button on the SO to populate.")]
    [SerializeField] private TraitDataList traitDataList;

    [Header("Roll Settings")]
    [Tooltip("Roll Ability-type traits every N levels (e.g., 5 = roll abilities at levels 5, 10, 15, etc.). " +
             "At other levels, roll Stat-type traits.")]
    [SerializeField] private int abilityRollInterval = 5;

    [Tooltip("Starting at this level, ability roll intervals become Keystone rolls instead. " +
             "(e.g., 20 means levels 20, 25, 30... roll Keystones instead of Abilities)")]
    [SerializeField] private int keystoneStartLevel = 20;

    /// <summary>
    /// Fired when traits are rolled on level-up.
    /// TraitRollerUI listens to this to display the options.
    /// </summary>
    public static event Action<List<TraitData>> OnTraitsRolled;
    public static event Action<List<TraitRollResult>> OnTraitsRolledWithTier;

    private void OnEnable()
    {
        if (GetComponent<LevelUpRewardDirector>() == null)
        {
            LevelUpManager.OnLevelUp += HandleLevelUp;
            subscribedToLevelUps = true;
        }
    }

    private void OnDisable()
    {
        if (subscribedToLevelUps)
        {
            LevelUpManager.OnLevelUp -= HandleLevelUp;
            subscribedToLevelUps = false;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        RollTraitsForLevelUp(newLevel, DetermineLegacyRollType(newLevel));
    }

    public void RollTraitsForLevelUp(LevelUpRewardRoundContext context)
    {
        if (context == null)
            return;

        PlayerController player = GetComponent<PlayerController>();
        if (player == null || !player.IsOwner) return;

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[TraitRoller] No CharacterData found on levelling player");
            return;
        }

        List<TraitRollResult> rolledWithTiers = RollTraitsWithTier(characterData, context);
        PublishRolledTraits(context.playerLevel, rolledWithTiers);
    }

    public void RollTraitsForLevelUp(int newLevel, TraitRollType rollType)
    {
        // Only the local (owning) player should roll traits
        PlayerController player = GetComponent<PlayerController>();
        if (player == null || !player.IsOwner) return;

        string traitTypeLabel = rollType.ToString();

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[TraitRoller] No CharacterData found on levelling player");
            return;
        }

        // Log player's current trait tag collection for synergy weighting
        CharacterTraitManager traitManager = GetComponent<CharacterTraitManager>();
        if (traitManager != null)
        {
            Dictionary<string, int> playerTags = traitManager.GetTraitTagCollection();
            if (playerTags.Count > 0)
            {
                List<string> tagSummary = new List<string>();
                foreach (var kvp in playerTags.OrderByDescending(x => x.Value))
                {
                    tagSummary.Add($"{kvp.Key}({kvp.Value})");
                }
                Debug.Log($"[TraitRoller] Level {newLevel}: Rolling {traitTypeLabel} traits. Player tag synergies: {string.Join(", ", tagSummary)}");
            }
            else
            {
                Debug.Log($"[TraitRoller] Level {newLevel}: Rolling {traitTypeLabel} traits (no synergies yet)");
            }
        }
        else
        {
            Debug.Log($"[TraitRoller] Level {newLevel}: Rolling {traitTypeLabel} traits");
        }

        List<TraitRollResult> rolledWithTiers = RollTraitsWithTier(characterData, newLevel, rollType);
        PublishRolledTraits(newLevel, rolledWithTiers);
    }

    private TraitRollType DetermineLegacyRollType(int newLevel)
    {
        bool isAbilityInterval = (abilityRollInterval > 0 && newLevel % abilityRollInterval == 0) || newLevel == 2;
        if (!isAbilityInterval)
            return TraitRollType.General;

        // Keystones are no longer triggered by player level — they fire when a tag threshold is reached.
        return TraitRollType.Ability;
    }

    /// <summary>
    /// Collect all eligible (not-yet-unlocked, level-appropriate) TraitData from the
    /// character's trait tree, then pick up to ROLL_COUNT distinct random entries.
    /// Uses weighted selection based on trait tag synergies with currently active traits.
    /// </summary>
    public List<TraitRollResult> RollTraitsWithTier(CharacterData characterData, int characterLevel, TraitRollType rollType = TraitRollType.General)
    {
        List<TraitData> pool = BuildEligiblePool(characterData, characterLevel, rollType);

        // Get player's trait tag collection for weighting
        CharacterTraitManager traitManager = GetComponent<CharacterTraitManager>();
        Dictionary<string, int> playerTagCounts = traitManager != null ? traitManager.GetTraitTagCollection() : new Dictionary<string, int>();

        List<TraitData> picked = PickRandomWeighted(pool, ROLL_COUNT, playerTagCounts);
        List<TraitRollResult> results = new List<TraitRollResult>();
        foreach (var trait in picked)
        {
            ItemTier rolledTier = trait.UsesTierScaling && trait.tierConfig != null ? TierScaler.RollTier(trait.tierConfig) : ItemTier.I;
            results.Add(new TraitRollResult
            {
                traitData = trait,
                rolledTier = rolledTier
            });
        }
        return results;
    }

    public List<TraitRollResult> RollTraitsWithTier(CharacterData characterData, LevelUpRewardRoundContext context)
    {
        List<TraitData> pool = BuildEligiblePool(characterData, context);

        CharacterTraitManager traitManager = GetComponent<CharacterTraitManager>();
        Dictionary<string, int> playerTagCounts = traitManager != null ? traitManager.GetTraitTagCollection() : new Dictionary<string, int>();

        List<TraitData> picked = PickRandomWeighted(pool, ROLL_COUNT, playerTagCounts);
        List<TraitRollResult> results = new List<TraitRollResult>();
        foreach (var trait in picked)
        {
            ItemTier rolledTier = trait.UsesTierScaling && trait.tierConfig != null ? TierScaler.RollTier(trait.tierConfig) : ItemTier.I;
            results.Add(new TraitRollResult
            {
                traitData = trait,
                rolledTier = rolledTier
            });
        }

        return results;
    }

    public List<TraitData> RollTraits(CharacterData characterData, int characterLevel, TraitRollType rollType = TraitRollType.General)
    {
        List<TraitData> pool = BuildEligiblePool(characterData, characterLevel, rollType);
        return PickRandom(pool, ROLL_COUNT);
    }

    /// <summary>
    /// Roll ability upgrade traits specifically for an ability that just reached max level.
    /// Only includes AbilityUpgrade traits that target the specified ability and have requiredAbilityLevel matching.
    /// </summary>
    public void RollAbilityUpgradeTraitsFor(AbilityConfig abilityConfig, int abilityLevel)
    {
        if (abilityConfig == null) return;

        PlayerController player = GetComponent<PlayerController>();
        if (player == null || !player.IsOwner) return;

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null) return;

        List<TraitData> pool = BuildAbilityUpgradePool(characterData, abilityConfig, abilityLevel);
        if (pool.Count == 0)
        {
            Debug.Log($"[TraitRoller] No ability upgrade traits available for {abilityConfig.abilityName} at level {abilityLevel}");
            return;
        }

        CharacterTraitManager traitManager = GetComponent<CharacterTraitManager>();
        Dictionary<string, int> playerTagCounts = traitManager != null ? traitManager.GetTraitTagCollection() : new Dictionary<string, int>();

        List<TraitData> picked = PickRandomWeighted(pool, ROLL_COUNT, playerTagCounts);
        List<TraitRollResult> results = new List<TraitRollResult>();
        foreach (var trait in picked)
        {
            ItemTier rolledTier = trait.UsesTierScaling && trait.tierConfig != null ? TierScaler.RollTier(trait.tierConfig) : ItemTier.I;
            results.Add(new TraitRollResult
            {
                traitData = trait,
                rolledTier = rolledTier
            });
        }

        Debug.Log($"[TraitRoller] Rolling {results.Count} ability upgrade traits for {abilityConfig.abilityName} (level {abilityLevel})");
        PublishRolledTraits(0, results); // Level 0 indicates ability upgrade round
    }

    /// <summary>
    /// Build a pool of AbilityUpgrade traits that target a specific ability at the specified level.
    /// </summary>
    private List<TraitData> BuildAbilityUpgradePool(CharacterData characterData, AbilityConfig targetAbility, int abilityLevel)
    {
        List<TraitData> pool = new List<TraitData>();

        if (traitDataList == null || traitDataList.traitGroups == null || traitDataList.traitGroups.Count == 0)
        {
            Debug.LogWarning("[TraitRoller] TraitDataList is not assigned or empty!");
            return pool;
        }

        HashSet<string> unlocked = new HashSet<string>(characterData.unlockedNodeIDs ?? new List<string>());
        string targetAbilityID = targetAbility?.name;
        string targetAbilityName = targetAbility?.abilityName;

        Debug.Log($"[TraitRoller] BuildAbilityUpgradePool: Looking for AbilityUpgrade traits targeting '{targetAbilityName}' (ID: {targetAbilityID}) at level {abilityLevel}");

        foreach (TraitData trait in traitDataList.AllTraits)
        {
            if (trait == null) continue;

            // Only AbilityUpgrade traits
            if (trait.traitType != TraitType.AbilityUpgrade) continue;

            // Skip already unlocked unique traits
            if (trait.IsUniqueTraitType && !string.IsNullOrEmpty(trait.traitID) && unlocked.Contains(trait.traitID))
            {
                Debug.Log($"[TraitRoller]   Skipping '{trait.displayName}' - already unlocked");
                continue;
            }

            // Skip if a mutually exclusive trait is already taken
            if (trait.mutuallyExclusiveWith != null)
            {
                bool blocked = false;
                foreach (TraitData exclusive in trait.mutuallyExclusiveWith)
                {
                    if (exclusive != null && !string.IsNullOrEmpty(exclusive.traitID) && unlocked.Contains(exclusive.traitID))
                    {
                        Debug.Log($"[TraitRoller]   Skipping '{trait.displayName}' - mutually exclusive with already-taken '{exclusive.displayName}'");
                        blocked = true;
                        break;
                    }
                }
                if (blocked) continue;
            }

            if (!MeetsRequiredTraitPrerequisites(trait, unlocked))
                continue;

            // Check if this trait targets the specified ability (compare by asset name, not reference)
            AbilityConfig required = trait.requiredAbility;
            if (required == null && trait.abilityReplacement != null)
                required = trait.abilityReplacement.requiredAbility;

            if (required == null)
            {
                Debug.Log($"[TraitRoller]   Skipping '{trait.displayName}' - no required ability specified");
                continue;
            }

            // Compare by asset name (ID) since references may differ
            bool matchesAbility = required.name == targetAbilityID ||
                                  required.abilityName == targetAbilityName;

            if (!matchesAbility)
            {
                Debug.Log($"[TraitRoller]   Skipping '{trait.displayName}' - requires '{required.abilityName}' (ID: {required.name}), not '{targetAbilityName}'");
                continue;
            }

            // Check ability level requirement
            if (trait.requiredAbilityLevel > 0 && abilityLevel < trait.requiredAbilityLevel)
            {
                Debug.Log($"[TraitRoller]   Skipping '{trait.displayName}' - requires ability level {trait.requiredAbilityLevel}, current is {abilityLevel}");
                continue;
            }

            Debug.Log($"[TraitRoller]   ELIGIBLE: '{trait.displayName}' (requires {required.abilityName} at level {trait.requiredAbilityLevel})");
            pool.Add(trait);
        }

        Debug.Log($"[TraitRoller] Built pool of {pool.Count} ability upgrade traits for {targetAbility.abilityName} at level {abilityLevel}");
        return pool;
    }

    /// <summary>
    /// Build a pool of TraitData the player hasn't unlocked yet and meets the level requirement.
    /// Uses the global TraitDataList instead of the character's trait tree.
    /// </summary>
    /// <param name="rollType">Which type of traits to include in the pool.</param>
    private List<TraitData> BuildEligiblePool(CharacterData characterData, int characterLevel, TraitRollType rollType = TraitRollType.General)
    {
        List<TraitData> pool = new List<TraitData>();
        string currentWeaponType = characterData?.WeaponRewardProgression?.weaponType;

        if (traitDataList == null || traitDataList.traitGroups == null || traitDataList.traitGroups.Count == 0)
        {
            Debug.LogWarning("[TraitRoller] TraitDataList is not assigned or empty! Assign a TraitDataList SO in the Inspector.");
            return pool;
        }

        HashSet<string> unlocked = new HashSet<string>(characterData.unlockedNodeIDs ?? new List<string>());

        foreach (TraitData trait in traitDataList.AllTraits)
        {
            if (trait == null) continue;
            List<string> weaponTags = trait.GetWeaponTags();
            bool isWeaponSpecificTrait = weaponTags != null && weaponTags.Count > 0;

            if (trait.IsUniqueTraitType)
            {
                if (!string.IsNullOrEmpty(trait.traitID) && unlocked.Contains(trait.traitID)) continue;
            }

            // Skip if a mutually exclusive trait is already taken
            if (trait.mutuallyExclusiveWith != null)
            {
                bool blocked = false;
                foreach (TraitData exclusive in trait.mutuallyExclusiveWith)
                {
                    if (exclusive != null && !string.IsNullOrEmpty(exclusive.traitID) && unlocked.Contains(exclusive.traitID))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) continue;
            }

            if (!MeetsRequiredTraitPrerequisites(trait, unlocked))
                continue;

            // Filter by trait type based on roll type
            switch (rollType)
            {
                case TraitRollType.Ability:
                    if (!trait.IsAbilityTraitType) continue;
                    break;
                case TraitRollType.Keystone:
                    if (trait.traitType != TraitType.Keystone) continue;
                    break;
                case TraitRollType.General:
                default:
                    if (trait.traitType != TraitType.General) continue;
                    if (isWeaponSpecificTrait) continue;
                    break;
            }

            pool.Add(trait);
        }

        Debug.Log($"[TraitRoller] Built pool of {pool.Count} eligible {rollType} traits");

        return pool;
    }

    private List<TraitData> BuildEligiblePool(CharacterData characterData, LevelUpRewardRoundContext context)
    {
        List<TraitData> pool = new List<TraitData>();
        if (context == null)
            return pool;

        if (traitDataList == null || traitDataList.traitGroups == null || traitDataList.traitGroups.Count == 0)
        {
            Debug.LogWarning("[TraitRoller] TraitDataList is not assigned or empty! Assign a TraitDataList SO in the Inspector.");
            return pool;
        }

        HashSet<string> unlocked = new HashSet<string>(characterData.unlockedNodeIDs ?? new List<string>());
        foreach (TraitData trait in traitDataList.AllTraits)
        {
            if (trait == null)
                continue;

            if (trait.IsUniqueTraitType && !string.IsNullOrEmpty(trait.traitID) && unlocked.Contains(trait.traitID))
                continue;

            // Skip if a mutually exclusive trait is already taken
            if (trait.mutuallyExclusiveWith != null)
            {
                bool blocked = false;
                foreach (TraitData exclusive in trait.mutuallyExclusiveWith)
                {
                    if (exclusive != null && !string.IsNullOrEmpty(exclusive.traitID) && unlocked.Contains(exclusive.traitID))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) continue;
            }

            if (!MeetsRequiredTraitPrerequisites(trait, unlocked))
                continue;

            if (ShouldIncludeTraitForContext(trait, context, characterData))
                pool.Add(trait);
        }

        Debug.Log($"[TraitRoller] Built pool of {pool.Count} eligible traits for level {context.playerLevel} ({context.roundType})");
        return pool;
    }

    private static bool MeetsRequiredTraitPrerequisites(TraitData trait, HashSet<string> unlocked)
    {
        if (trait == null || trait.requiredTraits == null || trait.requiredTraits.Count == 0)
            return true;

        bool hasAnyValidRequirement = false;
        foreach (TraitData req in trait.requiredTraits)
        {
            if (req == null || string.IsNullOrEmpty(req.traitID))
                continue;

            hasAnyValidRequirement = true;
            if (!unlocked.Contains(req.traitID))
                return false;
        }

        // If requirements are configured but all entries are null/invalid, don't block the roll.
        return !hasAnyValidRequirement;
    }

    private bool ShouldIncludeTraitForContext(TraitData trait, LevelUpRewardRoundContext context, CharacterData characterData = null)
    {
        switch (trait.traitType)
        {
            case TraitType.General:
                // General traits with requiredAbility only appear if player owns that ability
                if (!context.includeGeneralTraits)
                    return false;
                return MeetsAbilityRequirement(trait, characterData);
            case TraitType.Ability:
                return context.includeAbilityTraits && context.ownedAbilityCount < context.maxAbilityTraits;
            case TraitType.AbilityUpgrade:
                return context.includeAbilityUpgradeTraits && MeetsAbilityRequirement(trait, characterData);
            case TraitType.Keystone:
                if (!context.includeKeystoneTraits) return false;
                // A keystone with a requiredTag only appears when that specific tag triggered the roll
                if (!string.IsNullOrEmpty(trait.requiredTag) && !string.IsNullOrEmpty(context.keystoneTag))
                    return string.Equals(trait.requiredTag, context.keystoneTag, StringComparison.OrdinalIgnoreCase);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Check if the player meets the ability requirement for a trait.
    /// Returns true if no required ability is specified, or if the player owns it and meets the level requirement.
    /// </summary>
    private bool MeetsAbilityRequirement(TraitData trait, CharacterData characterData)
    {
        // Determine the required ability - check trait.requiredAbility first, then fall back to abilityReplacement
        AbilityConfig required = trait.requiredAbility;
        if (required == null && trait.abilityReplacement != null)
            required = trait.abilityReplacement.requiredAbility;

        // If no required ability specified, allow the trait
        if (required == null)
            return true;

        if (characterData == null)
            return false;

        // Check the CharacterAbilityManager's live runtime lists first (handles abilities
        // granted mid-run via traits, which aren't written back to the loadout SO).
        CharacterAbilityManager abilityManager = GetComponent<CharacterAbilityManager>();
        if (abilityManager != null)
        {
            if (abilityManager.GetWeaponAbilityRef()?.Config == required)
                return MeetsAbilityLevelRequirement(trait, characterData, required);

            if (abilityManager.GetDashAbilityRef()?.Config == required)
                return MeetsAbilityLevelRequirement(trait, characterData, required);

            foreach (var abilityRef in abilityManager.GetActiveTraitAbilityRefs())
            {
                if (abilityRef?.Config == required)
                    return MeetsAbilityLevelRequirement(trait, characterData, required);
            }

            foreach (var abilityRef in abilityManager.GetPassiveTraitAbilityRefs())
            {
                if (abilityRef?.Config == required)

                    return MeetsAbilityLevelRequirement(trait, characterData, required);
            }

            return false;
        }

        // Fallback: check characterData.abilityLoadout (only reflects initial state, not mid-run grants)
        var loadout = characterData.abilityLoadout;
        if (loadout == null) return false;

        if (loadout.WeaponAbility?.Config == required ||
            loadout.DashAbility?.Config == required)
            return MeetsAbilityLevelRequirement(trait, characterData, required);

        foreach (var abilityRef in loadout.TraitAbilities)
        {
            if (abilityRef?.Config == required)
                return MeetsAbilityLevelRequirement(trait, characterData, required);
        }

        return false;
    }

    private bool MeetsAbilityLevelRequirement(TraitData trait, CharacterData characterData, AbilityConfig required)
    {
        int requiredLevel = trait.requiredAbilityLevel;
        if (requiredLevel <= 0)
            return true;

        AbilityRewardProgression progression = characterData.FindAbilityRewardProgression(required);
        if (progression == null)
            return false;

        return progression.level >= requiredLevel;
    }

    private void PublishRolledTraits(int level, List<TraitRollResult> rolledWithTiers)
    {
        if (rolledWithTiers == null || rolledWithTiers.Count == 0)
        {
            Debug.LogWarning($"[TraitRoller] Level {level}: No eligible traits to roll!");
            return;
        }

        CharacterTraitManager traitManager = GetComponent<CharacterTraitManager>();
        Debug.Log("[TraitRoller] === Final Rolled Traits (with synergy breakdown) ===");
        for (int i = 0; i < rolledWithTiers.Count; i++)
        {
            TraitRollResult t = rolledWithTiers[i];
            Dictionary<string, int> playerTagCounts = traitManager != null ? traitManager.GetTraitTagCollection() : new Dictionary<string, int>();
            float weight = CalculateTraitWeight(t.traitData, playerTagCounts, logDetails: true);
            string tagsList = string.Join(", ", t.traitData.GetAllTags().Where(tag => !string.IsNullOrEmpty(tag)));
            string modSummary = BuildModifierSummary(t.traitData, t.rolledTier);
            Debug.Log($"[TraitRoller]  Roll {i + 1}: [{t.traitData.traitType}] \"{t.traitData.displayName}\" (ID: {t.traitData.traitID}, Tier: {t.rolledTier}, Weight: {weight:F1}, Tags: {tagsList}) — {t.traitData.description}{modSummary}");
        }

        List<TraitData> traitDataOnly = new List<TraitData>();
        foreach (var roll in rolledWithTiers)
        {
            TraitData tieredCopy = Instantiate(roll.traitData);
            tieredCopy.tierLevel = roll.rolledTier;
            traitDataOnly.Add(tieredCopy);
        }

        OnTraitsRolled?.Invoke(traitDataOnly);
        OnTraitsRolledWithTier?.Invoke(rolledWithTiers);
    }

    private bool MatchesWeaponRound(List<string> traitWeaponTags, string currentWeaponType)
    {
        if (traitWeaponTags == null || traitWeaponTags.Count == 0)
            return false;

        if (string.IsNullOrEmpty(currentWeaponType))
            return false;

        foreach (string tag in traitWeaponTags)
        {
            if (string.IsNullOrEmpty(tag))
                continue;

            if (string.Equals(tag, currentWeaponType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tag, "Any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Fisher-Yates partial shuffle to pick up to 'count' distinct items from the pool.
    /// </summary>
    private List<TraitData> PickRandom(List<TraitData> pool, int count)
    {
        List<TraitData> result = new List<TraitData>();
        if (pool.Count == 0) return result;

        // Work on a copy so we don't mutate the caller's list
        List<TraitData> copy = new List<TraitData>(pool);
        int picks = Mathf.Min(count, copy.Count);

        for (int i = 0; i < picks; i++)
        {
            int rand = UnityEngine.Random.Range(i, copy.Count);
            // Swap
            (copy[i], copy[rand]) = (copy[rand], copy[i]);
            result.Add(copy[i]);
        }

        return result;
    }

    /// <summary>
    /// Pick random traits with weighted selection based on tag synergies.
    /// Traits with tags matching the player's active trait tags get higher weight.
    /// Base weight: 1.0
    /// Per matching tag: +0.5 weight per count of that tag in player's collection
    /// </summary>
    private List<TraitData> PickRandomWeighted(List<TraitData> pool, int count, Dictionary<string, int> playerTagCounts)
    {
        List<TraitData> result = new List<TraitData>();
        if (pool.Count == 0) return result;

        // If no tags collected yet, use regular random selection
        if (playerTagCounts == null || playerTagCounts.Count == 0)
        {
            return PickRandom(pool, count);
        }

        // Calculate weights for each trait based on tag synergies
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        Debug.Log($"[TraitRoller] === Weighted Selection Pool (showing top 10 by weight) ===");
        List<KeyValuePair<TraitData, float>> weightedPool = new List<KeyValuePair<TraitData, float>>();

        foreach (TraitData trait in pool)
        {
            float weight = CalculateTraitWeight(trait, playerTagCounts);
            weights.Add(weight);
            totalWeight += weight;
            weightedPool.Add(new KeyValuePair<TraitData, float>(trait, weight));
        }

        // Show top 10 weighted traits for visibility
        var topWeighted = weightedPool.OrderByDescending(x => x.Value).Take(4);
        foreach (var kvp in topWeighted)
        {
            string tags = string.Join(", ", kvp.Key.GetAllTags().Where(t => !string.IsNullOrEmpty(t)));
            Debug.Log($"[TraitRoller]   {kvp.Key.displayName}: Weight={kvp.Value:F1} (Tags: {tags})");
        }
        Debug.Log($"[TraitRoller] Total pool size: {pool.Count} traits, Total weight: {totalWeight:F1}");

        // Pick 'count' distinct traits using weighted random selection
        List<TraitData> poolCopy = new List<TraitData>(pool);
        List<float> weightsCopy = new List<float>(weights);
        int picks = Mathf.Min(count, poolCopy.Count);

        Debug.Log($"[TraitRoller] === Selecting {picks} traits via weighted random ===");
        for (int i = 0; i < picks; i++)
        {
            for (int j = 0; j < poolCopy.Count; j++)
            {
                weightsCopy[j] = CalculateTraitWeight(poolCopy[j], playerTagCounts, result);
            }

            // Recalculate total weight (it changes as we remove items)
            float currentTotalWeight = 0f;
            foreach (float w in weightsCopy)
            {
                currentTotalWeight += w;
            }

            // Pick a random value between 0 and total weight
            float randomValue = UnityEngine.Random.Range(0f, currentTotalWeight);

            // Find which trait this value corresponds to
            float cumulative = 0f;
            int selectedIndex = 0;
            for (int j = 0; j < weightsCopy.Count; j++)
            {
                cumulative += weightsCopy[j];
                if (randomValue <= cumulative)
                {
                    selectedIndex = j;
                    break;
                }
            }

            // Add selected trait to result and remove from pool
            TraitData selected = poolCopy[selectedIndex];
            float selectedWeight = weightsCopy[selectedIndex];
            Debug.Log($"[TraitRoller]   Pick {i + 1}: Rolled {randomValue:F2}/{currentTotalWeight:F2} → Selected '{selected.displayName}' (weight: {selectedWeight:F1})");

            result.Add(selected);
            poolCopy.RemoveAt(selectedIndex);
            weightsCopy.RemoveAt(selectedIndex);
        }

        return result;
    }

    /// <summary>
    /// Calculate weight for a trait based on tag synergies with player's active traits.
    /// Base weight: 1.0
    /// Matching tags use diminishing returns so established synergies matter without dominating rolls.
    /// Traits that overlap heavily with traits already picked in this same roll are penalized to improve variety.
    /// </summary>
    private float CalculateTraitWeight(TraitData trait, Dictionary<string, int> playerTagCounts, List<TraitData> alreadySelectedTraits = null, bool logDetails = false)
    {
        const float BASE_WEIGHT = 0.5f;
        const float SYNERGY_WEIGHT_SCALE = 0.50f;
        const float MAX_TOTAL_SYNERGY_BONUS = 10f;
        const float SAME_TAG_ROLL_PENALTY = 0f;
        const float MIN_WEIGHT = 0.1f;

        float weight = BASE_WEIGHT;
        List<string> matchingTags = new List<string>();

        // Get all tags from this trait
        List<string> traitTags = trait.GetAllTags()
            .Where(tag => !string.IsNullOrEmpty(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        float totalSynergyBonus = 0f;

        foreach (string tag in traitTags)
        {
            if (playerTagCounts.ContainsKey(tag))
            {
                int tagCount = playerTagCounts[tag];
                float bonus = Mathf.Log(tagCount + 1f, 2f) * SYNERGY_WEIGHT_SCALE;
                totalSynergyBonus += bonus;
                matchingTags.Add($"{tag}(x{tagCount}=+{bonus:F1})");
            }
        }

        weight += Mathf.Min(totalSynergyBonus, MAX_TOTAL_SYNERGY_BONUS);

        int overlappingSelectedTagCount = 0;
        if (alreadySelectedTraits != null && alreadySelectedTraits.Count > 0 && traitTags.Count > 0)
        {
            HashSet<string> selectedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (TraitData selectedTrait in alreadySelectedTraits)
            {
                if (selectedTrait == null)
                    continue;

                foreach (string selectedTag in selectedTrait.GetAllTags())
                {
                    if (!string.IsNullOrEmpty(selectedTag))
                        selectedTags.Add(selectedTag);
                }
            }

            foreach (string traitTag in traitTags)
            {
                if (selectedTags.Contains(traitTag))
                    overlappingSelectedTagCount++;
            }
        }

        if (overlappingSelectedTagCount > 0)
        {
            weight /= 1f + (overlappingSelectedTagCount * SAME_TAG_ROLL_PENALTY);
        }

        weight = Mathf.Max(MIN_WEIGHT, weight);

        // Log detailed weight calculation only when requested (e.g., for final rolled traits)
        if (logDetails && (matchingTags.Count > 0 || overlappingSelectedTagCount > 0))
        {
            string synergySummary = matchingTags.Count > 0 ? string.Join(", ", matchingTags) : "none";
            string overlapSummary = overlappingSelectedTagCount > 0 ? $", SameRollPenalty(x{overlappingSelectedTagCount})" : string.Empty;
            Debug.Log($"[TraitRoller]     Weight calculation for '{trait.displayName}': Base={BASE_WEIGHT:F1} + Synergies[{synergySummary}]{overlapSummary} = {weight:F1}");
        }

        return weight;
    }

    /// <summary>
    /// Build a short summary of stat modifiers for the debug log.
    /// Now includes tier scaling information and weapon type context.
    /// </summary>
    private string BuildModifierSummary(TraitData trait, ItemTier tier = ItemTier.I)
    {
        List<string> parts = new List<string>();
        float tierMultiplier = TierScaler.GetMultiplier(tier, trait.tierConfig);

        // Check if trait targets a specific ability for context labeling
        string abilityName = trait.requiredAbility?.abilityName;
        string abilitySuffix = !string.IsNullOrEmpty(abilityName) ? $" for {abilityName}" : "";

        // Stat modifiers
        if (trait.statModifiers != null)
        {
            foreach (var mod in trait.statModifiers)
            {
                float scaledValue = mod.value * tierMultiplier;
                string sign = scaledValue >= 0 ? "+" : "";
                string pct = mod.modifierType == TraitModifierType.Percentage ? "%" : "";

                if (tier != ItemTier.I)
                    parts.Add($"{sign}{scaledValue:F0}{pct} {mod.statID}{abilitySuffix} (Tier {tier})");
                else
                    parts.Add($"{sign}{mod.value}{pct} {mod.statID}{abilitySuffix}");
            }
        }

        // Ammo modifiers (ability upgrade traits)
        if (trait.weaponAmmoModifier != null)
        {
            var ammo = trait.weaponAmmoModifier;
            string abilityLabel = !string.IsNullOrEmpty(abilityName) ? $" for {abilityName}" : "";

            if (ammo.magazineSizeBonus != 0)
            {
                int scaledMag = Mathf.RoundToInt(ammo.magazineSizeBonus * tierMultiplier);
                string sign = scaledMag >= 0 ? "+" : "";
                if (tier != ItemTier.I)
                    parts.Add($"Grants {sign}{scaledMag} ammo{abilityLabel} (Tier {tier})");
                else
                    parts.Add($"Grants {sign}{ammo.magazineSizeBonus} ammo{abilityLabel}");
            }
            if (ammo.reloadTimeDelta != 0)
            {
                float scaledReload = ammo.reloadTimeDelta * tierMultiplier;
                string sign = scaledReload >= 0 ? "+" : "";
                if (tier != ItemTier.I)
                    parts.Add($"{sign}{scaledReload:F2}s reload{abilityLabel} (Tier {tier})");
                else
                    parts.Add($"{sign}{ammo.reloadTimeDelta:F2}s reload{abilityLabel}");
            }
        }

        if (parts.Count == 0) return "";
        return $" | Mods: [{string.Join(", ", parts)}]";
    }


    [System.Serializable]
    public class TraitRollResult
    {
        public TraitData traitData;
        public ItemTier rolledTier;

        public float GetScaledValue(float baseValue)
        {
            return TierScaler.ScaleValue(baseValue, rolledTier);
        }
    }
}
