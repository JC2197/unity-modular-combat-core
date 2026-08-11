using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base config for item types that can exist in stacks.
/// Shared visuals and stack instance creation live here so concrete stackable
/// item configs only need to define identity and payload.
/// </summary>
public abstract class StackableItemConfig : ItemConfig
{
    [Header("Sprites")]
    [Tooltip("Sprite shown in inventory")]
    public Sprite inventorySprite;

    [Tooltip("Sprite shown on ground")]
    public Sprite worldSprite;

    [Header("Particle System Override")]
    public ParticleSystem particleSystemOverride;

    public virtual int MaxStackSize => ItemInstance.MAX_STACK_SIZE;

    protected ItemInstance CreateStackInstance(string itemType, string displayName, int rarityTier, int stackSize)
    {
        ItemInstance item = new ItemInstance(itemType, displayName, rarityTier, Mathf.Clamp(stackSize, 1, MaxStackSize));
        item.description = description;
        return item;
    }
}
