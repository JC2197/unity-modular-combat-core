using System;
using System.Collections.Generic;
using JoeConticello.ModularCombatCore;
using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponAdvancementClass
{
    MainHand,
    OffHand,
    TwoHanded
}

[Serializable]
public class WeaponProgressionEntry
{
    public WeaponAdvancementClass weaponClass = WeaponAdvancementClass.MainHand;
    [Min(1)] public int advancementLevel = 1;
    public TierScalingConfig tierScalingConfig;
    public bool overrideBaseDamageRange = true;
    public int baseDamageMin = 1;
    public int baseDamageMax = 3;
}

[Serializable]
public class ArmorProgressionEntry
{
    public ArmorClass armorClass = ArmorClass.Light;
    public ArmorSlot armorSlot = ArmorSlot.Chest;
    [Min(1)] public int advancementLevel = 1;
    public TierScalingConfig tierScalingConfig;
    public bool overrideBaseStats = true;
    public List<StatModifierRange> baseStatRanges = new List<StatModifierRange>();

    [FormerlySerializedAs("baseStats")]
    [SerializeField, HideInInspector]
    private List<StatModifier> legacyBaseStats = new List<StatModifier>();

    [FormerlySerializedAs("overrideBaseDefensiveStats")]
    [SerializeField, HideInInspector]
    private bool legacyOverrideBaseDefensiveStats = true;

    [FormerlySerializedAs("baseDefensiveStats")]
    [SerializeField, HideInInspector]
    private List<DefensiveStatRange> legacyBaseDefensiveStats = new List<DefensiveStatRange>();

    public bool ShouldOverrideBaseStats()
    {
        return overrideBaseStats || legacyOverrideBaseDefensiveStats;
    }

    public List<DefensiveStatRange> GetLegacyDefensiveRanges()
    {
        return legacyBaseDefensiveStats;
    }

    public List<StatModifier> GetLegacyBaseStats()
    {
        return legacyBaseStats;
    }
}

[CreateAssetMenu(fileName = "GearProgressionDatabase", menuName = "Items/Gear Progression Database")]
public class GearProgressionDatabase : ScriptableObject
{
    public List<WeaponProgressionEntry> weaponEntries = new List<WeaponProgressionEntry>();
    public List<ArmorProgressionEntry> armorEntries = new List<ArmorProgressionEntry>();

    private static GearProgressionDatabase instance;
    public static GearProgressionDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GearProgressionDatabase>("GearProgressionDatabase");
            }
            return instance;
        }
    }

    public bool TryGetWeaponEntry(WeaponConfig config, out WeaponProgressionEntry entry)
    {
        entry = null;
        if (config == null)
            return false;

        WeaponAdvancementClass key = ResolveWeaponClass(config);
        int targetAdvancement = Mathf.Max(1, config.advancementLevel);

        WeaponProgressionEntry fallback = null;
        for (int i = 0; i < weaponEntries.Count; i++)
        {
            WeaponProgressionEntry candidate = weaponEntries[i];
            if (candidate == null || candidate.weaponClass != key)
                continue;

            if (candidate.advancementLevel == targetAdvancement)
            {
                entry = candidate;
                return true;
            }

            if (fallback == null)
                fallback = candidate;
        }

        if (fallback != null)
        {
            entry = fallback;
            return true;
        }

        return false;
    }

    public bool TryGetArmorEntry(ArmorConfig config, out ArmorProgressionEntry entry)
    {
        entry = null;
        if (config == null)
            return false;

        int targetAdvancement = Mathf.Max(1, config.advancementLevel);

        ArmorProgressionEntry fallback = null;

        for (int i = 0; i < armorEntries.Count; i++)
        {
            ArmorProgressionEntry candidate = armorEntries[i];
            if (candidate == null || candidate.armorClass != config.armorClass || candidate.armorSlot != config.armorSlot)
                continue;

            if (candidate.advancementLevel == targetAdvancement)
            {
                entry = candidate;
                return true;
            }

            if (fallback == null)
                fallback = candidate;
        }

        if (fallback != null)
        {
            entry = fallback;
            return true;
        }

        return false;
    }

    public static WeaponAdvancementClass ResolveWeaponClass(WeaponConfig config)
    {
        if (config == null)
            return WeaponAdvancementClass.MainHand;

        if (config.is2Handed)
            return WeaponAdvancementClass.TwoHanded;

        if (config.isOffhand && !config.isMainHand)
            return WeaponAdvancementClass.OffHand;

        return WeaponAdvancementClass.MainHand;
    }
}

public static class GearAdvancementUtility
{
    public static ItemTier AdvancementLevelToTier(int advancementLevel)
    {
        int clamped = Mathf.Clamp(advancementLevel, 1, 6);
        return (ItemTier)clamped;
    }

    public static ItemTier RollGearTier(int advancementLevel, TierScalingConfig tierScalingConfig)
    {
        ItemTier rolledTier = TierScaler.RollTier(tierScalingConfig);
        ItemTier minimumTier = AdvancementLevelToTier(advancementLevel);
        if ((int)rolledTier < (int)minimumTier)
            rolledTier = minimumTier;
        return rolledTier;
    }

    public static void ResolveWeaponSettings(
        WeaponConfig config,
        out int advancementLevel,
        out TierScalingConfig tierScalingConfig,
        out int baseDamageMin,
        out int baseDamageMax)
    {
        advancementLevel = config != null ? Mathf.Max(1, config.advancementLevel) : 1;
        tierScalingConfig = config != null ? config.tierScalingConfig : null;
        baseDamageMin = config != null ? config.weaponDamageMin : 1;
        baseDamageMax = config != null ? config.weaponDamageMax : 1;

        GearProgressionDatabase db = GearProgressionDatabase.Instance;
        if (db != null && db.TryGetWeaponEntry(config, out WeaponProgressionEntry entry))
        {
            advancementLevel = Mathf.Max(1, entry.advancementLevel);
            if (entry.tierScalingConfig != null)
                tierScalingConfig = entry.tierScalingConfig;

            if (entry.overrideBaseDamageRange)
            {
                baseDamageMin = entry.baseDamageMin;
                baseDamageMax = entry.baseDamageMax;
            }
        }

        if (baseDamageMax < baseDamageMin)
            baseDamageMax = baseDamageMin;
    }

    public static void ResolveArmorSettings(
        ArmorConfig config,
        out int advancementLevel,
        out TierScalingConfig tierScalingConfig,
        out List<StatModifierRange> baseStatRanges,
        out List<StatModifier> legacyBaseStats,
        out List<DefensiveStatRange> legacyDefensiveRanges)
    {
        advancementLevel = config != null ? Mathf.Max(1, config.advancementLevel) : 1;
        tierScalingConfig = config != null ? config.tierScalingConfig : null;
        baseStatRanges = config != null
            ? CloneStatModifierRanges(config.baseStatRanges)
            : new List<StatModifierRange>();
        legacyBaseStats = config != null
            ? CloneStatModifiers(config.GetLegacyBaseStats())
            : new List<StatModifier>();
        legacyDefensiveRanges = new List<DefensiveStatRange>();

        GearProgressionDatabase db = GearProgressionDatabase.Instance;
        if (db != null && db.TryGetArmorEntry(config, out ArmorProgressionEntry entry))
        {
            advancementLevel = Mathf.Max(1, entry.advancementLevel);
            if (entry.tierScalingConfig != null)
                tierScalingConfig = entry.tierScalingConfig;

            if (entry.ShouldOverrideBaseStats())
            {
                baseStatRanges = CloneStatModifierRanges(entry.baseStatRanges);
                legacyBaseStats = CloneStatModifiers(entry.GetLegacyBaseStats());
                legacyDefensiveRanges = CloneDefensiveRanges(entry.GetLegacyDefensiveRanges());
            }
        }
    }

    public static List<StatModifier> RollBaseStats(List<StatModifierRange> ranges, List<StatModifier> legacyValues)
    {
        List<StatModifier> result = new List<StatModifier>();
        if (ranges != null)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                StatModifierRange range = ranges[i];
                if (range == null || string.IsNullOrWhiteSpace(range.statID))
                    continue;

                float rolledValue = range.RollValue();
                if (Mathf.Approximately(rolledValue, 0f))
                    continue;

                result.Add(new StatModifier
                {
                    statID = range.statID,
                    value = rolledValue,
                    modifierType = range.modifierType
                });
            }

            if (result.Count > 0)
                return result;
        }

        if (legacyValues == null)
            return result;

        for (int i = 0; i < legacyValues.Count; i++)
        {
            StatModifier statValue = legacyValues[i];
            if (statValue == null || string.IsNullOrWhiteSpace(statValue.statID))
                continue;

            if (Mathf.Approximately(statValue.value, 0f))
                continue;

            result.Add(new StatModifier
            {
                statID = statValue.statID,
                value = statValue.value,
                modifierType = statValue.modifierType
            });
        }

        return result;
    }

    public static List<StatModifier> RollLegacyDefensiveStats(List<DefensiveStatRange> ranges)
    {
        List<StatModifier> result = new List<StatModifier>();
        if (ranges == null)
            return result;

        for (int i = 0; i < ranges.Count; i++)
        {
            DefensiveStatRange defensiveStat = ranges[i];
            if (defensiveStat == null)
                continue;

            float rolledValue = defensiveStat.RollValue();
            if (Mathf.Approximately(rolledValue, 0f))
                continue;

            result.Add(new StatModifier
            {
                statID = defensiveStat.GetStatID(),
                value = rolledValue,
                modifierType = ModifierType.Flat
            });
        }

        return result;
    }

    private static List<DefensiveStatRange> CloneDefensiveRanges(List<DefensiveStatRange> source)
    {
        List<DefensiveStatRange> clone = new List<DefensiveStatRange>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            DefensiveStatRange item = source[i];
            if (item == null)
                continue;

            clone.Add(new DefensiveStatRange
            {
                statType = item.statType,
                minValue = item.minValue,
                maxValue = item.maxValue
            });
        }

        return clone;
    }

    private static List<StatModifierRange> CloneStatModifierRanges(List<StatModifierRange> source)
    {
        List<StatModifierRange> clone = new List<StatModifierRange>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            StatModifierRange range = source[i];
            if (range == null || string.IsNullOrWhiteSpace(range.statID))
                continue;

            clone.Add(new StatModifierRange
            {
                statID = range.statID,
                modifierType = range.modifierType,
                minValue = range.minValue,
                maxValue = range.maxValue
            });
        }

        return clone;
    }

    private static List<StatModifier> CloneStatModifiers(List<StatModifier> source)
    {
        List<StatModifier> clone = new List<StatModifier>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            StatModifier stat = source[i];
            if (stat == null || string.IsNullOrWhiteSpace(stat.statID))
                continue;

            clone.Add(new StatModifier
            {
                statID = stat.statID,
                value = stat.value,
                modifierType = stat.modifierType
            });
        }

        return clone;
    }
}
