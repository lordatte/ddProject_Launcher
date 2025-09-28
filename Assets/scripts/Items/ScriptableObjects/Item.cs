using UnityEngine;

public abstract class Item : ScriptableObject
{
    [Header("Item Basic Information")]
    [SerializeField] private string _itemName;
    [SerializeField] private string _playFabId;
    [SerializeField] private Sprite _icon;

    [Header("Item Properties")]
    [SerializeField] private bool _isConsumable;
    [SerializeField] private bool _isStackable;
    [SerializeField] private bool _isTradable;
    [SerializeField] private int _maxStackSize = 64; // Added MaxStackSize with default value

    // Public properties with proper naming convention
    public string ItemName => _itemName;
    public string PlayFabId => _playFabId;
    public Sprite Icon => _icon;
    public bool IsConsumable => _isConsumable;
    public bool IsStackable => _isStackable;
    public bool IsTradable => _isTradable;
    public int MaxStackSize => _maxStackSize; // Added MaxStackSize property

    // Optional: If you need methods for additional logic
    public virtual void Use()
    {
        // Base use functionality to be overridden by derived classes
        Debug.Log($"Using item: {_itemName}");
    }

    public virtual string GetDescription()
    {
        return $"Item: {_itemName}";
    }
}