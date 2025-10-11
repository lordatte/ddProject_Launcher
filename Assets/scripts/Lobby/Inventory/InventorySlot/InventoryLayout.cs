[System.Serializable]
public class InventoryLayout
{
    public string ItemInstanceId;   // PlayFab’s ItemInstance.Id
    public int SlotIndex;           // 0..44 in your 45-slot grid
}