using UnityEngine;

public abstract class Item : ScriptableObject
{
    [Header("Item Basic Information")]
    [SerializeField] private string itemName;
    [SerializeField] private string playFabId;

    [Header("Item Properties")]
    [SerializeField] private bool isConsumable;
    [SerializeField] private bool isStackable;
    [SerializeField] private bool isTradable;

    // Protected properties for derived class access
    protected string ItemName => itemName;
    protected string PlayFabId => playFabId;
    protected bool IsConsumable => isConsumable;
    protected bool IsStackable => isStackable;
    protected bool IsTradable => isTradable;

    public string GetItemName() => itemName;
    public string GetPlayFabId() => playFabId;
    public bool GetIsConsumable() => isConsumable;
    public bool GetIsStackable() => isStackable;
    public bool GetIsTradable() => isTradable;


}