using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI stackCountText;

    [Header("Slot Data")]
    [SerializeField] protected Item currentItem;
    [SerializeField] private int stackCount = 1;

    // Visual settings
    private readonly Color EMPTY_SLOT_COLOR = new Color(1f, 0.976f, 0.882f, 0f); // FFF9E1 in RGB

    // Public properties
    public Item CurrentItem => currentItem;
    public int StackCount => stackCount;
    public bool IsEmpty => currentItem == null;
    public bool IsFull => !IsEmpty && currentItem.IsStackable && stackCount >= currentItem.MaxStackSize;
    public int AvailableSpace => CalculateAvailableSpace();

    private void Awake()
    {
        EnsureUIReferences();
        InitializeEmptySlot();
    }

    #region Public API
    public virtual bool TryAddItem(Item item, int count = 1)
    {
        if (item == null) return false;

        if (IsEmpty)
            return AddNewItem(item, count);

        if (CanStackWith(item))
            return AddToExistingStack(count);

        return false;
    }

    public virtual void RemoveItem()
    {
        currentItem = null;
        stackCount = 0;
        UpdateSlotVisuals();
    }

    public bool TryIncreaseStack(int amount = 1)
    {
        if (IsEmpty || !currentItem.IsStackable || amount <= 0)
            return false;

        int actualAmount = Mathf.Min(amount, AvailableSpace);
        if (actualAmount == 0)
            return false;

        stackCount += actualAmount;
        UpdateSlotVisuals();
        return actualAmount == amount;
    }

    public void DecreaseStack(int amount = 1)
    {
        if (IsEmpty || amount <= 0) return;

        stackCount = Mathf.Max(0, stackCount - amount);

        if (stackCount == 0)
            RemoveItem();
        else
            UpdateSlotVisuals();
    }

    public void UseItem()
    {
        if (IsEmpty) return;

        Debug.Log($"Using item: {currentItem.ItemName}");
        currentItem.Use();

        if (currentItem.IsConsumable)
            DecreaseStack(1);
    }

    public virtual bool CanAcceptItem(Item item, int count = 1)
    {
        if (IsEmpty) return true;
        return CanStackWith(item) && (stackCount + count <= currentItem.MaxStackSize);
    }

    public bool ContainsItem(Item item) => currentItem == item;

    public void SetUIReferences(Image image, TextMeshProUGUI text)
    {
        itemImage = image;
        stackCountText = text;
        UpdateSlotVisuals();
    }

    public void RefreshUI() => UpdateSlotVisuals();
    #endregion

    #region Private Implementation
    private bool AddNewItem(Item item, int count)
    {
        currentItem = item;
        stackCount = CalculateInitialStackCount(item, count);
        UpdateSlotVisuals();
        return true;
    }

    private bool AddToExistingStack(int count)
    {
        int actualAmount = Mathf.Min(count, AvailableSpace);
        if (actualAmount == 0) return false;

        stackCount += actualAmount;
        UpdateSlotVisuals();
        return actualAmount == count;
    }

    private bool CanStackWith(Item item)
    {
        return currentItem == item && currentItem.IsStackable;
    }

    private int CalculateAvailableSpace()
    {
        if (IsEmpty || !currentItem.IsStackable) return 0;
        return Mathf.Max(0, currentItem.MaxStackSize - stackCount);
    }

    private int CalculateInitialStackCount(Item item, int requestedCount)
    {
        if (!item.IsStackable) return 1;
        return Mathf.Min(requestedCount, item.MaxStackSize);
    }
    #endregion

    #region UI Management
    private void EnsureUIReferences()
    {
        if (itemImage == null)
            itemImage = FindUIContainer<Image>("ItemImage");

        if (stackCountText == null)
            stackCountText = FindUIContainer<TextMeshProUGUI>("StackText");
    }

    private T FindUIContainer<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : GetComponentInChildren<T>();
    }

    private void UpdateSlotVisuals()
    {
        EnsureUIReferences();

        if (IsEmpty)
            ShowEmptySlot();
        else
            ShowItemSlot();
    }

    private void ShowItemSlot()
    {
        // Configure item image
        if (itemImage != null)
        {
            itemImage.sprite = currentItem.Icon;
            itemImage.enabled = true;
            itemImage.color = Color.white;
            itemImage.preserveAspect = true;
        }

        // Configure stack text
        if (stackCountText != null)
        {
            bool shouldShowStack = currentItem.IsStackable && stackCount > 1;
            stackCountText.enabled = shouldShowStack;
            stackCountText.text = shouldShowStack ? stackCount.ToString() : "";
        }
    }

    private void ShowEmptySlot()
    {
        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.enabled = true;
            itemImage.color = EMPTY_SLOT_COLOR;
        }

        if (stackCountText != null)
        {
            stackCountText.enabled = false;
            stackCountText.text = "";
        }
    }

    private void InitializeEmptySlot()
    {
        currentItem = null;
        stackCount = 0;
        ShowEmptySlot();
    }
    #endregion

    #region Editor Support
    private void OnValidate()
    {
        if (Application.isPlaying)
            UpdateSlotVisuals();
    }
    #endregion
}
