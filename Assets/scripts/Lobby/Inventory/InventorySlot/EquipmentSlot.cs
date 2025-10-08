using UnityEngine;

public class EquipmentSlot : InventorySlot
{
    [Header("Equipment Slot Settings")]
    [SerializeField] private string acceptedItemClass; // e.g., "Weapon", "Hat", "Clothes", etc.

    // Events for equipment changes
    public System.Action<Item> OnItemEquipped;
    public System.Action<Item> OnItemUnequipped;

    private Item equippedItem; // Track currently equipped item

    #region Overridden Methods
    public override bool TryAddItem(Item item, int count = 1)
    {
        if (!CanAcceptItem(item, count))
            return false;

        // Call base method to add the item
        bool success = base.TryAddItem(item, count);

        if (success)
        {
            EquipItem(item);
        }

        return success;
    }

    public override void RemoveItem()
    {
        Item removedItem = currentItem;
        base.RemoveItem();

        if (removedItem != null)
        {
            UnequipItem(removedItem);
        }
    }

    public override bool CanAcceptItem(Item item, int count = 1)
    {
        // Check if the item class matches the accepted class
        if (!IsItemClassAccepted(item))
        {
            Debug.Log($"Item class not accepted. Required: {acceptedItemClass}, Got: {item.GetType().Name}");
            return false;
        }

        // Equipment slots typically only hold one item (no stacking)
        if (!IsEmpty)
            return false;

        return base.CanAcceptItem(item, count);
    }
    #endregion

    #region Equipment Specific Methods
    private bool IsItemClassAccepted(Item item)
    {
        if (string.IsNullOrEmpty(acceptedItemClass))
            return true;

        // Check if the item's type matches the accepted class
        return item.GetType().Name == acceptedItemClass;
    }

    private void EquipItem(Item item)
    {
        equippedItem = item;
        Debug.Log($"Equipped: {item.ItemName} in {acceptedItemClass} slot");

        // You can add specific equipment logic here based on item type
        if (item is Weapon weapon)
        {
            HandleWeaponEquip(weapon);
        }
        else if (item is Hat hat)
        {
            HandleHatEquip(hat);
        }
        else if (item is Clothes clothes)
        {
            HandleClothesEquip(clothes);
        }
        // Add more equipment types as needed

        OnItemEquipped?.Invoke(item);
    }

    private void UnequipItem(Item item)
    {
        Debug.Log($"Unequipped: {item.ItemName} from {acceptedItemClass} slot");

        // You can add specific unequip logic here based on item type
        if (item is Weapon weapon)
        {
            HandleWeaponUnequip(weapon);
        }
        else if (item is Hat hat)
        {
            HandleHatUnequip(hat);
        }
        else if (item is Clothes clothes)
        {
            HandleClothesUnequip(clothes);
        }
        // Add more equipment types as needed

        equippedItem = null;
        OnItemUnequipped?.Invoke(item);
    }
    #endregion

    #region Equipment Type Handlers
    private void HandleWeaponEquip(Weapon weapon)
    {
        // Add weapon-specific equip logic here
        Debug.Log($"Weapon equipped - Damage: {weapon.GetDamage()}, Defense: {weapon.GetDefense()}");

        // Example: Apply weapon stats to player
        // PlayerStats.Instance.AddDamage(weapon.GetDamage());
        // PlayerStats.Instance.AddDefense(weapon.GetDefense());
    }

    private void HandleWeaponUnequip(Weapon weapon)
    {
        // Add weapon-specific unequip logic here
        Debug.Log("Weapon unequipped");

        // Example: Remove weapon stats from player
        // PlayerStats.Instance.RemoveDamage(weapon.GetDamage());
        // PlayerStats.Instance.RemoveDefense(weapon.GetDefense());
    }

    private void HandleHatEquip(Hat hat)
    {
        // Add hat-specific equip logic here
        Debug.Log("Hat equipped");
    }

    private void HandleHatUnequip(Hat hat)
    {
        // Add hat-specific unequip logic here
        Debug.Log("Hat unequipped");
    }

    private void HandleClothesEquip(Clothes clothes)
    {
        // Add clothes-specific equip logic here
        Debug.Log("Clothes equipped");
    }

    private void HandleClothesUnequip(Clothes clothes)
    {
        // Add clothes-specific unequip logic here
        Debug.Log("Clothes unequipped");
    }
    #endregion

    #region Public Methods
    public string GetAcceptedItemClass() => acceptedItemClass;

    public void SetAcceptedItemClass(string newClass)
    {
        acceptedItemClass = newClass;

        // If current item doesn't match the new class, remove it
        if (!IsEmpty && !IsItemClassAccepted(currentItem))
        {
            RemoveItem();
        }
    }

    public Item GetEquippedItem() => equippedItem;

    public bool IsEquipped() => equippedItem != null;
    #endregion

    #region Drag and Drop Integration
    // This method can be called from your drag and drop system when an item is dragged into this slot
    public void HandleItemDropped(Item item, int count = 1)
    {
        if (TryAddItem(item, count))
        {
            Debug.Log($"Successfully equipped {item.ItemName}");
        }
        else
        {
            Debug.Log($"Failed to equip {item.ItemName}");
        }
    }

    // This method can be called from your drag and drop system when an item is dragged out of this slot
    public void HandleItemRemoved()
    {
        if (!IsEmpty)
        {
            RemoveItem();
        }
    }
    #endregion
}