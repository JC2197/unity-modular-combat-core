using UnityEngine;

/// <summary>
/// Abstract base for all crafting-tool upgrade behaviours.
/// Create concrete subclasses (e.g. AugmentOperation, DuplicateOperation) and
/// assign them to a <see cref="ToolItemConfig"/>'s operations array in the Inspector.
///
/// Each operation independently decides whether it can run given the current
/// gear + optional orb selection, and performs its effect when executed.
/// </summary>
public abstract class UpgradeOperation : ScriptableObject
{
    [Header("Operation Info")]
    [Tooltip("Short label shown in UI (e.g. 'Augment', 'Duplicate').")]
    public string operationLabel = "Operate";

    [Tooltip("Tooltip text describing what this operation does.")]
    [TextArea(2, 4)]
    public string operationDescription = "";

    
    // ── Validity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when all prerequisites for this operation are met and the
    /// Craft button should be interactive. Called every time the combinator
    /// selection changes.
    /// </summary>
    /// <param name="gear">The gear item in the ItemSlot (may be null).</param>
    /// <param name="orb">The orb in the SlottedCraft slot (may be null).</param>
    public abstract bool CanApply(ItemInstance gear, ItemInstance orb);

    // ── Execution ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Performs the upgrade and returns the resulting item (may be the same
    /// instance modified in-place, a new item, or null on failure).
    /// Only called after <see cref="CanApply"/> returns true.
    /// </summary>
    /// <param name="gear">The gear item in the ItemSlot.</param>
    /// <param name="orb">The orb in the slot (null for non-slottable tools).</param>
    /// <returns>The output item to place back in the result slot, or null on failure.</returns>
    public abstract ItemInstance Apply(ItemInstance gear, ItemInstance orb);
}
