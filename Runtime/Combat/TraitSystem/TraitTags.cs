using UnityEngine;

/// <summary>
/// PropertyAttributes for trait tag dropdowns.
/// Tags are now configured in TraitTagDatabase ScriptableObject.
/// 
/// Usage:
/// - Core Tags: Body, Mind, Skill, Survival, Power, Faith
/// - Specialized Tags: Fire, Ice, Lightning, Physical, Poison, etc.
/// Each tag has an associated color configured via StatColorDropdown in TraitTagDatabase.
/// </summary>

// ============================================================================
// PropertyAttribute classes for dropdown drawers
// ============================================================================

/// <summary>
/// Attribute to mark a string field as a Core Trait Tag dropdown.
/// Populates from TraitTagDatabase.coreTags.
/// </summary>
public class CoreTraitTagDropdownAttribute : PropertyAttribute { }

/// <summary>
/// Attribute to mark a string field as a Specialized Trait Tag dropdown.
/// Populates from TraitTagDatabase.specializedTags.
/// </summary>
public class SpecializedTraitTagDropdownAttribute : PropertyAttribute { }

